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
using System;
using System.Collections.Generic;
using System.Reflection;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  // Transpilers often need to look for a specific sequence of instructions, in
  // order, to replace.  They also need to log errors if the unpatched code
  // changes in a way that breaks the sequence matching.  This utility allows
  // the caller to define a sequence of Phases, each of which has a matcher
  // callback and a replacer callback.  When the matcher matches the current
  // instruction, it is replaced by the output of the replacer.  Matches only
  // proceed in order of phases, and an error is logged if we don't complete
  // the final phase.
  public class TranspilerSequence {
    public delegate bool MatchInstructionCallbackType(CodeInstruction code);
    public delegate CodeInstruction[] InstructionReplacerCallbackType(CodeInstruction code);

    public struct Phase {
      public MatchInstructionCallbackType matcher;
      public InstructionReplacerCallbackType replacer;
    }

    public static IEnumerable<CodeInstruction> Execute(
        string label,
        Phase[] phases,
        IEnumerable<CodeInstruction> instructions) {
      int index = 0;
      foreach (var code in instructions) {
        if (index < phases.Length) {
          if (phases[index].matcher(code)) {
            foreach (var replacement in phases[index].replacer(code)) {
              yield return replacement;
            }
            index++;
          } else {
            yield return code;
          }
        } else {
          yield return code;
        }
      }

      if (index < phases.Length) {
        Logger.LogError($"Failed to patch {label}, {index} / {phases.Length} phases patched");
      }
    }  // Execute
  }  // public class TranspilerSequence
}
