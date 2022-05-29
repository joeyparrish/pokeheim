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
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class PlayerMods {
    private const string PokedexFakeStationPrefix = "com.pokeheim.pokedex.";
    private const string PokedexIconPath = "Pokeheim/Assets/Pokedex icon.png";

    [PokeheimInit]
    public static void Init() {
      CommandManager.Instance.AddConsoleCommand(new ResetPokedex());
    }

    public static void LogCapture(this Player player, string prefabName) {
      // Abuse m_knownStations (maps string to int) to keep track of monsters
      // caught for the Pokedex.
      var fakeStationName = PokedexFakeStationPrefix + prefabName;
      int caught = 0;
      player.m_knownStations.TryGetValue(fakeStationName, out caught);
      caught += 1;
      player.m_knownStations[fakeStationName] = caught;

      player.UpdateTrainerSkill();
    }

    public static void UpdateTrainerSkill(this Player player) {
      var skillType = BallItem.Skill;
      var skill = player.m_skills.GetSkill(skillType);
      var oldLevel = skill.m_level;

      // Set the trainer skill level based on the fullness of the pokedex.
      var pokedexPercent = MonsterMetadata.PokedexFullness() * 100f;
      skill.m_level = (float)((int)pokedexPercent);
      var leftovers = pokedexPercent - skill.m_level;
      skill.m_accumulator = skill.GetNextLevelRequirement() * leftovers;

      if (skill.m_level != oldLevel) {
        player.OnSkillLevelup(skillType, skill.m_level);
        var messageType = oldLevel > 0 ?
            MessageHud.MessageType.TopLeft : MessageHud.MessageType.Center;
        var message = "$msg_skillup $skill_monster_training: " +
            (int)skill.m_level;
        player.Message(messageType, message, 0, skill.m_info.m_icon);
      }
    }

    public static bool HasInPokedex(this Player player, string prefabName) {
      var fakeStationName = PokedexFakeStationPrefix + prefabName;
      var caught = 0;
      player.m_knownStations.TryGetValue(fakeStationName, out caught);
      return caught > 0;
    }

    public static string GetTrophyName(string prefabName) {
      return MonsterMetadata.Get(prefabName)?.TrophyName;
    }

    public static string GetLore(string prefabName) {
      var metadata = MonsterMetadata.Get(prefabName);

      // Abuse m_knownStations (maps string to int) to keep track of
      // monsters caught for the Pokedex.
      var fakeStationName = PokedexFakeStationPrefix + prefabName;
      int caught = 0;
      var player = Player.m_localPlayer;
      player.m_knownStations.TryGetValue(fakeStationName, out caught);

      var lore = $"$stats_type: {metadata.FactionName}\n";
      lore += $"$stats_hp: {metadata.BaseHealth}\n";
      lore += $"$stats_damage: {metadata.TotalDamage}\n";
      lore += $"$stats_catch_rate: {metadata.CatchRate:P2}\n";
      lore += $"$stats_caught: {caught}";
      return lore;
    }

    public static IEnumerable<string> GetKnownPokedexEntries(Player player) {
      // Abuse m_knownStations (maps string to int) to keep track of monsters
      // caught for the Pokedex.
      var keys = new List<string>(player.m_knownStations.Keys);
      foreach (var key in keys) {
        // The text after the prefix is a monster prefab name.
        if (key.StartsWith(PokedexFakeStationPrefix)) {
          var prefabName = key.Substring(PokedexFakeStationPrefix.Length);

          // If we added an alias after the data was stored, we may need to
          // coalesce two entries into one.
          var metadata = MonsterMetadata.Get(prefabName);
          if (metadata.PrefabName != prefabName) {
            var value = player.m_knownStations[key];
            player.m_knownStations[PokedexFakeStationPrefix + metadata.PrefabName] += value;
            player.m_knownStations.Remove(key);
            continue;
          }

          yield return prefabName;
        }
      }
    }

    class ResetPokedex : ConsoleCommand {
      public override string Name => "resetpokedex";
      public override string Help => "Clear the pokedex";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var player = Player.m_localPlayer;
        var keys = new List<string>(player.m_knownStations.Keys);
        foreach (var key in keys) {
          if (key.StartsWith(PokedexFakeStationPrefix)) {
            player.m_knownStations.Remove(key);
          }
        }
      }
    }

    // We use LogCapture/UpdateTrainerSkill to set the trainer skill level.
    // RaiseSkill is suppressed for this skill.
    [HarmonyPatch(typeof(Player), nameof(Player.RaiseSkill))]
    class TrainerSkillOnlyChangedByCapture_Patch {
      static bool Prefix(Skills.SkillType skill) {
        // For the trainer skill, suppress this method.
        return skill != BallItem.Skill;
      }
    }

    // Instead of trophy prefab names, this will return monster prefab names.
    [HarmonyPatch(typeof(Player), nameof(Player.GetTrophies))]
    class OverrideTrophiesToDrivePokedex_Patch {
      static List<string> Postfix(List<string> ignored, Player __instance) {
        var player = __instance;
        var result = new List<string>();

        foreach (var prefabName in GetKnownPokedexEntries(player)) {
          // Only add the prefab name to the list if there's a trophy
          // associated with it.  Otherwise, we will not be able to display
          // it in the Pokedex.
          var metadata = MonsterMetadata.Get(prefabName);
          if (metadata.TrophyName != null) {
            result.Add(prefabName);
          }
        }
        return result;
      }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    class RenameTrophiesPanel_Patch {
      static void Postfix(InventoryGui __instance) {
        var gui = __instance;

        foreach (var component in gui.m_inventoryRoot.GetComponentsInChildren<UITooltip>()) {
          if (component.name == "Trophies") {
            component.Set("", "$pokedex");
          }
        }

        foreach (var component in gui.m_inventoryRoot.GetComponentsInChildren<Button>()) {
          if (component.name == "Trophies") {
            foreach (var child in component.GetComponentsInChildren<Image>()) {
              if (child.name == "Image") {
                child.sprite = AssetUtils.LoadSpriteFromFile(PokedexIconPath);
              }
            }
          }
        }
      }
    }

    [HarmonyPatch]
    class ShowPokedexCompletion_Patch {
      // Annoyingly, the Pokedex title must be patched on every Update().
      // This was not true in November 2021, and it is true in May of 2022.
      // Somehow, Valheim changed in a way that makes this kind of patch more
      // difficult.  To keep the extra Update() code lightweight, we precompute
      // the Text element and the actual text we set inside it.
      static Text PokedexTitleElement;
      static string PokedexTitleText;

      [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnOpenTrophies))]
      [HarmonyPostfix]
      static void ComputePokedexTitleElementAndText(InventoryGui __instance) {
        var pokedexPercent = MonsterMetadata.PokedexFullness() * 100f;
        Logger.LogInfo($"Pokedex {pokedexPercent:n1}% complete");
        PokedexTitleText = Localization.instance.Localize(
            "$pokedex_percent_complete", pokedexPercent.ToString("n1"));

        var gui = __instance;
        foreach (var component in gui.m_trophiesPanel.GetComponentsInChildren<Text>()) {
          if (component.name == "topic") {
            PokedexTitleElement = component;
          }
        }
      }

      [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
      [HarmonyPostfix]
      static void PatchPokedexTitle(InventoryGui __instance) {
        if (!InventoryGui.IsVisible()) {
          return;
        }
        PokedexTitleElement.text = PokedexTitleText;
      }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateTrophyList))]
    class UpdatePokedex_Patch {
      static IEnumerable<CodeInstruction> Transpiler(
          IEnumerable<CodeInstruction> instructions,
          ILGenerator generator) {
        var foundGetStringItem = false;
        var foundLoreTarget = false;

        var monsterNameBackupVar = generator.DeclareLocal(typeof(String));

        var getStringItemMethod = typeof(List<string>).GetMethod("get_Item");
        var getLoreMethod = typeof(PlayerMods).GetMethod(nameof(PlayerMods.GetLore));
        var getTrophyNameMethod = typeof(PlayerMods).GetMethod(nameof(PlayerMods.GetTrophyName));

        foreach (var code in instructions) {
          // These targets appear in this order.
          if (foundGetStringItem == false) {
            yield return code;

            if (code.opcode == OpCodes.Callvirt &&
                code.operand as MethodInfo == getStringItemMethod) {
              // This is getting the next string from the trophy list.
              foundGetStringItem = true;

              // After the original instruction, inject instructions to back up
              // the original monster name in our own local var.  Hopefully
              // nobody else is using the same slot number in another mod's
              // transpiler for the same method...
              yield return new CodeInstruction(OpCodes.Stloc_S, monsterNameBackupVar);
              yield return new CodeInstruction(OpCodes.Ldloc_S, monsterNameBackupVar);
              // Then convert that to a trophy name.  The original method will
              // now store this into a local var of its own, and we don't care
              // what index it has because we have our own backup.
              yield return new CodeInstruction(OpCodes.Call, getTrophyNameMethod);
            }
          } else if (foundLoreTarget == false) {
            if (code.opcode == OpCodes.Ldstr &&
                code.operand as String == "_lore") {
              // This occurs right before the original method concatenates the
              // trophy name with "_lore".  Skip this, and instead...
              foundLoreTarget = true;

              // Remove the trophy name from the stack.
              yield return new CodeInstruction(OpCodes.Pop);
              // Load our backup of the original monster name onto the stack.
              yield return new CodeInstruction(OpCodes.Ldloc_S, monsterNameBackupVar);
              // Get the replacement lore (untranslated) for this Pokedex entry.
              yield return new CodeInstruction(OpCodes.Call, getLoreMethod);
              // The next instruction is a 2-string concat operation.  So place
              // a blank string onto the stack.
              yield return new CodeInstruction(OpCodes.Ldstr, "");
              // The rest of the loop will now concat the two and then localize.
            } else {
              yield return code;
            }
          } else {
            yield return code;
          }
        }

        if (!foundGetStringItem || !foundLoreTarget) {
          Logger.LogError("Failed to patch UpdateTrophyList! " +
              $"({foundGetStringItem}, {foundLoreTarget})");
        }
      }
    }

    // Don't take damage from the monsters.  Clothing is just for decoration.
    [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
    class PlayerTakesNoDamage_Patch {
      static float Postfix(float originalResult) {
        return 1e9f;
      }
    }

    // Increase base HP in case of a fall, and stamina for fun.
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    class PlayerHasMoreHPAndStamina_Patch {
      static void Postfix(Player __instance) {
        var player = __instance;
        player.m_baseHP = 50f;
        player.m_baseStamina = 100f;
      }
    }

    // Clothing is just for decoration in Pokeheim, and you can't craft
    // potions, so the player is never freezing.
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.IsFreezing))]
    class PlayerIsNeverFreezing_Patch {
      static bool Postfix(bool originalResult) {
        return false;
      }
    }

    // There's no good way to get keys in Pokeheim, so let's just unlock all
    // the doors.
    [HarmonyPatch(typeof(Door), nameof(Door.Awake))]
    class PlayerDoesNotNeedAKey_Patch {
      static void Prefix(ref ItemDrop ___m_keyItem) {
        ___m_keyItem = null;
      }
    }

    // Let the player keep everything when they die.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveInventoryToGrave))]
    class PlayerKeepsEverythingOnDeath_Patch {
      static bool Prefix() {
        return false;
      }
    }

    // Players can always punch their own monsters, regardless of PVP setting.
    // The disadvantage of achieving it in this simple way is that fists will
    // never work against wild monsters.
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    class PlayerCanAlwaysAbusePokemon_Patch {
      static void Postfix(Player __instance) {
        var player = __instance;
        player.m_unarmedWeapon.m_itemData.m_shared.m_tamedOnly = true;
      }
    }
  }
}
