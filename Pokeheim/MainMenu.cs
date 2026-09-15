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
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  // Shows our version on the main menu, under Jotunn's, and hides the game's
  // own version label there.
  //
  // The game's label is small, low contrast, and sits at the bottom of a
  // column of other text, which makes it hard to read.  We still patch
  // Version.GetVersionString elsewhere, because that feeds the logs and the
  // multiplayer version check; this only changes what the main menu shows.
  [Feature(Features.MainMenu)]
  public static class MainMenu {
    [PokeheimInit]
    public static void Init() {
      var holder = new GameObject("PokeheimVersionLabel");
      UnityEngine.Object.DontDestroyOnLoad(holder);
      holder.AddComponent<VersionLabel>();
    }

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

    // Drawn with IMGUI rather than built into the UI hierarchy, to match how
    // Jotunn draws the label we are sitting underneath.
    class VersionLabel : MonoBehaviour {
      // Jotunn's DebugHelper.OnGUI draws "Jötunn v<version>" into
      // Rect(Screen.width - 100, 5, 100, 25) while the "start" scene is
      // active.  We mirror that and drop one line below it.
      //
      // Their rectangle cannot be read at runtime, so these are hardcoded
      // assumptions about their layout.  If they move their label, ours ends
      // up visibly out of place rather than quietly wrong, which is the
      // failure we would rather have.
      private const float JotunnWidth = 100f;
      private const float JotunnHeight = 25f;
      private const float JotunnTop = 5f;

      // How close the label may come to the right edge of the screen when a
      // long version pushes it out past Jotunn's box.
      private const float RightMargin = 5f;

      private GUIStyle style = null;

      private void OnGUI() {
        // Only on the main menu, matching Jotunn.
        if (SceneManager.GetActiveScene().name != "start") {
          return;
        }

        // GUI.skin is only valid inside OnGUI, so build this on first use
        // rather than in Init().
        if (style == null) {
          style = new GUIStyle(UnityEngine.GUI.skin.label);
          // The default label style wraps.  In a box this size, a version like
          // "3.10.10" wraps to a second line and is then cut off part way down
          // it, so measure and place the text ourselves instead.
          style.wordWrap = false;
        }

        var content = new GUIContent(
            "Pokéheim v" + PokeheimMod.PluginVersion);
        var size = style.CalcSize(content);

        // Line up with Jotunn's label, but slide left instead of running off
        // the edge of the screen when our version is longer than fits.
        var x = Mathf.Min(
            Screen.width - JotunnWidth,
            Screen.width - size.x - RightMargin);

        UnityEngine.GUI.Label(
            new Rect(x, JotunnTop + JotunnHeight, size.x, size.y),
            content,
            style);
      }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    class ReplaceLogo_Patch {
      static void Postfix(FejdStartup __instance) {
        var menu = __instance.m_mainMenu;
        if (menu == null) {
          Jotunn.Logger.LogError("No main menu; cannot replace the logo.");
          return;
        }

        // "Logo" is a container.  As of 2026 it holds the logo itself, several
        // drifting ember effects, and a parked, inactive variant for each
        // themed update: Ashlands, Mistlands, and the old Hearth & Home badge.
        // Transform.LogHierarchy below will show you the current shape of it.
        var container = menu.transform.FindChildIgnoringCase("Logo");
        if (container == null) {
          Jotunn.Logger.LogError(
              "No \"Logo\" under the main menu.  Its hierarchy is:");
          menu.transform.LogHierarchy();
          return;
        }

        // Match on being active rather than on a fixed path.  The themed
        // variants exist because the game swaps which logo is live, so if a
        // promotion switches "LOGO" off and "AshlandsLogo" on, we want to
        // follow it rather than silently paint a hidden object, which is
        // indistinguishable from doing nothing.  Only active objects are
        // considered, which also excludes the parked variants.
        Image image = null;
        foreach (var candidate in
                 container.GetComponentsInChildren<Image>(includeInactive: false)) {
          var name = candidate.name;
          if (name.IndexOf("logo", StringComparison.OrdinalIgnoreCase) < 0) {
            // Embers and other decoration.
            continue;
          }
          if (name.IndexOf("glow", StringComparison.OrdinalIgnoreCase) >= 0) {
            // The themed logos come with a separate glow layer behind them.
            continue;
          }
          image = candidate;
          break;
        }

        if (image == null) {
          Jotunn.Logger.LogError(
              "Found no active logo Image.  The Logo hierarchy is:");
          container.LogHierarchy();
          return;
        }

        Jotunn.Logger.LogDebug($"Replacing the logo on \"{image.name}\".");
        image.sprite = Utils.LoadSprite("Logo.png");
      }
    }

    // Add our version number to the game's built-in version number.
    [HarmonyPatch(typeof(Version), nameof(Version.GetVersionString))]
    class VersionString_Patch {
      static void Postfix(ref string __result) {
        __result += " " + PokeheimMod.PluginName +
                    " " + PokeheimMod.PluginVersion;
      }
    }
  }  // public static class MainMenu
}
