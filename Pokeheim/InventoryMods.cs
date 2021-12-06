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
using UnityEngine;

namespace Pokeheim {
  public class InventoryMods {
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Awake))]
    class LargeInventory_Patch {
      static void Postfix(Humanoid __instance) {
        // Normally 4 rows, now 8.
        __instance.m_inventory.m_height = 8;
      }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    class LargeInventoryBackground_Patch {
      static void Postfix(InventoryGui __instance) {
        var gui = __instance;

        var playerInventoryBackground =
            gui.m_player.Find("Bkg").GetComponent<RectTransform>();
        // Double the size of the background to match m_height above.
        // Because this is stretching downward, and the origin is the
        // bottom-left, we need to move the min anchor down by 1x.
        playerInventoryBackground.anchorMin = new Vector2(0f, -1f);

        // Move the container inventory GUI down by the same amount we expanded
        // the player inventory GUI.
        gui.m_container.anchoredPosition -= new Vector2(0f, gui.m_player.rect.height);
      }
    }
  }
}
