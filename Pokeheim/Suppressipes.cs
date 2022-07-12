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
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  // Only let the user build certain things.  We want to focus the game on
  // catching 'em all.  Basic weapons are allowed.  We also allow customized,
  // cheaper-to-build versions of certain built-ins.
  public static class Suppressipes {
    private static List<string> AllowedRecipeNames = new List<string> {
      "Recipe_ArrowWood",
      "Recipe_AxeStone",
      "Recipe_CheapBow",
      "Recipe_Club",
      "Recipe_Hammer",
      "Recipe_Hoe",
      "Recipe_PickaxeAntlerNoBoss",
      "Recipe_SaddleUniversal",  // See Riding.cs
      "Recipe_TorchNoResin",
    };

    private static List<string> AllowedBuildPieces = new List<string> {
      "$piece_firepit",
      "$piece_levelground",
      "$piece_raise",
    };

    // Allowed pickables.  All food is implicitly allowed, and doesn't need to
    // be listed here.  Any other pickable item will be disabled.
    private static List<string> AllowedPickables = new List<string> {
      "$item_wood",
      "$item_stone",
    };

    // Allowed drops.  Allowed pickables are implicitly allowed drops, and
    // trophies of all kind are also implicitly allowed.  Any other droppable
    // item will not appear in drops of any kind.
    private static List<string> AllowedDrops = new List<string> {
      "$item_leatherscraps",
    };

    [PokeheimInit]
    public static void Init() {
      Utils.OnVanillaPrefabsAvailable += delegate {
        AddCustomRecipes();
      };
    }

    private static void AddCustomRecipes() {
      var pickaxeConfig = new ItemConfig {
        Requirements = new[] {
          new RequirementConfig {
            Item = "Wood",
            Amount = 10,
          },
          new RequirementConfig {
            Item = "TrophyDeer",
            Amount = 1,
          },
        },
      };
      var customPickaxe = new CustomItem(
          "PickaxeAntlerNoBoss", "PickaxeAntler", pickaxeConfig);
      ItemManager.Instance.AddItem(customPickaxe);

      var torchConfig = new ItemConfig {
        Requirements = new[] {
          new RequirementConfig {
            Item = "Wood",
            Amount = 1,
          },
        },
      };
      var customTorch = new CustomItem("TorchNoResin", "Torch", torchConfig);
      ItemManager.Instance.AddItem(customTorch);

      var bowConfig = new ItemConfig {
        Requirements = new[] {
          new RequirementConfig {
            Item = "Wood",
            Amount = 10,
          },
          new RequirementConfig {
            Item = "LeatherScraps",
            Amount = 1,
          },
        },
      };
      var customBow = new CustomItem("CheapBow", "Bow", bowConfig);
      ItemManager.Instance.AddItem(customBow);
    }

    private static bool IsDropAllowed(GameObject gameObject) {
      if (gameObject == null) {
        return false;
      }

      var item = gameObject.GetComponent<ItemDrop>()?.m_itemData;
      if (item == null) {
        return false;
      }

      var itemType = item.m_shared.m_itemType;
      var isTrophy = itemType == ItemDrop.ItemData.ItemType.Trophie;
      var name = item.m_shared.m_name;

      return isTrophy ||
             AllowedPickables.Contains(name) ||
             AllowedDrops.Contains(name);
    }

    // Hide the recipes and build pieces we don't explicitly allow, and allow
    // the rest to be built without a workbench.
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateKnownRecipesList))]
    class HideRecipesAndDontRequireWorkbenches_Patch {
      static void Prefix(Player __instance) {
        var player = __instance;

        foreach (var recipe in ObjectDB.instance.m_recipes) {
          if (AllowedRecipeNames.Contains(recipe.name) ||
              BallItem.RecipeNames.Contains(recipe.name)) {
            recipe.m_craftingStation = null;
          } else {
            recipe.m_enabled = false;
          }
        }

        // Loop patterned after Player.UpdateKnownRecipesList().
        player.m_tempOwnedPieceTables.Clear();
        player.m_inventory.GetAllPieceTables(player.m_tempOwnedPieceTables);

        foreach (var tempOwnedPieceTable in player.m_tempOwnedPieceTables) {
          foreach (var gameObject in tempOwnedPieceTable.m_pieces) {
            var piece = gameObject.GetComponent<Piece>();
            if (AllowedBuildPieces.Contains(piece.m_name)) {
              piece.m_craftingStation = null;
            } else {
              piece.m_enabled = false;
            }
          }
        }
      }
    }

    // Anything we allow should be durable, since there are no workbenches for
    // repair.
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
    class InfinitelyDurable_Patch {
      static void Postfix(ItemDrop.ItemData ___m_itemData) {
        ___m_itemData.m_shared.m_useDurability = false;
      }
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Awake))]
    class HideUselessPickables_Patch {
      static void Postfix(Pickable __instance) {
        var pickable = __instance;
        var item = pickable.m_itemPrefab.GetComponent<ItemDrop>().m_itemData;
        var itemType = item.m_shared.m_itemType;
        var isFood = itemType == ItemDrop.ItemData.ItemType.Consumable;
        var name = item.m_shared.m_name;

        if (isFood) {
          // Food is always allowed.
          return;
        }

        if (AllowedPickables.Contains(name)) {
          return;
        }

        // Every other pickable is disabled.
        Logger.LogDebug($"Disabling pickable: " +
            $"{pickable.m_itemPrefab} name: {item.m_shared.m_name}");
        pickable.gameObject.SetActive(false);
      }
    }

    // These stands (like the ones that hold Surtling cores in the forest
    // caves) never having anything relevant Pokeheim.  Disable them.
    [HarmonyPatch(typeof(PickableItem), nameof(PickableItem.Awake))]
    class HideAllPickableStands_Patch {
      static void Postfix(PickableItem __instance) {
        __instance.SetupItem(enabled: false);
      }
    }

    // Remove useless item drops from all non-characters.
    [HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropList),
                  new Type[] {})]
    class FilterUselessDrops_Patch {
      static void Postfix(List<GameObject> __result) {
        var originalList = new List<GameObject>(__result);
        __result.Clear();

        foreach (var gameObject in originalList) {
          if (IsDropAllowed(gameObject)) {
            __result.Add(gameObject);
          } else {
            Logger.LogDebug($"Suppressing drop: {gameObject}");
          }
        }
      }
    }

    // Remove useless item drops from all characters.
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.Start))]
    class FilterUselessCharacterDrops_Patch {
      static void Postfix(CharacterDrop __instance) {
        var originalList = new List<CharacterDrop.Drop>(__instance.m_drops);
        __instance.m_drops.Clear();

        foreach (var drop in originalList) {
          if (IsDropAllowed(drop.m_prefab)) {
            __instance.m_drops.Add(drop);
          } else {
            Logger.LogDebug($"Suppressing character drop: {drop.m_prefab}");
          }
        }
      }
    }
  }
}
