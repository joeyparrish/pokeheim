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

using Logger = Jotunn.Logger;

namespace Pokeheim {
  // TODO: Now Odin is here... what's the final interaction?
  public static class OdinMods {
    // These are keys which will be used to store additional fields in ZDO.
    private const string IsStaticKey = "com.pokeheim.IsStatic";

    private static Odin staticOdin = null;

    public static void SpawnStaticOdin() {
      // Just because he's the All-Father doesn't mean you can have as many of
      // him as you want.
      if (staticOdin != null && staticOdin.enabled) {
        return;
      }

      Vector3 templePosition;
      ZoneSystem.instance.GetLocationIcon("StartTemple", out templePosition);

      var odinPrefab = PrefabManager.Instance.GetPrefab("odin");
      var rotation = Quaternion.identity;
      var clone = UnityEngine.Object.Instantiate(
          odinPrefab, templePosition, rotation);

      staticOdin = clone.GetComponent<Odin>();
      staticOdin.SetStatic();
      Logger.LogDebug($"Spawned static Odin: {staticOdin}");
    }

    public static void SetStatic(this Odin odin) {
      odin.SetExtraData(IsStaticKey, true);

      // Make static Odin impossible to push around by removing his physics.
      var body = odin.GetComponent<Rigidbody>();
      if (body != null) {
         UnityEngine.Object.Destroy(body);
      }
    }

    public static bool IsStatic(this Odin odin) {
      return odin.GetExtraData(IsStaticKey, false);
    }

    // Find existing static Odin characters when we reload the game.
    [HarmonyPatch(typeof(Odin), nameof(Odin.Awake))]
    class TrackOdin_Patch {
      static void Postfix(Odin __instance) {
        Odin odin = __instance;

        if (odin.IsStatic()) {
          staticOdin = odin;
        }
      }
    }

    // Change the Update() logic for a static Odin.
    [HarmonyPatch(typeof(Odin), nameof(Odin.Update))]
    class OdinCanBeStatic_Patch {
      static bool Prefix(Odin __instance) {
        Odin odin = __instance;

        if (odin.IsStatic() == false) {
          // Call the original method.
          return true;
        }

        // This is a fork of Odin's Update() method that does everything except
        // disappear.
        if (odin.m_nview != null && odin.m_nview.IsOwner()) {
          // Face the closest player.
          Player closestPlayer = Player.GetClosestPlayer(
              odin.transform.position, 100f);
          if (closestPlayer != null) {
            Vector3 forward =
                closestPlayer.transform.position - odin.transform.position;
            forward.y = 0f;
            forward.Normalize();
            odin.transform.rotation = Quaternion.LookRotation(forward);
          }
        }

        // Suppress the original.
        return false;
      }
    }
  }
}
