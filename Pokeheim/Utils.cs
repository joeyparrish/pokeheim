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
using Jotunn.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class Utils {
    // Other classes can register callbacks here without worrying about
    // unregistering.  Similar events from Jotunn will be invoked many times,
    // and for most, you must unregister your callbacks after the first time.
    // Ours are tailored to our needs, and will be invoked once or many times
    // as appropriate.  Use them to call methods like StealFromPrefab, to
    // register custom items, or to modify built-in locations.

    public static Hook OnVanillaPrefabsAvailable =
        new Hook("OnVanillaPrefabsAvailable");
    public static Hook OnVanillaLocationsAvailable =
        new Hook("OnVanillaLocationsAvailable", oneShot: false);
    public static Hook OnFirstSceneStart =
        new Hook("OnFirstSceneStart");
    public static Hook OnRPCsReady =
        new Hook("OnRPCsReady", oneShot: false);
    public static Hook OnDLCManAwake =
        new Hook("OnDLCManAwake", oneShot: false);

    [HarmonyPatch]
    private static class HookPatches {
      [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), HarmonyPrefix]
      private static void VanillaPrefabs() => OnVanillaPrefabsAvailable.Trigger();

      [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations)), HarmonyPostfix]
      private static void VanillaLocations() => OnVanillaLocationsAvailable.Trigger();

      [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
      private static void FirstScene() => OnFirstSceneStart.Trigger();

      [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake)), HarmonyPostfix]
      private static void RPCsReady() => OnRPCsReady.Trigger();

      [HarmonyPatch(typeof(DLCMan), nameof(DLCMan.Awake)), HarmonyPostfix]
      private static void DLCAwake() => OnDLCManAwake.Trigger();

      [HarmonyPatch(typeof(DLCMan), nameof(DLCMan.OnDestroy)), HarmonyPostfix]
      private static void DLCDead() => OnDLCManAwake.Untrigger();
    }

    public class Hook {
      public bool Triggered => triggered;
      public string Name => name;

      protected string name = "";
      private bool oneShot = false;
      private bool triggered = false;
      private List<Action> callbacks = new List<Action>();

      public Hook(string name, bool oneShot = true) {
        this.name = name;
        this.oneShot = oneShot;
      }

      static public Hook operator +(Hook hook1, Hook hook2) {
        return new MultiHook(hook1, hook2);
      }

      static public Hook operator +(Hook hook, Action callback) {
        hook.callbacks.Add(callback);

        if (hook.triggered) {
          hook.SafeInvoke(callback);
        }

        return hook;
      }

      internal void Trigger() {
        if (oneShot && triggered) {
          return;
        }

        triggered = true;
        Logger.LogDebug($"Triggering hook {name}");

        foreach (Action callback in callbacks) {
          SafeInvoke(callback);
        }
      }

      internal void Untrigger() {
        triggered = false;
        Logger.LogDebug($"Untriggering hook {name}");
      }

      private void SafeInvoke(Action callback) {
        try {
          callback();
        } catch (Exception e) {
          Logger.LogError($"Exception thrown at event {name} in {callback.Method.DeclaringType.Name}.{callback.Method.Name}:\n{e}");
        }
      }
    }

    public class MultiHook : Hook {
      private List<Hook> hooks = new List<Hook>();

      public MultiHook(Hook hook1, Hook hook2) : base("", false) {
        this.name = hook1.Name + ", " + hook2.Name;

        hooks.Add(hook1);
        hooks.Add(hook2);

        hook1 += () => this.SubHookCallback();
        hook2 += () => this.SubHookCallback();
      }

      private void SubHookCallback() {
        foreach (var hook in hooks) {
          if (!hook.Triggered) {
            return;
          }
        }

        Trigger();
      }
    }

    public static bool GetExtraData(this ZNetView netView, int key, bool defaultValue) {
      return netView?.GetZDO()?.GetBool(key, defaultValue) ?? defaultValue;
    }

    public static string GetExtraData(this ZNetView netView, int key, string defaultValue) {
      return netView?.GetZDO()?.GetString(key, defaultValue) ?? defaultValue;
    }

    public static void SetExtraData(this ZNetView netView, int key, bool newValue) {
      var zdo = netView.GetZDO();
      if (zdo == null) {
        Logger.LogError($"Unable to set {key} on {netView}!");
        return;
      }
      zdo.Set(key, newValue);
    }

    public static void SetExtraData(this ZNetView netView, int key, string newValue) {
      var zdo = netView.GetZDO();
      if (zdo == null) {
        Logger.LogError($"Unable to set {key} on {netView}!");
        return;
      }
      zdo.Set(key, newValue);
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

    // If a unique location hasn't spawned, you should modify the prefab of it.
    // Otherwise, you should modify the one that already exists in the world.
    public static GameObject GetSpawnedLocationOrPrefab(string name) {
      var zoneLocation = ZoneManager.Instance.GetZoneLocation(name);
      if (zoneLocation == null) {
        Logger.LogError($"No such location: {name}");
        return null;
      }

      var prefabName = zoneLocation.m_prefab.name;
      var cloneName = prefabName + "(Clone)";
      foreach (var location in Location.m_allLocations) {
        if (location.gameObject.name == cloneName) {
          Logger.LogDebug($"Found existing instance of location {name}.");
          return location.gameObject;
        }
      }

      Logger.LogDebug($"Found no existing instances of location {name}.");
      return zoneLocation.m_prefab;
    }

    private static string cachedAssetRootPath = null;

    private static void FindAssetRootPath() {
      if (cachedAssetRootPath == null) {
        string assemblyFolder = System.IO.Path.GetDirectoryName(
            Assembly.GetExecutingAssembly().Location);
        cachedAssetRootPath = Path.Combine(
            assemblyFolder, "Pokeheim", "Assets");
      }
    }

    public static string GetAssetPath(string asset) {
      FindAssetRootPath();
      return Path.Combine(cachedAssetRootPath, asset);
    }

    public static Sprite LoadSprite(string asset) {
      return LoadSprite(asset, Vector2.zero);
    }

    public static Sprite LoadSprite(string asset, Vector2 pivot) {
      return AssetUtils.LoadSpriteFromFile(GetAssetPath(asset), pivot);
    }

    public static Texture2D LoadTexture(string asset) {
      return AssetUtils.LoadTexture(GetAssetPath(asset));
    }

    public static void ZDestroy(this Component thing) {
      thing.gameObject.ZDestroy();
    }

    public static void ZDestroy(this GameObject thing) {
      var netView = thing.GetComponent<ZNetView>();
      if (netView == null || netView.GetZDO() == null) {
        UnityEngine.Object.Destroy(thing.gameObject);
        return;
      }

      netView.ClaimOwnership();
      netView.Destroy();
    }
  }
}
