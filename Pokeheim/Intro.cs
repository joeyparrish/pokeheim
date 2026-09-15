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
  }
}
