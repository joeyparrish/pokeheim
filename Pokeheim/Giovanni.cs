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
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  [Feature(Features.Giovanni)]
  public static class Giovanni {
    private const string VendorLocation = "Vendor_BlackForest";

    private static ParticleSystem ParticlePrefab = null;

    [PokeheimInit]
    public static void Init() {
      Utils.OnVanillaPrefabsAvailable += delegate {
        var prefab = PrefabManager.Instance.GetPrefab("Blob");
        var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>();
        foreach (var system in particleSystems) {
          if (system.gameObject.name == "wetsplsh") {
            ParticlePrefab = system;
            Logger.LogInfo($"Found prefab for ShadowSmoke: {ParticlePrefab}");
          }
        }
        if (ParticlePrefab == null) {
          Logger.LogError("Unable to find prefab for ShadowSmoke");
        }
      };
    }

    // This has to happen on the spawned location rather than on the location
    // prefab, because the smoke relies on a spawned scene object.
    [HarmonyPatch(typeof(Location), nameof(Location.Awake))]
    class DressUpHalstein_Patch {
      static void Postfix(Location __instance) {
        // Spawned instances carry a "(Clone)" suffix.
        if (!__instance.gameObject.name.StartsWith(VendorLocation)) {
          return;
        }

        foreach (var petable in
                 __instance.GetComponentsInChildren<Petable>(includeInactive: true)) {
          petable.m_name = "$npc_persian";
          AddShadowSmoke(petable.transform);
          return;
        }

        Logger.LogWarning($"Unable to find Lox in {VendorLocation}");
      }
    }

    private static void AddShadowSmoke(Transform parent) {
      if (ParticlePrefab == null) {
        Logger.LogError("No particle prefab for shadow smoke.");
        return;
      }

      var smoke = UnityEngine.Object.Instantiate(ParticlePrefab, parent);
      // Raise it a little off the ground.
      smoke.transform.localPosition = new Vector3(0f, 1.5f, 0f);
      // Scale it up to Lox size.
      smoke.transform.localScale *= 3.0f;

      var main = smoke.main;
      // Without this, scaling the transform moves the emitter but not the
      // simulation, and the effect stays Blob-sized on a Lox.
      main.scalingMode = ParticleSystemScalingMode.Hierarchy;
      // Make it purple.  Although a color picker told me the color I want was
      // about (0.4, 0.1, 0.8), for whatever reason, this is what actually
      // looks right in-game.
      main.startColor = new Color(0.1f, 0f, 1f);

      smoke.Play();
    }

    // TODO: Split Haldor and Hildir
    [HarmonyPatch]
    class Giovanni_Patch {
      // Rename Haldor.
      [HarmonyPatch(typeof(Trader), nameof(Trader.GetHoverName))]
      [HarmonyPatch(typeof(Trader), nameof(Trader.GetHoverText))]
      [HarmonyPostfix]
      static void replaceName(Trader __instance, ref string __result) {
        // TODO: Name Hildir
        if (__instance.m_name == "$npc_haldor") {
          __result = Localization.instance.Localize("$npc_giovanni");
        } else {
          Logger.LogInfo($"Unrecognized Trader: {__instance.m_name}");
        }
      }

      // Make it so that you can't interact with them.
      [HarmonyPatch(typeof(Trader), nameof(Trader.Interact))]
      [HarmonyPrefix]
      static bool disableInteraction(ref bool __result) {
        __result = false;
        return false;
      }

      [HarmonyPatch(typeof(Trader), nameof(Trader.Start))]
      [HarmonyPostfix]
      static void replaceSpeech(Trader __instance) {
        var trader = __instance;

        // TODO: Dialog for Hildir
        if (__instance.m_name == "$npc_haldor") {
          trader.m_randomTalk = Utils.GenerateStringList(
              "$npc_giovanni_smalltalk", 13);

          trader.m_randomGreets = Utils.GenerateStringList(
              "$npc_giovanni_greeting", 9);

          trader.m_randomGoodbye = Utils.GenerateStringList(
              "$npc_giovanni_goodbye", 5);
        }

        // Make them chattier.  (30 => 15)
        trader.m_randomTalkInterval = 15;
      }
    }
  }
}
