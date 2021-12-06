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
using System.Collections.Generic;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class Giovanni {
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
      // TODO: Battle Giovanni & Halstein.  Make Halstein a shadow Pokemon.
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
