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
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class Giovanni {
    private static ParticleSystem BlobParticlePrefab = null;
    private static ParticleSystem ShadowSmoke = null;

    [PokeheimInit]
    public static void Init() {
      Utils.OnVanillaPrefabsAvailable += delegate {
        var prefab = PrefabManager.Instance.GetPrefab("Blob");
        var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>();
        foreach (var system in particleSystems) {
          if (system.gameObject.name == "wetsplsh") {
            BlobParticlePrefab = system;
            Logger.LogDebug($"Found prefab for ShadowSmoke: {BlobParticlePrefab}");
          }
        }
      };

      Utils.OnVanillaLocationsAvailable += delegate {
        // Find Halstein.  He only exists as a MonoBehaviour with a HoverText
        // component attached.
        var locationObject =
            Utils.GetSpawnedLocationOrPrefab("Vendor_BlackForest");
        foreach (var hover in locationObject.GetComponentsInChildren<HoverText>()) {
          if (hover.m_text == "$npc_halstein") {
            // Rename him.
            hover.m_text = "$npc_persian";
            Logger.LogDebug($"Renamed Halstein: {hover}");

            // Attach "shadow smoke" to him.
            ShadowSmoke = UnityEngine.Object.Instantiate(
                BlobParticlePrefab, hover.transform)
                    .GetComponent<ParticleSystem>();
            // Scale it up to Lox size.
            ShadowSmoke.transform.localScale *= 3.0f;
            // Raise it a little off the ground.
            ShadowSmoke.transform.localPosition += new Vector3(0f, 1f, 0f);
            // And make it purple.  Although a color picker told me the color I
            // want was about (0.4, 0.1, 0.8), for whatever reason, this is
            // what actually looks right in-game.
            var main = ShadowSmoke.main;
            main.startColor = new Color(0.1f, 0f, 1f);
            Logger.LogDebug($"Shadow smoke added to Halstein: {ShadowSmoke}");
            return;
          }
        }
        Logger.LogError("Unable to locate Halstein!");
      };
    }

    [HarmonyPatch]
    class Giovanni_Patch {
      // Rename Haldor.
      [HarmonyPatch(typeof(Trader), nameof(Trader.GetHoverName))]
      [HarmonyPatch(typeof(Trader), nameof(Trader.GetHoverText))]
      [HarmonyPostfix]
      static string replaceName(string originalReturn) {
        return Localization.instance.Localize("$npc_giovanni");
      }

      // Make it so that you can't interact with him.
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

        trader.m_randomTalk = Utils.GenerateStringList(
            "$npc_giovanni_smalltalk", 13);

        trader.m_randomGreets = Utils.GenerateStringList(
            "$npc_giovanni_greeting", 9);

        trader.m_randomGoodbye = Utils.GenerateStringList(
            "$npc_giovanni_goodbye", 5);
      }
    }
  }
}
