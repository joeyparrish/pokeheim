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
    private const string PokedexIconPath = "Pokedex icon.png";

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

    public static string GetTrophyPrefabName(string prefabName) {
      return MonsterMetadata.Get(prefabName).TrophyName;
    }

    // Untranslated name
    public static string GetEntryName(string prefabName) {
      var metadata = MonsterMetadata.Get(prefabName);

      var player = Player.m_localPlayer;
      if (!player.HasInPokedex(prefabName)) {
        return "???";
      }

      return metadata.GenericName;
    }

    public static Sprite GetEntryIcon(string prefabName) {
      var metadata = MonsterMetadata.Get(prefabName);

      var player = Player.m_localPlayer;
      if (!player.HasInPokedex(prefabName)) {
        return metadata.TrophyShadowIcon;
      } else {
        return metadata.TrophyIcon;
      }
    }

    public static string GetLore(string prefabName) {
      var metadata = MonsterMetadata.Get(prefabName);

      // Abuse m_knownStations (maps string to int) to keep track of
      // monsters caught for the Pokedex.
      var fakeStationName = PokedexFakeStationPrefix + prefabName;
      int caught = 0;
      var player = Player.m_localPlayer;
      player.m_knownStations.TryGetValue(fakeStationName, out caught);

      if (caught == 0) {
        return "";
      }

      var lore = $"$stats_type: {metadata.FactionName}\n";
      lore += $"$stats_hp: {metadata.BaseHealth}\n";
      lore += $"$stats_damage: {metadata.TotalDamage}\n";
      lore += $"$stats_catch_rate: {metadata.CatchRate:P2}\n";
      lore += $"$stats_caught: {caught}";
      return lore;
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
      static List<string> Postfix(List<string> ignored) {
        var result = new List<string>();
        foreach (var metadata in MonsterMetadata.GetAllMonsters()) {
          result.Add(metadata.PrefabName);
        }
        return result;
      }
    }

    [HarmonyPatch]
    class RenameTrophiesPanel_Patch {
      static Text TrophyPanelTitle = null;

      [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
      [HarmonyPostfix]
      static void FindPokedexComponents(InventoryGui __instance) {
        var gui = __instance;

        foreach (var component in gui.m_trophiesPanel.GetComponentsInChildren<Text>()) {
          if (component.name == "topic") {
            TrophyPanelTitle = component;
            break;
          }
        }

        foreach (var component in gui.m_inventoryRoot.GetComponentsInChildren<UITooltip>()) {
          if (component.name == "Trophies") {
            component.Set("", "$pokedex");
          }
        }

        foreach (var component in gui.m_inventoryRoot.GetComponentsInChildren<Button>()) {
          if (component.name == "Trophies") {
            foreach (var child in component.GetComponentsInChildren<Image>()) {
              if (child.name == "Image") {
                child.sprite = Utils.LoadSprite(PokedexIconPath);
              }
            }
          }
        }
      }

      [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnOpenTrophies))]
      [HarmonyPostfix]
      static void ComputePokedexTitleElementAndText(InventoryGui __instance) {
        var pokedexPercent = MonsterMetadata.PokedexFullness() * 100f;
        Logger.LogInfo($"Pokedex {pokedexPercent:n1}% complete");
        Utils.PatchUIText(TrophyPanelTitle,
            Localization.instance.Localize(
            "$pokedex_percent_complete", pokedexPercent.ToString("n1")));
      }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateTrophyList))]
    class UpdatePokedex_Patch {
      static IEnumerable<CodeInstruction> Transpiler(
          IEnumerable<CodeInstruction> instructions,
          ILGenerator generator) {
        var monsterNameBackupVar = generator.DeclareLocal(typeof(String));

        var getStringItemMethod = typeof(List<string>).GetMethod("get_Item");
        var localizeMethod = typeof(Localization).GetMethod(
            "Localize", new Type[] { typeof(string) });
        var getIconMethod = typeof(ItemDrop.ItemData).GetMethod("GetIcon");

        var getTrophyPrefabNameMethod = typeof(PlayerMods).GetMethod(nameof(PlayerMods.GetTrophyPrefabName));
        var getEntryNameMethod = typeof(PlayerMods).GetMethod(nameof(PlayerMods.GetEntryName));
        var getEntryIconMethod = typeof(PlayerMods).GetMethod(nameof(PlayerMods.GetEntryIcon));
        var getLoreMethod = typeof(PlayerMods).GetMethod(nameof(PlayerMods.GetLore));

        var phases = new TranspilerSequence.Phase[] {
          new TranspilerSequence.Phase {
            matcher = code => (code.opcode == OpCodes.Callvirt &&
                               (code.operand as MethodInfo) == getStringItemMethod),
            replacer = code => new CodeInstruction[] {
              // Load a string from the array of trophy prefab names.
              code,

              // After the original instruction, inject instructions to back up
              // the original monster name in our own local var.
              new CodeInstruction(OpCodes.Stloc_S, monsterNameBackupVar),
              new CodeInstruction(OpCodes.Ldloc_S, monsterNameBackupVar),

              // Then convert that to a trophy name.  The original method will
              // now store this into a local var of its own, and we don't care
              // what index it has because we have our own backup.
              new CodeInstruction(OpCodes.Call, getTrophyPrefabNameMethod),
            },
          },
          new TranspilerSequence.Phase {
            matcher = code => (code.opcode == OpCodes.Callvirt &&
                               (code.operand as MethodInfo) == localizeMethod),
            replacer = code => new CodeInstruction[] {
              // Right before we localize the entry name, replace it.
              // Remove the trophy name from the stack.
              new CodeInstruction(OpCodes.Pop),
              // Load our backup of the original monster name onto the stack.
              new CodeInstruction(OpCodes.Ldloc_S, monsterNameBackupVar),
              // Get the replacement name (untranslated) for this Pokedex entry.
              new CodeInstruction(OpCodes.Call, getEntryNameMethod),
              // Continue with the Localize() call.
              code,
            },
          },
          new TranspilerSequence.Phase {
            matcher = code => (code.opcode == OpCodes.Callvirt &&
                               (code.operand as MethodInfo) == getIconMethod),
            replacer = code => new CodeInstruction[] {
              // Instead of loading the icon from the trophy ItemDrop, call our
              // method instead.
              // Remove the item data from the stack.
              new CodeInstruction(OpCodes.Pop),
              // Load our backup of the original monster name onto the stack.
              new CodeInstruction(OpCodes.Ldloc_S, monsterNameBackupVar),
              // Get the replacement icon for this Pokedex entry.
              new CodeInstruction(OpCodes.Call, getEntryIconMethod),
            },
          },
          new TranspilerSequence.Phase {
            matcher = code => (code.opcode == OpCodes.Ldstr &&
                               (code.operand as String) == "_lore"),
            replacer = code => new CodeInstruction[] {
              // This occurs right before the original method concatenates the
              // trophy name with "_lore".  Skip this, and instead...
              // Remove the trophy name from the stack.
              new CodeInstruction(OpCodes.Pop),
              // Load our backup of the original monster name onto the stack.
              new CodeInstruction(OpCodes.Ldloc_S, monsterNameBackupVar),
              // Get the replacement lore (untranslated) for this Pokedex entry.
              new CodeInstruction(OpCodes.Call, getLoreMethod),
              // The next instruction is a 2-string concat operation.  So place
              // a blank string onto the stack.
              new CodeInstruction(OpCodes.Ldstr, ""),
              // The rest of the loop will now concat the two and then localize.
            },
          },
        };
        return TranspilerSequence.Execute(
            "UpdateTrophyList", phases, instructions);
      }
    }

    // Don't take damage from the monsters.  Clothing is just for decoration.
    [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
    class PlayerTakesNoDamage_Patch {
      static float Postfix(float originalResult) {
        return 1e9f;
      }
    }

    // Increase base HP in case of a fall, and max weight so the player doesn't
    // have to think about carrying too much.
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    class PlayerHasMoreHPAndCanCarryMore_Patch {
      static void Postfix(Player __instance) {
        var player = __instance;
        player.m_baseHP = 50f;
        player.m_maxCarryWeight = 1000f;
      }
    }

    // The player shouldn't have to think about stamina in Pokeheim.
    [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
    class PlayerAlwaysHasStamina_Patch {
      static void Prefix(Player __instance, ref float v) {
        // Except while swimming.  You can't swim across the ocean.
        // That's what saddles are for.
        if (!__instance.IsSwiming()) {
          v = 0f;  // No stamina used.
        }
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

    // Don't create a tombstone, and let the player keep everything when they
    // die.
    [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
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

    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    class ModifyPlayerAttacks_Patch {
      static void Prefix(Character __instance, HitData hit) {
        var monster = __instance;
        Character attacker = hit.GetAttacker();

        if (attacker != null && attacker.IsPlayer()) {
          if (monster.IsBoss()) {
            // The boss is immune to attacks from the Player.  You _must_ use
            // captured monsters on a boss.
            hit.ApplyModifier(0f);
          } else {
            // No matter what badass weapons you bring into Pokeheim (which you
            // shouldn't do!), they will all do about 5 damage, equivalent to a
            // club.  This forces people to play Pokeheim the way it was meant.
            // We will back up fire damage, though, and leave that alone, so
            // that torches stay pretty useful on early monsters.
            var fireDamage = hit.m_damage.m_fire;
            hit.m_damage.m_fire = 0f;

            var totalDamage = hit.GetTotalDamage();
            hit.ApplyModifier(5f / totalDamage);

            hit.m_damage.m_fire = fireDamage;
          }
        }
      }
    }
  }
}
