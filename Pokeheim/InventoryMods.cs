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
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public class InventoryMods {
    private const string SortIconPath = "Sort icon.png";

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

    static int CompareItems(ItemDrop.ItemData a, ItemDrop.ItemData b) {
      // Balls first.
      if (a.IsBall() && !b.IsBall()) {
        return -1;
      }
      if (!a.IsBall() && b.IsBall()) {
        return 1;
      }

      if (a.IsBall() && b.IsBall()) {
        var aInhabitant = a.GetInhabitant();
        var bInhabitant = b.GetInhabitant();
        // Uninhabited balls first.
        if (aInhabitant == null && bInhabitant != null) {
          return -1;
        }
        if (aInhabitant != null && bInhabitant == null) {
          return 1;
        }

        // Sort uninhabited balls by type, going up by strength.
        if (aInhabitant == null && bInhabitant == null) {
          return a.BallFactor().CompareTo(b.BallFactor());
        }

        // Sort inhabited balls by monster inside.
        // This is Pokedex order.
        return aInhabitant.CompareTo(bInhabitant);
      }

      // Sort non-ball items.

      // Non-equippable items first.
      if (!a.IsEquipable() && b.IsEquipable()) {
        return -1;
      }
      if (a.IsEquipable() && !b.IsEquipable()) {
        return 1;
      }

      // Equippable, but non-equipped items next.
      if (!a.m_equiped && b.m_equiped) {
        return -1;
      }
      if (a.m_equiped && !b.m_equiped) {
        return 1;
      }

      // Equipped items last.

      // For non-ball items that are tied (both non-equippable, both
      // equippable, or both equipped), break ties by name.
      return a.m_shared.m_name.CompareTo(b.m_shared.m_name);
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    class ChangeArmorDisplayToSortButton_Patch {
      static void Postfix(InventoryGui __instance) {
        var gui = __instance;

        // The Text element that shows the current armor level.
        var armorText = gui.m_armor;
        // The rectangle behind it.
        var armorRect = armorText.transform.parent;
        // The armor icon above the text.
        var armorIcon = armorRect.Find("armor_icon").GetComponent<Image>();

        // Disable the original text.
        armorText.gameObject.SetActive(false);

        // Replace the icon.
        armorIcon.sprite = Utils.LoadSprite(SortIconPath);

        // Center the icon.
        armorIcon.rectTransform.anchoredPosition = new Vector2(0.5f, 0.5f);

        // Make the icon clickable.
        var sortButton = armorIcon.gameObject.AddComponent<Button>();

        // For some reason, the tooltip prefab on UITooltip isn't static.  So
        // grab another UITooltip _BEFORE_ adding ours.
        var otherTip = gui.m_inventoryRoot.GetComponentInChildren<UITooltip>();

        // Add a tooltip, which requires Selectable (Button) be added first.
        var tooltip = armorIcon.gameObject.AddComponent<UITooltip>();
        tooltip.m_tooltipPrefab = otherTip.m_tooltipPrefab;
        tooltip.Set("", "$sort_items");

        // On click, sort the inventory after the first row.
        sortButton.onClick.AddListener(delegate {
          var sortable = new List<ItemDrop.ItemData>();

          foreach (var item in Player.m_localPlayer.m_inventory.GetAllItems()) {
            // We only sort the items that are past the first row.  We don't
            // want to mess with the player's numbered items.
            if (item.m_gridPos.y > 0) {
              sortable.Add(item);
            }
          }

          sortable.Sort(CompareItems);

          // Lay out the items in order, starting in the second row of the
          // inventory.
          var y = 1;
          var x = 0;
          var width = Player.m_localPlayer.m_inventory.m_width;

          foreach (var item in sortable) {
            item.m_gridPos.y = y;
            item.m_gridPos.x = x;
            x++;
            if (x >= width) {
              y++;
              x = 0;
            }
          }
        });
      }
    }
  }
}
