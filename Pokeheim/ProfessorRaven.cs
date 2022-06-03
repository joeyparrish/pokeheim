/**
 * Pokeheim - A Valheim Mod
 * Copyright (C) 2021 Joey Parrish
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class ProfessorRaven {
    private static readonly HashSet<string> OverrideTutorials = new HashSet<string> {
      // These override built-in tutorials:
      "temple1",
      "temple2",
      "inventory",
      "food",
      "cold",
      "encumbered",
      "death",
      "hammer",
      "pickaxe",
      "Eikthyr",
      "boss_trophy",

      // Added specially for Pokeheim:
      "pokeball",
      "caught",
      "caught_boss",
      "caught_all_bosses",
      "caught_em_all",
      "catch_raven",
    };

    private static HashSet<string> SkippedTutorials = new HashSet<string> {
      // These are triggered by GuidePoint objects and should not be achievable
      // in Pokeheim.
      "sleepingspot",
      "guardstone",
      "bathtub",
      "maptable",
      "workbench",
      "portal",
      "smelter",
      "temple4",  // Shown when Eikthyr is chained, but we disabled the hooks.

      // These are triggered by other events or items.  We don't need them, so
      // they are disabled.
      "start",
      "hunger",
      "ore",
      "hoe",
      "blackforest",
      "randomevent",
      "shield",
      "wishbone",
    };

    private static readonly HashSet<string> BossTrophies = new HashSet<string> {
      "$item_trophy_eikthyr",
      "$item_trophy_elder",
      "$item_trophy_bonemass",
      "$item_trophy_dragonqueen",
      "$item_trophy_goblinking",
    };

    [PokeheimInit]
    public static void Init() {
      CommandManager.Instance.AddConsoleCommand(new ActivateTutorial());
      CommandManager.Instance.AddConsoleCommand(new ResetLogBook());
    }

    // Rather than injecting all our custom tutorials into Tutorials, do what
    // Tutorial.ShowText does after a lookup, but with data constructed
    // on-the-fly according to our convention.  This inlines Raven.AddTempText
    // to add the "immediate" feature.  Some tutorials will take precedence
    // over others in queue.
    public static void PokeheimTutorial(
        this Player player, string name, bool immediate = false) {
      if (!player.HaveSeenTutorial(name)) {
        if (!Raven.IsInstantiated()) {
          UnityEngine.Object.Instantiate(
              Tutorial.instance.m_ravenPrefab,
              new Vector3(0f, 0f, 0f),
              Quaternion.identity);
        }

        foreach (var tempText in Raven.m_tempTexts) {
          if (tempText.m_key == name) {
            return;
          }
        }

        var ravenText = new Raven.RavenText {
          m_key = name,
          m_label = $"$pokeheim_tutorial_{name}_label",
          m_topic = $"$pokeheim_tutorial_{name}_topic",
          // Pre-localize this so that it can contain other localization
          // tokens, such as $KEY_Hide.
          m_text = Localization.instance.Localize(
              $"$pokeheim_tutorial_{name}_text"),
          m_munin = false,
          m_static = false,
        };

        if (immediate) {
          ravenText.m_priority = 123456789;
          Raven.m_tempTexts.Insert(0, ravenText);
        } else {
          Raven.m_tempTexts.Add(ravenText);
        }

        // This tutorial triggers a side-effect, but we only want to do it once.
        // So the side-effect is triggered from here, where we are guarded
        // against triggering it multiple times.
        if (name == "caught_em_all") {
          OdinMods.SpawnStaticOdin();
        }
      }
    }

    // Rename Hugin.
    [HarmonyPatch(typeof(Raven), nameof(Raven.Awake))]
    class ProfessorRaven_Patch {
      static void Postfix(ref string ___m_name) {
        ___m_name = "$professor_raven";
      }
    }

    // Make Professor Raven complain if you try to catch him.
    [HarmonyPatch(typeof(Raven), nameof(Raven.Damage))]
    class CantCatchRavenTutorial_Patch {
      static void Postfix(HitData hit) {
        var player = hit.GetAttacker() as Player;

        if (hit.m_skill == BallItem.Skill && player != null) {
          string name = "catch_raven";

          if (player.HaveSeenTutorial("catch_raven")) {
            // He warned you.  Now your name is Gary.
            name = "catch_raven_2";
            player.m_nview.GetZDO().Set("playerName", "Gary");
          }

          player.PokeheimTutorial(name, immediate: true);
        }
      }
    }

#if DEBUG
    // Find all tutorials keys that can be found through static objects and
    // prefabs.  Any we don't know about already get logged.
    [HarmonyPatch(typeof(Tutorial), nameof(Tutorial.Awake))]
    class FindNewTutorials_Patch {
      static IEnumerable<string> AllTutorialKeys() {
        foreach (var tutorial in Tutorial.instance.m_texts) {
          yield return tutorial.m_name;
        }
        // Some tutorials are triggered through GuidePoint objects.
        foreach (var prefab in ZNetScene.instance.m_prefabs) {
          var guidepoints = prefab.GetComponentsInChildren<GuidePoint>();
          foreach (var gp in guidepoints) {
            yield return gp.m_text.m_key;
          }
        }
      }

      static void Postfix() {
        foreach (var key in AllTutorialKeys()) {
          if (!OverrideTutorials.Contains(key) && !SkippedTutorials.Contains(key)) {
            Logger.LogWarning($"New tutorial found: {key}");
            SkippedTutorials.Add(key);
          }
        }
      }
    }

    [HarmonyPatch(typeof(Raven), nameof(Raven.Talk))]
    class LogAllTutorials_Patch {
      static void Postfix(Raven __instance) {
        var tutorial = __instance.m_currentText;
        Logger.LogInfo($"Showing tutorial key: {tutorial.m_key} text: {tutorial.m_text}");
      }
    }

    // Find tutorials for which we don't have all the necessary localization
    // keys.  This can help identify typos quickly without having to trigger
    // every tutorial at runtime.
    [HarmonyPatch(typeof(Tutorial), nameof(Tutorial.Awake))]
    class FindMissingTutorialTranslations_Patch {
      static void Postfix() {
        var localizations = LocalizationManager.Instance.GetLocalization();
        // Only check English here.  A separate tool will check for missing
        // localizations generally in each language by comparing with English.
        // This just identifies missing or typo'd names for tutorials in the
        // English source.
        var language = "English";

        foreach (var key in OverrideTutorials) {
          var label = $"$pokeheim_tutorial_{key}_label";
          var topic = $"$pokeheim_tutorial_{key}_topic";
          var text = $"$pokeheim_tutorial_{key}_text";

          if (!localizations.Contains(language, label)) {
            Logger.LogError($"Missing tutorial label: {label}");
          }

          if (!localizations.Contains(language, topic)) {
            Logger.LogError($"Missing tutorial topic: {topic}");
          }

          if (!localizations.Contains(language, text)) {
            Logger.LogError($"Missing tutorial text: {text}");
          }
        }
      }
    }
#endif

    // Replace the text that Professor Raven uses to get your attention.
    [HarmonyPatch(typeof(Raven), nameof(Raven.Awake))]
    class OverrideRavenGettingAttention_Patch {
      static void Postfix(Raven __instance) {
        var raven = __instance;
        raven.m_randomTextsImportant = new List<string> {
          "$pokeheim_raven_attention_1",
          "$pokeheim_raven_attention_2",
          "$pokeheim_raven_attention_3",
          "$pokeheim_raven_attention_4",
          "$pokeheim_raven_attention_5",
          "$pokeheim_raven_attention_6",
          "$pokeheim_raven_attention_7",
        };
        // I haven't seen these in practice.  Not sure how to trigger them.
        raven.m_randomTexts = new List<string> {
          "$pokeheim_raven_bored_1",
          "$pokeheim_raven_bored_2",
          "$pokeheim_raven_bored_3",
        };
      }
    }

    // Intercept and override all Raven text.  No original game tutorials will
    // be used in Pokeheim.  Any tutorial not in our list of overrides will be
    // ignored.  There are two entrypoints for text, each of which should be
    // observed and modified.
    [HarmonyPatch]
    class ReplaceTutorials_Patch {
      [HarmonyPatch(typeof(Raven), nameof(Raven.RegisterStaticText))]
      [HarmonyPrefix]
      static bool patchRavenStaticText(Raven.RavenText text) {
        var key = text.m_key;

        if (CanShowTutorial(key)) {
          // Register this modified text instead.
          text.m_topic = $"$pokeheim_tutorial_{key}_topic";
          text.m_text = $"$pokeheim_tutorial_{key}_text";
          text.m_label = $"$pokeheim_tutorial_{key}_label";

          // This specific text needs us to pre-localize and insert a value for
          // the number of bosses.  That's called future-proofing!
          if (key == "temple1") {
            text.m_text = Localization.instance.Localize(
                text.m_text, MonsterMetadata.NumberOfBosses().ToString());
          }
          return true;
        }

        return false;
      }

      [HarmonyPatch(typeof(Raven), nameof(Raven.AddTempText))]
      [HarmonyPrefix]
      static bool patchRavenTempText(
          ref string key, ref string topic, ref string text, ref string label) {
        if (CanShowTutorial(key)) {
          // Register this modified text instead.
          topic = $"$pokeheim_tutorial_{key}_topic";
          text = $"$pokeheim_tutorial_{key}_text";
          label = $"$pokeheim_tutorial_{key}_label";
          return true;
        }

        return false;
      }

      static bool CanShowTutorial(string key) {
#if DEBUG && false
        // Log a stack trace showing how this was initiated.  It is helpful to
        // understand the origin of built-in tutorials.
        Logger.LogInfo($"Tutorial \"{key}\" stack trace: {Environment.StackTrace}");
#endif

        if (OverrideTutorials.Contains(key)) {
          return true;
#if DEBUG
        } else if (!SkippedTutorials.Contains(key)) {
          Logger.LogWarning($"New tutorial found: {key}");
          SkippedTutorials.Add(key);
#endif
        }
        return false;
      }
    }

    // Trigger custom tutorials based on items.
    [HarmonyPatch(typeof(Player), nameof(Player.OnInventoryChanged))]
    class CustomInventoryTutorials_Patch {
      static void Postfix(Player __instance) {
        var player = __instance;
        if (player.m_isLoading) {
          return;
        }

        foreach (var item in player.m_inventory.GetAllItems()) {
          if (item.GetInhabitant()?.Faction == Character.Faction.Boss) {
            if (MonsterMetadata.CaughtAllBosses()) {
              player.PokeheimTutorial("caught_all_bosses");
            } else {
              player.PokeheimTutorial("caught_boss");
            }
          } else if (BossTrophies.Contains(item.m_shared.m_name)) {
            player.PokeheimTutorial("boss_trophy");
          } else if (item.IsBall()) {
            player.PokeheimTutorial("pokeball");
          }
        }
      }
    }

    [HarmonyPatch(typeof(Raven), nameof(Raven.FlyAway))]
    class DropFeathersOnDeparture_Patch {
      static void Prefix(Raven __instance, ref bool forceTeleport) {
        var raven = __instance;

        // Always go "poof" instead of flying away.
        forceTeleport = true;

        // Always leave behind feathers, which makes it easier for players to
        // end up crafting flint arrows.
        var feathersPrefab = PrefabManager.Instance.GetPrefab("Feathers");
        if (feathersPrefab != null) {
          var position = raven.transform.position + Vector3.zero;
          position.MoveToFloor(offset: 0.1f);
          var rotation = Quaternion.identity;
          UnityEngine.Object.Instantiate(feathersPrefab, position, rotation);
        }
      }
    }

    class ActivateTutorial : ConsoleCommand {
      public override string Name => "tutorial";
      public override string Help => "[key] Activates a specific tutorial by its key.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        if (args.Length > 0) {
          var key = args[0];

          foreach (var tutorialText in Tutorial.instance.m_texts) {
            if (tutorialText.m_name == key) {
              Activate(key, builtIn: true);
              return;
            }
          }

          if (OverrideTutorials.Contains(key)) {
            Activate(key, builtIn: false);
            return;
          }

          Debug.Log($"Tutorial not found: \"{key}\"");
        } else {
          Debug.Log($"Please specify a tutorial to activate.");
        }
      }

      private static void Activate(string key, bool builtIn) {
        Player.m_localPlayer.m_shownTutorials.Remove(key);

        if (SkippedTutorials.Contains(key)) {
          Debug.Log($"Tutorial \"{key}\" is disabled.");
          return;
        }

        if (builtIn) {
          Debug.Log($"Activating built-in tutorial \"{key}\".");
          Player.m_localPlayer.ShowTutorial(key);
        } else {
          Debug.Log($"Activating Pokeheim tutorial \"{key}\".");
          Player.m_localPlayer.PokeheimTutorial(key);
        }
      }
    }

    class ResetLogBook : ConsoleCommand {
      public override string Name => "resetlogbook";
      public override string Help => "Resets all the entries in the player's log book.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        Player.m_localPlayer.m_knownTexts.Clear();
      }
    }
  }
}
