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

using Logger = Jotunn.Logger;

namespace Pokeheim {
  [Feature(Features.Intro)]
  public static class Intro {
    private static string PokeheimIntroFlag = "com.pokeheim.IntroSeen";

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    class Intro_Patch {
      static void Prefix(Player __instance, ref bool spawnValkyrie) {
        var player = __instance;

        // Skip the Valkyrie.  The game takes this as a parameter now, so we
        // no longer have to clear the prefab reference ourselves.  (It became
        // a SoftReference, which cannot be nulled anyway.)
        spawnValkyrie = false;

        // but force our own version of the intro text for player who are new
        // to Pokeheim.
        if (player.HaveUniqueKey(PokeheimIntroFlag) == false) {
          player.AddUniqueKey(PokeheimIntroFlag);

          // Clear the tutorial flags and log book, too, since we've replaced
          // the content of those in Pokeheim.
          player.m_knownTexts.Clear();
          player.m_shownTutorials.Clear();

          TextViewer.instance.ShowText(
              TextViewer.Style.Intro,
              "INTRO",
              "$pokeheim_intro",
              autoHide: false);
        }
      }
    }

    // Suppress the game's own intro scroll, which we replace above.
    //
    // We never had to do this before.  Until 1.0 the intro text was shown by
    // Valkyrie.ShowText(), so declining the Valkyrie removed the text as a side
    // effect.  1.0 moved the text to Game.ShowIntro(), queued whenever the
    // profile reports a first spawn, and decoupled it from the Valkyrie
    // entirely.  Skipping the Valkyrie therefore no longer skips the text, and
    // a new character got two scrolls: the game's, then ours.
    //
    // Skipping the body is safe.  Its caller sets m_inIntro before deciding
    // between the cinematic and this, and that flag is what the spawn logic
    // reads afterwards, so the only thing lost is the text itself.
    [HarmonyPatch(typeof(Game), nameof(Game.ShowIntro))]
    class SuppressVanillaIntro_Patch {
      static bool Prefix() {
        Logger.LogInfo("Suppressing the game's intro scroll.");
        return false;
      }
    }

    // Valheim 1.0 added pre-rendered cinematics: an intro at startup and again
    // on a new world, an outro and credits at the end of the game, and dream
    // videos when you sleep.  None of them fit Pokeheim, which tells its own
    // story, so starve the whole system rather than intercepting each trigger.
    [HarmonyPatch(typeof(CinematicsManager), nameof(CinematicsManager.Awake))]
    class SuppressCinematics_Patch {
      static void Postfix(CinematicsManager __instance) {
        // Both intro paths check these before they call Play().  Clearing them
        // matters most for the one at startup: letting Play() merely fail
        // there logs "Failed to play intro cinematic" as an error on every
        // launch.
        //
        // The new-world path falls through to ShowIntro(), the old text intro,
        // which is exactly what Intro_Patch above replaces.  So this restores
        // the behaviour Pokeheim was built around rather than fighting it.
        __instance.m_introOnStartup = false;
        __instance.m_introOnNewWorld = false;

        // Everything else reaches Play() through GetVideo(), which searches
        // this list.  With it empty, GetVideo() returns null and Play() bails
        // on its own null check, so we never have to patch Play itself.
        //
        // That matters: all three Play() overloads funnel into one, but
        // suppressing it directly would strand the main menu.  Its cinematics
        // viewer calls Play() and then hides the menu, relying on Play's
        // completion callback to bring it back.  Emptying the list instead
        // leaves nothing in the viewer to select in the first place, since it
        // is built by iterating this same list.
        __instance.m_videos.Clear();
      }
    }
  }
}
