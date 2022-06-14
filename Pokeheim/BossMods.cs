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
using Jotunn.Managers;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class BossMods {
    [PokeheimInit]
    public static void Init() {
      Utils.OnVanillaLocationsAvailable += delegate {
        // Add a Vegvisir to each boss site, pointing to the next boss site.
        var temple = ZoneManager.Instance.GetZoneLocation("StartTemple");
        var prefab =
            temple.m_prefab.GetComponentInChildren<Vegvisir>().gameObject;

        AddVegvisir(prefab, "Eikthyrnir", "GDKing", "$enemy_gdking",
            overridePosition: true,
            new Vector3(-7.0f, 0.8f, -0.4f));

        AddVegvisir(prefab, "GDKing", "Bonemass", "$enemy_bonemass");

        AddVegvisir(prefab, "Bonemass", "Dragonqueen", "$enemy_dragon");

        AddVegvisir(prefab, "Dragonqueen", "GoblinKing", "$enemy_goblinking",
            overridePosition: true,
            new Vector3(0f, 1.5f, 5f),
            overrideRotation: true,
            Quaternion.identity);

        AddVegvisir(prefab, "GoblinKing", null, null);
      };
    }

    private static void AddVegvisir(
        GameObject prefab,
        string locationName,
        string nextLocationName,
        string nextEnemyName,
        bool overridePosition = false,
        Vector3 position = default(Vector3),
        bool overrideRotation = false,
        Quaternion rotation = default(Quaternion)) {
      var locationObject = Utils.GetSpawnedLocationOrPrefab(locationName);

      if (locationObject.GetComponentInChildren<Vegvisir>() != null) {
        Logger.LogDebug($"{locationName} already has a Vegvisir!");
        return;
      }

      // Find the runestone with the boss's altar instructions.
      var runeStone =
          locationObject.transform.GetComponentInChildren<RuneStone>();
      if (runeStone == null) {
        Logger.LogError($"Stone not found in {locationName}!");
        return;
      }

      // Copy the position and rotation of the original stone if needed.
      if (overridePosition == false) {
        position = runeStone.transform.localPosition;
      }
      if (overrideRotation == false) {
        rotation = runeStone.transform.localRotation;
      }

      // Deactivate the original stone.
      runeStone.gameObject.SetActive(false);

      GameObject vegvisir = null;
      if (nextLocationName != null) {
        // Create a new Vegvisir pointing to the next boss.
        vegvisir = UnityEngine.Object.Instantiate(
              prefab, locationObject.transform);
        vegvisir.name = $"Vegvisir_{nextLocationName}";

        vegvisir.transform.localPosition = position;
        vegvisir.transform.localRotation = rotation;

        vegvisir.GetComponent<Vegvisir>().m_locationName = nextLocationName;
        vegvisir.GetComponent<Vegvisir>().m_pinName = nextEnemyName;

        Logger.LogDebug($"Placed Vegvisir {vegvisir} at {position}");
      }

      // For some reason, removing item stands here does not work.
    }

    // Allow spawning bosses at altars without any specific items, but only one
    // at a time, and not if you already have one in a ball.
    [HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.Interact))]
    class BossSpawn_Patch {
      static void Postfix(OfferingBowl __instance, Humanoid user, bool hold) {
        // I don't know what "hold" does, but these Interact() methods all
        // short-circuit if hold is true.  This one also short-circuits if a
        // boss is already spawning.
        var altar = __instance;
        if (hold || altar.IsBossSpawnQueued()) {
          return;
        }

        foreach (var monster in Character.GetAllCharacters()) {
          if (monster.GetPrefabName() == altar.m_bossPrefab.name) {
            // There is already one in the world.
            return;
          }
        }

        foreach (var item in user.m_inventory.GetAllItems()) {
          if (item.GetInhabitant()?.PrefabName == altar.m_bossPrefab.name) {
            // The user already has one in a ball.
            user.Message(MessageHud.MessageType.Center, "$boss_already_caught");
            return;
          }
        }

        if (altar.m_itemSpawnPoint != null) {
          altar.m_fuelAddedEffects.Create(
              altar.m_itemSpawnPoint.position,
              altar.transform.rotation);
        }

        altar.SpawnBoss(altar.transform.position);
        user.Message(
            MessageHud.MessageType.Center,
            "<color=yellow>$boss_spawn</color>");
      }
    }

    // Ignore items "used" at the altar.  This prevents Interact() from doing
    // anything, which lets us patch it with a Postfix.
    [HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.Awake))]
    class BossSpawnItem_Patch {
      static void Postfix(OfferingBowl __instance) {
        var altar = __instance;
        altar.m_useItemStands = false;
      }
    }

    // For some reason, we can't deactivate these item stands by modifying the
    // Location during startup.  Instead, we wait for the ItemStand to wake up,
    // check if it's part of a Location, then disable it.
    [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.Awake))]
    class DeactivateItemStandsInSpecialLocations_Patch {
      static void Postfix(ItemStand __instance) {
        var stand = __instance;
        if (stand.gameObject.GetComponentsInParent<Location>() != null) {
          // This object is part of a "location", such as the starting location
          // or a boss altar.  Deactivate it.  It may still be visible, though.
          stand.gameObject.SetActive(false);
        }
      }
    }

    // Update the hover text of altars to match the new logic above.
    [HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.GetHoverText))]
    class BossSpawnPointHover_Patch {
      static string Postfix(string originalResult, OfferingBowl __instance) {
        var altar = __instance;
        return Localization.instance.Localize(altar.m_name + "\n" +
            "[<color=yellow><b>$KEY_Use</b></color>] $boss_begin_raid");
      }
    }

    // Normally, a Vegvisir shows you the closest location to itself.
    // Instead, show the closest location to the starting location.
    // Since the player will have to circle back to the temple at the end of
    // the game, this is helpful.
    [HarmonyPatch(typeof(Vegvisir), nameof(Vegvisir.Interact))]
    class FindBossesCloseToTemple_Patch {
      public static Vector3 getTemplePosition() {
        Vector3 position;
        ZoneSystem.instance.GetLocationIcon("StartTemple", out position);
        return position;
      }

      static IEnumerable<CodeInstruction> Transpiler(
          IEnumerable<CodeInstruction> instructions,
          ILGenerator generator) {
        var getPositionMethod = typeof(Transform).GetMethod("get_position");

        var getTemplePositionMethod =
            typeof(FindBossesCloseToTemple_Patch).GetMethod(
                "getTemplePosition",
                BindingFlags.Static | BindingFlags.Public);

        var phases = new TranspilerSequence.Phase[] {
          new TranspilerSequence.Phase {
            matcher = code => (code.opcode == OpCodes.Callvirt &&
                               (code.operand as MethodInfo) == getPositionMethod),
            replacer = code => new CodeInstruction[] {
              // After getting the position of this Vegvisir,
              code,
              // Pop it from the stack,
              new CodeInstruction(OpCodes.Pop),
              // And get the position of the starting temple instead.
              new CodeInstruction(OpCodes.Call, getTemplePositionMethod),
            },
          },
        };

        return TranspilerSequence.Execute(
            "Vegvisir.Interact", phases, instructions);
      }
    }
  }
}
