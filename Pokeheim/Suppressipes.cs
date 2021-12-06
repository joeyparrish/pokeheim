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
      "Recipe_ArrowFlint",
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
  }
}
