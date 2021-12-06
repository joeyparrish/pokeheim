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
      new Contents {
        PrefabName = "ArrowFlint",
        Min = 10,
        Max = 50,
        Weight = 2f,
      },
      new Contents {
        PrefabName = "LeatherScraps",
        Min = 3,
        Max = 13,
        Weight = 2f,
      },
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
      new Contents {
        PrefabName = "TrophyDeer",
        Min = 1,
        Max = 1,
        Weight = 0.03f,
      },
      new Contents {
        PrefabName = "CheapBow",
        Min = 1,
        Max = 1,
        Weight = 0.03f,
      },
      new Contents {
        PrefabName = "SaddleUniversal",
        Min = 1,
        Max = 1,
        Weight = 0.03f,
      },
    };

    [PokeheimInit]
    public static void Init() {
      Utils.OnFirstSceneStart += delegate {
        foreach (var prefab in ZNetScene.instance.m_prefabs) {
          var container = prefab.GetComponent<Container>();
          if (container != null) {
            ReplaceContainerContents(
                container, $"prefab container");
          }
        }
      };

      Utils.OnVanillaLocationsAvailable += delegate {
        foreach (var location in ZoneSystem.instance.m_locationsByHash.Values) {
          var containers =
              location.m_prefab.GetComponentsInChildren<Container>();
          foreach (var container in containers) {
            ReplaceContainerContents(
                container, $"location {location.m_prefabName}");
          }
        }
      };
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
