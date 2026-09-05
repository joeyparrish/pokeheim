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
using UnityEngine;
using UnityEngine.SceneManagement;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  // Shows our version on the main menu, under Jotunn's, and hides the game's
  // own version label there.
  //
  // The game's label is small, low contrast, and sits at the bottom of a
  // column of other text, which makes it hard to read.  We still patch
  // Version.GetVersionString elsewhere, because that feeds the logs and the
  // multiplayer version check; this only changes what the main menu shows.
  [Feature(Features.MenuVersion)]
  public static class MenuVersion {
    // Hide the game's version label on the main menu.
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    class HideNativeVersion_Patch {
      static void Postfix(FejdStartup __instance) {
        var label = __instance.m_versionLabel;
        if (label == null) {
          Logger.LogWarning("No version label to hide on the main menu.");
          return;
        }
        label.gameObject.SetActive(false);
      }
    }

    [PokeheimInit]
    public static void Init() {
      var holder = new GameObject("PokeheimVersionLabel");
      UnityEngine.Object.DontDestroyOnLoad(holder);
      holder.AddComponent<VersionLabel>();
    }

    // Drawn with IMGUI rather than built into the UI hierarchy, to match how
    // Jotunn draws the label we are sitting underneath.
    class VersionLabel : MonoBehaviour {
      // Jotunn's DebugHelper.OnGUI draws "Jötunn v<version>" into
      // Rect(Screen.width - 100, 5, 100, 25) while the "start" scene is
      // active.  We mirror that and drop one line below it.
      //
      // Their rectangle cannot be read at runtime, so this offset is a
      // hardcoded assumption about their layout.  If they move their label,
      // ours ends up visibly out of place rather than quietly wrong, which is
      // the failure we would rather have.
      private const float Width = 100f;
      private const float Height = 25f;
      private const float JotunnTop = 5f;

      private void OnGUI() {
        // Only on the main menu, matching Jotunn.
        if (SceneManager.GetActiveScene().name != "start") {
          return;
        }

        UnityEngine.GUI.Label(
            new Rect(Screen.width - Width, JotunnTop + Height, Width, Height),
            "Pokéheim v" + PokeheimMod.PluginVersion);
      }
    }
  }
}
