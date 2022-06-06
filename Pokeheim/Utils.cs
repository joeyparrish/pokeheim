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
using Jotunn.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class Utils {
    // Other classes can register callbacks here without worrying about
    // unregistering.  Unlike PrefabManager/ZoneManager's event of the same
    // name, this will only be invoked once.  Use them to call methods like
    // StealFromPrefab, to register custom items, or to modify built-in
    // locations.
    public static event Action OnVanillaPrefabsAvailable;
    public static event Action OnVanillaLocationsAvailable;
    public static event Action OnFirstSceneStart;

    [PokeheimInit]
    public static void Init() {
      PrefabManager.OnVanillaPrefabsAvailable += NotifyVanillaPrefabsAvailable;
      ZoneManager.OnVanillaLocationsAvailable += NotifyVanillaLocationsAvailable;
      PrefabManager.OnPrefabsRegistered += NotifyFirstSceneStart;
    }

    private static void NotifyVanillaPrefabsAvailable() {
      PrefabManager.OnVanillaPrefabsAvailable -= NotifyVanillaPrefabsAvailable;

      foreach (Action callback in OnVanillaPrefabsAvailable.GetInvocationList()) {
        try {
          callback();
        } catch (Exception e) {
          Logger.LogWarning($"Exception thrown at event {(new StackFrame(1).GetMethod().Name)} in {callback.Method.DeclaringType.Name}.{callback.Method.Name}:\n{e}");
        }
      }
    }

    private static void NotifyVanillaLocationsAvailable() {
      ZoneManager.OnVanillaLocationsAvailable -= NotifyVanillaLocationsAvailable;

      foreach (Action callback in OnVanillaLocationsAvailable.GetInvocationList()) {
        try {
          callback();
        } catch (Exception e) {
          Logger.LogWarning($"Exception thrown at event {(new StackFrame(1).GetMethod().Name)} in {callback.Method.DeclaringType.Name}.{callback.Method.Name}:\n{e}");
        }
      }
    }

    private static void NotifyFirstSceneStart() {
      PrefabManager.OnPrefabsRegistered -= NotifyFirstSceneStart;

      foreach (Action callback in OnFirstSceneStart.GetInvocationList()) {
        try {
          callback();
        } catch (Exception e) {
          Logger.LogWarning($"Exception thrown at event {(new StackFrame(1).GetMethod().Name)} in {callback.Method.DeclaringType.Name}.{callback.Method.Name}:\n{e}");
        }
      }
    }

    private static ZDO GetZDO(Component thing) {
      var view = thing.GetComponent<ZNetView>();
      return view?.GetZDO();
    }

    public static bool GetExtraData(this Component thing, string key, bool defaultValue) {
      var zdo = GetZDO(thing);
      return zdo != null ? zdo.GetBool(key, defaultValue) : defaultValue;
    }

    public static string GetExtraData(this Component thing, string key, string defaultValue) {
      var zdo = GetZDO(thing);
      return zdo != null ? zdo.GetString(key, defaultValue) : defaultValue;
    }

    public static float GetExtraData(this Component thing, string key, float defaultValue) {
      var zdo = GetZDO(thing);
      return zdo != null ? zdo.GetFloat(key, defaultValue) : defaultValue;
    }

    public static long GetExtraData(this Component thing, string key, long defaultValue) {
      var zdo = GetZDO(thing);
      return zdo != null ? zdo.GetLong(key, defaultValue) : defaultValue;
    }

    public static void SetExtraData(this Component thing, string key, bool newValue) {
      var zdo = GetZDO(thing);
      if (zdo == null) {
        Logger.LogError($"Unable to set {key} on {thing}!");
        return;
      }
      zdo.Set(key, newValue);
    }

    public static void SetExtraData(this Component thing, string key, string newValue) {
      var zdo = GetZDO(thing);
      if (zdo == null) {
        Logger.LogError($"Unable to set {key} on {thing}!");
        return;
      }
      zdo.Set(key, newValue);
    }

    public static void SetExtraData(this Component thing, string key, float newValue) {
      var zdo = GetZDO(thing);
      if (zdo == null) {
        Logger.LogError($"Unable to set {key} on {thing}!");
        return;
      }
      zdo.Set(key, newValue);
    }

    public static void SetExtraData(this Component thing, string key, long newValue) {
      var zdo = GetZDO(thing);
      if (zdo == null) {
        Logger.LogError($"Unable to set {key} on {thing}!");
        return;
      }
      zdo.Set(key, newValue);
    }

    public static TextReceiver GetTextReceiver(this Component thing, string storageKey) {
      return new ComponentTextReceiver(thing, storageKey);
    }

    // A TextReceiver implementation (used to receive text from an input
    // dialog) that stores data in a component's ZDO.
    private class ComponentTextReceiver : TextReceiver {
      private ZDO zdo;
      private string key;

      // SetText is called asynchronously.  (Surprise!)  So we store
      // a ZDO and a key, and store the eventual text under that key.
      public ComponentTextReceiver(Component thing, string storageKey) {
        zdo = GetZDO(thing);
        key = storageKey;
      }

      public string GetText() {
        return zdo.GetString(key, "");
      }

      public void SetText(string text) {
        zdo.Set(key, text);
      }
    }

    public static string GetPrefabName(this Component thing) {
      // This turns something like "Eikthyr(Clone)" into "Eikthyr".
      return thing.gameObject.name.Split('(')[0];
    }

    public delegate ResultType StealCallbackType<ComponentType, ResultType>(ComponentType component);

    public static ResultType StealFromPrefab<ComponentType, ResultType>(
        string prefabName,
        StealCallbackType<ComponentType, ResultType> getResult) {
      var prefab = PrefabManager.Instance.GetPrefab(prefabName);
      var component = prefab.GetComponent<ComponentType>();
      if (component == null) {
        component = prefab.GetComponentsInChildren<ComponentType>(true)[0];
      }
      return getResult(component);
    }

    public static void MoveToFloor(this Vector3 position, float offset = 0f) {
      if (ZoneSystem.instance.FindFloor(position, out var height)) {
        position.y = height + offset;
      }
    }

    private static IEnumerator DoDelayAndThenCall(float seconds, Action callback) {
      yield return new WaitForSeconds(seconds);
      callback();
    }

    public static void DelayCall(this MonoBehaviour thing, float seconds, Action callback) {
      thing.StartCoroutine(DoDelayAndThenCall(seconds, callback));
    }

    public static float DistanceXZ(Vector3 a, Vector3 b) {
      var a2 = new Vector2(a.x, a.z);
      var b2 = new Vector2(b.x, b.z);
      return Vector2.Distance(a2, b2);
    }

    public static void DisableParticleEffects(
        GameObject gameObject, bool keepFirst = false) {
      var particleSystems =
          gameObject.GetComponentsInChildren<ParticleSystem>();

      // For item drops, we don't disable the very first effect, since that is
      // the sparkles attached to an item drop.  We want to keep those.
      for (var i = keepFirst ? 1 : 0; i < particleSystems.Length; ++i) {
        var particleSystem = particleSystems[i];
        var emission = particleSystem.emission;
        emission.enabled = false;
        particleSystem.Stop();
      }
    }

    public static void DisableSounds(GameObject gameObject) {
      var audioSources = gameObject.GetComponentsInChildren<AudioSource>();
      foreach (var source in audioSources) {
        source.enabled = false;
      }
    }

    public static Player GetPlayerByName(string name) {
      if (name == null || name == "") {
        return null;
      }

      var players = Player.m_players;
      foreach (var player in players) {
        if (player.GetPlayerName() == name) {
          return player;
        }
      }

      return null;
    }

    public static void SetGlobalScaleToOne(this Transform transform) {
      // We can only set a local scale, relative to the parent transform.
      // The global scale (lossyScale) is read-only.  But some objects should
      // be set to a global scale of 1, regardless of their parent's scale.
      transform.localScale = Vector3.one;
      var parentScale = transform.lossyScale;
      transform.localScale = new Vector3(
          1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
      // lossyScale should now be equal to Vector3.one.
    }

    public static string PathFrom(this Transform child, Transform ancestor) {
      if (child == ancestor) {
        return "";
      }

      var path = child.name;
      var parent = child.parent;
      while (parent != null) {
        path = parent.name + "/" + path;
        if (parent == ancestor) {
          return path;
        }
        parent = parent.parent;
      }

      throw new ArgumentException($"GameObject {ancestor} is not an ancestor of {child}");
    }

    public static string PathFrom(this Transform child, Component ancestor) {
      return child.PathFrom(ancestor.transform);
    }

    public static string PathFrom(this Transform child, GameObject ancestor) {
      return child.PathFrom(ancestor.transform);
    }

    public static List<string> GenerateStringList(string prefix, int num) {
      var list = new List<String>();
      foreach (int index in Enumerable.Range(1, num)) {
        list.Add(prefix + index.ToString("d2"));
      }
      return list;
    }

    // Some UI Text elements are automatically re-localized on Update() by
    // Valheim's Localization class.  This makes it impossible to patch their
    // text contents without also patching the text cached in Localization.
    // (This was not true in November 2021, and it is true in May of 2022.)
    // This encapsulates the solution and patches the text in both the Text
    // element itself and the cached data in Localization (if present).
    public static void PatchUIText(Text element, string newText) {
      element.text = newText;

      var dictionary = Localization.instance.textStrings;
      if (dictionary.ContainsKey(element)) {
        dictionary[element] = newText;
      }
    }
  }
}
