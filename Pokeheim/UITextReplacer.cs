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
using UnityEngine.UI;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  // Annoyingly, some UI Text elements must be patched on every Update().
  // This was not true in November 2021, and it is true in May of 2022.
  // Somehow, Valheim changed in a way that makes this kind of patch more
  // difficult.  This encapsulates the concept and dynamically patches a class's
  // Update() method.  To keep things lightweight, the caller must precompute
  // the actual element to modify, and provide a boolean callback to decide
  // when to modify it.  The caller can change the actual replacement text at
  // any time, as appropriate.
  public class UITextReplacer {
    // Typedef for a callback.
    public delegate bool ShouldUpdateCallbackType();

    // We can only patch with static methods, so we need a registry to track
    // the individual instances.
    // Key is a class whose Update() method has been patched.
    // Value is a list of replacers attached to that class.
    private static Dictionary<Type, List<UITextReplacer>> registry =
        new Dictionary<Type, List<UITextReplacer>>();

    public string text = "";

    private Text textElement;
    private ShouldUpdateCallbackType shouldUpdate;

    public UITextReplacer(
        Type updatingType, Text textElement,
        ShouldUpdateCallbackType shouldUpdate) {
      if (updatingType == null) {
        Logger.LogError("updatingType is null!");
        return;
      }
      if (textElement == null) {
        Logger.LogError("textElement is null!");
        return;
      }

      this.textElement = textElement;
      this.shouldUpdate = shouldUpdate;

      var updateMethod = updatingType.GetMethod(
          "Update",
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (updateMethod == null) {
        Logger.LogError($"Unable to find Update() on {updatingType}!");
        return;
      }

      if (!registry.ContainsKey(updatingType)) {
        registry.Add(updatingType, new List<UITextReplacer>());
      }
      registry[updatingType].Add(this);

      var onUpdatePatch = typeof(UITextReplacer).GetMethod(
          "OnUpdate",
          BindingFlags.Static | BindingFlags.NonPublic);
      PokeheimMod.harmony.Patch(
          updateMethod, postfix: new HarmonyMethod(onUpdatePatch));
    }

    private static void OnUpdate(System.Object __instance) {
      var type = __instance.GetType();
      foreach (var replacer in registry[type]) {
        replacer.UpdateHook();
      }
    }

    public void SetText(string text) {
      this.text = text;
    }

    public void UpdateHook() {
      if (shouldUpdate()) {
        textElement.text = text;
      }
    }
  }
}
