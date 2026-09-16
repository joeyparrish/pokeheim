/**
 * Pokeheim - A Valheim Mod
 * Copyright (C) 2021-2026 Joey Parrish
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
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  // Replaces the contents of all chests found in the wild with items relevant
  // to Pokeheim.
  public static class ContainerMods {
    private class Contents {
      public string PrefabName;
      public int Min;
      public int Max;
      public float Weight;

      public DropTable.DropData ToDrop() {
        return new DropTable.DropData {
          m_item = PrefabManager.Instance.GetPrefab(PrefabName),
          m_stackMin = Min,
          m_stackMax = Max,
          m_weight = Weight,
        };
      }
    }

    private static List<Contents> ContainerOptions = new List<Contents> {
      // Pokeballs
      new Contents {
        PrefabName = "Pokeball",
        Min = 20,
        Max = 80,
        Weight = 4f,
      },
      new Contents {
        PrefabName = "Greatball",
        Min = 10,
        Max = 40,
        Weight = 2f,
      },
      new Contents {
        PrefabName = "Ultraball",
        Min = 5,
        Max = 20,
        Weight = 1f,
      },

      // Basic weapons
      new Contents {
        PrefabName = "ArrowWood",
        Min = 10,
        Max = 50,
        Weight = 2f,
      },
      new Contents {
        PrefabName = "CheapBow",
        Min = 1,
        Max = 1,
        Weight = 0.03f,
      },

      // Berries for monsters, and for making Pokeballs
      new Contents {
        PrefabName = "Raspberry",
        Min = 5,
        Max = 20,
        Weight = 2f,
      },
      new Contents {
        PrefabName = "Blueberries",
        Min = 5,
        Max = 20,
        Weight = 2f,
      },
      new Contents {
        PrefabName = "MushroomYellow",
        Min = 5,
        Max = 20,
        Weight = 1f,
      },
      new Contents {
        PrefabName = "CloudBerry",
        Min = 5,
        Max = 20,
        Weight = 0.1f,
      },

      // For making a saddle
      new Contents {
        PrefabName = "LeatherScraps",
        Min = 3,
        Max = 13,
        Weight = 2f,
      },

      // For making a pickaxe
      new Contents {
        PrefabName = "TrophyDeer",
        Min = 1,
        Max = 1,
        Weight = 0.03f,
      },

      // Very lucky: a saddle
      new Contents {
        PrefabName = "SaddleUniversal",
        Min = 1,
        Max = 1,
        Weight = 0.03f,
      },
    };

    // Every container fills itself from the same method, whether it came from a
    // location, a dungeon room, or the world, so one patch covers all of them.
    //
    // This used to be three passes that walked prefabs instead: ZNetScene's
    // prefab list, every location's prefab, and every dungeon room's prefab.
    // Two of those cannot work any more, because a location's and a room's
    // prefab are SoftReferences now, owned by the asset system and not
    // necessarily loaded when we would want to read them.  Patching the
    // instance is both simpler and more complete: it catches containers no
    // prefab walk would have reached.
    //
    // A prefix, because Awake() calls this to roll the contents, so a postfix
    // would arrive after the vanilla loot had already been generated.
    [HarmonyPatch(typeof(Container), nameof(Container.AddDefaultItems))]
    class ReplaceChestContents_Patch {
      static void Prefix(Container __instance) {
        ReplaceContainerContents(__instance, __instance.name);
      }
    }

    private static void ReplaceContainerContents(
        Container container, string source) {
      var table = container.m_defaultItems;
      if (table.m_drops.Count > 0) {
        Logger.LogDebug($"Updating contents of {container} from {source}");

        table.m_dropMin = 3;
        table.m_dropMax = 5;
        table.m_dropChance = 1f;
        table.m_oneOfEach = true;

        table.m_drops.Clear();
        foreach (var contents in ContainerOptions) {
          table.m_drops.Add(contents.ToDrop());
        }
      }
    }
  }
}
