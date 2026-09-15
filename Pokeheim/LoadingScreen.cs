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
using UnityEngine.UI;

namespace Pokeheim {
  [Feature(Features.LoadingScreen)]
  public static class LoadingScreen {
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    class TweakLoadingScreen_Patch {
      static void Postfix(Hud __instance) {
        // Update the tips.
        __instance.m_loadingTips = Utils.GenerateStringList(
            "$pokeheim_loadscreen_tip", 14);

        // Update the logo.
        var image = __instance.m_loadingProgress.transform.Find("text_darken/Logotype");
        image.GetComponent<Image>().sprite = Utils.LoadSprite("Logo.png");
      }
    }
  }
}
