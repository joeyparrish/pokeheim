﻿/**
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

using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class SerpentMods {
    [PokeheimInit]
    public static void Init() {
      // Spawn serpents closer to shore to make it more reasonable to find and
      // catch them.
      Utils.OnVanillaCreaturesAvailable += delegate {
        var shorelineSerpentConfig = new CreatureConfig();
        shorelineSerpentConfig.AddSpawnConfig(new SpawnConfig {
          Name = "ShorelineSerpent",

          SpawnChance = 1f,  // percentage chance
          SpawnInterval = 100f,  // seconds between spawns
          SpawnDistance = 100f,  // between instances

          // All biomes, but only when there's water at least 3m deep:
          Biome = (Heightmap.Biome)0xffff,
          BiomeArea = Heightmap.BiomeArea.Everything,
          MinOceanDepth = 3f,
          MaxOceanDepth = 1e9f,
        });
        shorelineSerpentConfig.Faction = Character.Faction.SeaMonsters;

        var prefab = CreatureManager.Instance.GetCreaturePrefab("Serpent");

        var shorelineSerpent = new CustomCreature(
            prefab, fixReference: false, shorelineSerpentConfig);

        CreatureManager.Instance.AddCreature(shorelineSerpent);
      };
    }
  }
}
