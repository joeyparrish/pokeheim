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
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class ShinyMods {
    private static readonly Vector2 centerPivot = new Vector2(0.5f, 0.5f);
    private static readonly float shinySpawnRatePercent = 5;
    private static readonly string ShinyHudPath = "Shiny.png";
    private static Sprite ShinyHudIcon;

    [PokeheimInit]
    public static void Init() {
      ShinyHudIcon = Utils.LoadSprite(ShinyHudPath, centerPivot);
    }

    // Instead of star icons in the HUD, show the shiny icon for shiny monsters.
    [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.ShowHud))]
    class ShinyMonsterHud_Patch {
      static bool hudDataIsOld = false;

      static void Prefix(EnemyHud __instance, Character c) {
        hudDataIsOld = __instance.m_huds.TryGetValue(c, out var hud);
      }

      static void Postfix(EnemyHud __instance, Character c) {
        if (hudDataIsOld) {
          // We already patched this one.
          return;
        }

        if (!__instance.m_huds.TryGetValue(c, out var hud)) {
          // This happens when the patch from Fainting.cs suppresses the method.
          // Ignore it.
          return;
        }

        if (hud.m_level3 == null) {
          // This is true of bosses.
          return;
        }

        // Make a replacement for the level 3 GUI, anchored to the name UI.
        var replacement = new GameObject("Shiny image");
        replacement.transform.SetParent(hud.m_name.transform);

        // Set up a rectangle for the shiny icon.
        var imageRect = replacement.AddComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = new Vector2(0.5f, 24.0f);  // Above name
        imageRect.sizeDelta = new Vector2(24.0f, 24.0f);

        // Place the icon in that rectangle.
        var image = replacement.AddComponent<Image>();
        image.sprite = ShinyHudIcon;

        // Deactivate the original level3 UI and replace it with ours.
        hud.m_level3.gameObject.SetActive(false);
        hud.m_level3 = imageRect;
      }
    }

    // The spawn rates for level 2+ monsters are inconsistent.
    // Currently, some spawners use a hard-coded number like 10%, while others
    // use a member variable.  We want to force them all to use the same
    // number, which is one we control.
    // This patch is general enough to apply to all three types.
    [HarmonyPatch(typeof(CreatureSpawner), nameof(CreatureSpawner.Spawn))]
    [HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.SpawnOne))]
    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Spawn))]
    class ShinySpawnRate_Patch {
      static IEnumerable<CodeInstruction> Transpiler(
          IEnumerable<CodeInstruction> instructions) {
        return new CodeMatcher(instructions)
            .MatchForward(/* cursor at start of match */ false,
                // Random number from 0-100
                new CodeMatch(OpCodes.Ldc_R4, 0f),
                new CodeMatch(OpCodes.Ldc_R4, 100f),
                new CodeMatch(OpCodes.Call, AccessTools.Method(
                    typeof(UnityEngine.Random),
                    nameof(UnityEngine.Random.Range),
                    new Type[] { typeof(float), typeof(float) })))
            // Next the target code will load a constant or member variable for
            // comparison to the random number.  This is the percent chance to
            // spawn at level 2+.  We don't care what it was, or what the
            // instructions looked like, so we seek forward to the BLE
            // instruction afterward.
            .MatchForward(/* cursor at start of match */ false,
                new CodeMatch(OpCodes.Ble))
            // Now just before the BLE, we pop the comparison value from the
            // stack and insert our own value.
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldc_R4, shinySpawnRatePercent))
            .InstructionEnumeration();
      }
    }

    // If a monster already exists at level 2, map it to level 3 on reload.
    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    class MapMonsterLevelOnReload_Patch {
      static void Postfix(Character __instance) {
        var monster = __instance;
        if (monster.m_level == 2) monster.SetLevel(3);
      }
    }

    // If a new monster is spawned at level 2, map it to level 3 when the
    // spawner calls SetLevel().
    [HarmonyPatch(typeof(Character), nameof(Character.SetLevel))]
    class MapMonsterLevelOnSpawn_Patch {
      static void Prefix(ref int level) {
        if (level == 2) level = 3;
      }
    }

    // If an item already exists in your inventory at level 2, map it to level 3
    // and combine it with any existing level 3 items.  This also applies to
    // chests.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load))]
    class CombineLevelsInInventory_Patch {
      static void Postfix(Inventory __instance) {
        var inventory = __instance;
        // Make a copy so we can modify the inventory in the loop.
        var list = new List<ItemDrop.ItemData>(inventory.GetAllItems());
        foreach (var item in list) {
          if (item.IsInhabitedBall()) {
            var inhabitant = item.GetInhabitant();
            if (inhabitant.Level == 2) {
              // Remove it from the inventory.
              inventory.RemoveItem(item);

              // Create an equivalent item at level 3.
              var replacementItem = BallItem.UpgradeInhabitant(item, 3);

              // Add that to the inventory, where it will naturally stack with
              // any existing level 3 items.
              inventory.AddItem(replacementItem);
            }
          }
        }
      }
    }
  }
}
