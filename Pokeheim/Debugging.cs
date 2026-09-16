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

#if DEBUG
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  [Feature(Features.Debugging)]
  public static class Debugging {
    private static readonly int IsFrozenKey = "com.pokeheim.IsFrozen".GetStableHashCode();

    private static bool PrintSoundNames = false;

    private static GameObject Find(string name) {
      return GameObject.Find(name + "(Clone)");
    }

    private static Character FindCharacter(string name) {
      var gameObject = Find(name);
      return gameObject?.GetComponent<Character>();
    }

    [RegisterCommand]
    class Spin : ConsoleCommand {
      public override string Name => "spin";
      public override string Help => "[name] [x] [y] [z] - Spin an object in 3 dimensions";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var gameObject = Find(args[0]);
        if (gameObject == null) {
          Debug.Log($"Unable to find object named {args[0]} to spin");
        } else {
          gameObject.transform.Rotate(
              float.Parse(args[1]), float.Parse(args[2]), float.Parse(args[3]));
        }
      }
    }

    [RegisterCommand]
    class Move : ConsoleCommand {
      public override string Name => "move";
      public override string Help => "[name] [x] [y] [z] - Move an object in 3 dimensions";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var gameObject = Find(args[0]);
        if (gameObject == null) {
          Debug.Log($"Unable to find object named {args[0]} to move");
        } else {
          gameObject.transform.position += new Vector3(
              float.Parse(args[1]), float.Parse(args[2]), float.Parse(args[3]));
        }
      }
    }

    [RegisterCommand]
    class Scale : ConsoleCommand {
      public override string Name => "scale";
      public override string Help => "[name] [scale] - Scale an object";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var gameObject = Find(args[0]);
        if (gameObject == null) {
          Debug.Log($"Unable to find object named {args[0]} to scale");
        } else {
          gameObject.transform.localScale *= float.Parse(args[1]);
        }
      }
    }

    [RegisterCommand]
    class Freeze : ConsoleCommand {
      public override string Name => "freeze";
      public override string Help => "[name] - Stop a character from moving";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var character = FindCharacter(args[0]);
        if (character == null) {
          Debug.Log($"Unable to find character named {args[0]} to freeze");
          return;
        }

        character.m_nview.SetExtraData(IsFrozenKey, true);
      }
    }

    [RegisterCommand]
    class Unfreeze : ConsoleCommand {
      public override string Name => "unfreeze";
      public override string Help => "[name] - Let a character move again";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var character = FindCharacter(args[0]);
        if (character == null) {
          Debug.Log($"Unable to find character named {args[0]} to unfreeze");
          return;
        }

        character.m_nview.SetExtraData(IsFrozenKey, false);
      }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.UpdateMotion))]
    class FreezeCharacter_Patch {
      static bool Prefix(Character __instance) {
        var character = __instance;
        if (character.m_nview.GetExtraData(IsFrozenKey, false)) {
          return false;
        }
        return true;
      }
    }

    [RegisterCommand]
    class GetGravity : ConsoleCommand {
      public override string Name => "getgravity";
      public override string Help => "[name] - Get gravity setting for a character";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var character = FindCharacter(args[0]);
        if (character == null) {
          Debug.Log($"Unable to find character named {args[0]}");
          return;
        }

        Debug.Log($"{character} gravity setting is {character.m_body.useGravity}");
      }
    }

    [RegisterCommand]
    class SetGravity : ConsoleCommand {
      public override string Name => "setgravity";
      public override string Help => "[name] [true/false] - Change gravity setting for a character";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var character = FindCharacter(args[0]);
        if (character == null) {
          Debug.Log($"Unable to find character named {args[0]}");
          return;
        }

        var newValue = bool.Parse(args[1]);

        character.m_body.useGravity = newValue;
        var transform = character.GetComponent<ZSyncTransform>();
        if (transform != null) {
          transform.m_useGravity = newValue;
        }
      }
    }

    [RegisterCommand]
    class GetMass : ConsoleCommand {
      public override string Name => "getmass";
      public override string Help => "[name] - Get mass setting for a character";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var character = FindCharacter(args[0]);
        if (character == null) {
          Debug.Log($"Unable to find character named {args[0]}");
          return;
        }

        Debug.Log($"{character} mass setting is {character.m_body.mass}");
      }
    }

    [RegisterCommand]
    class SetMass : ConsoleCommand {
      public override string Name => "setmass";
      public override string Help => "[name] [true/false] - Change mass setting for a character";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var character = FindCharacter(args[0]);
        if (character == null) {
          Debug.Log($"Unable to find character named {args[0]}");
          return;
        }

        character.m_body.mass = float.Parse(args[1]);
      }
    }

    // GameObject.Find only sees active objects, and Find() above only looks for
    // spawned prefabs.  Menus and panels are usually inactive until opened,
    // which is exactly when you want to look inside them, so fall back to a
    // scan that includes inactive objects.
    private static GameObject FindAnywhere(string name) {
      var gameObject = GameObject.Find(name) ?? Find(name);
      if (gameObject != null) {
        return gameObject;
      }

      GameObject fallback = null;
      foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>()) {
        if (candidate.name != name) {
          continue;
        }
        // This also returns prefabs and other assets that belong to no scene.
        // Prefer something really in the scene, but take an asset if that is
        // all there is.
        if (candidate.scene.IsValid()) {
          return candidate;
        }
        if (fallback == null) {
          fallback = candidate;
        }
      }
      return fallback;
    }

    [RegisterCommand]
    class DumpHierarchy : ConsoleCommand {
      public override string Name => "dumphierarchy";
      public override string Help => "[name] [depth=4] - Dump an object's descendants and their components.  With no name, dumps every GUI root.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var depth = 4;
        var name = (string)null;

        // A bare number means a depth for the GUI dump, so that you can ask for
        // more of it without having to name something first.  Nothing in the
        // game is called "6".
        if (args.Length > 0) {
          int parsed;
          if (int.TryParse(args[0], out parsed)) {
            depth = parsed;
          } else {
            name = args[0];
            if (args.Length > 1 && !int.TryParse(args[1], out depth)) {
              Debug.Log($"Not a number: {args[1]}");
              return;
            }
          }
        }

        if (name == null) {
          DumpGuiRoots(depth);
          return;
        }

        var gameObject = FindAnywhere(name);
        if (gameObject == null) {
          Debug.Log($"Unable to find an object named {name} to dump");
          return;
        }

        // The tree goes to the log rather than the console, because it is
        // usually far too big to read on screen.
        Logger.LogInfo($"Hierarchy of {gameObject.name}:");
        gameObject.transform.LogHierarchy(maxDepth: depth);
        Debug.Log($"Dumped hierarchy of {gameObject.name} to the log.");
      }

      // Every canvas in the loaded scenes, which is as close to a single "GUI
      // root" as the game has.  Doing it this way rather than naming one object
      // means it works the same in the main menu and in the game, and picks up
      // anything Jotunn or another mod has added.
      private static void DumpGuiRoots(int depth) {
        var count = 0;
        foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>()) {
          // Prefabs and other assets belong to no scene.
          if (!canvas.gameObject.scene.IsValid()) {
            continue;
          }
          // A nested canvas is already inside its root's tree.
          if (!canvas.isRootCanvas) {
            continue;
          }
          Logger.LogInfo($"GUI root: {canvas.name}");
          canvas.transform.LogHierarchy(maxDepth: depth);
          count++;
        }

        if (count == 0) {
          Debug.Log("Found no GUI roots.");
        } else {
          Debug.Log($"Dumped {count} GUI root(s) to the log.");
        }
      }
    }

    [RegisterCommand]
    class DumpAnimations : ConsoleCommand {
      public override string Name => "dumpanimations";
      public override string Help => "[name] - Dump animation triggers for a specific character";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var character = FindCharacter(args[0]);
        if (character == null) {
          Debug.Log($"Unable to find character named {args[0]} to dump");
          return;
        }

        PrintAnimationTriggers_Patch.TargetPrefabName = args[0];
        Logger.LogInfo($"Dumping future animation triggers for character: {args[0]}");
      }
    }

    [RegisterCommand]
    class Animate : ConsoleCommand {
      public override string Name => "animate";
      public override string Help => "[name] [triggername] - Force a character to use a specific animation";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var character = FindCharacter(args[0]);
        if (character == null) {
          Debug.Log($"Unable to find character named {args[0]} to animate");
          return;
        }

        var animator = character.m_zanim;
        animator.SetTrigger(args[1]);
      }
    }

    [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetTrigger))]
    class PrintAnimationTriggers_Patch {
      public static string TargetPrefabName = "";

      static void Postfix(ZSyncAnimation __instance, string name) {
        var prefabName = __instance.GetPrefabName();
        if (prefabName == TargetPrefabName) {
          Logger.LogInfo($"{prefabName} animation trigger {name}");
        }
      }
    }

    [RegisterCommand]
    class Destroy : ConsoleCommand {
      public override string Name => "destroy";
      public override string Help => "[name] - Destroy an object";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var gameObject = Find(args[0]);
        if (gameObject == null) {
          Debug.Log($"Unable to find object named {args[0]} to destroy");
          return;
        }

        ZNetScene.instance.Destroy(gameObject);
      }
    }

    [RegisterCommand]
    class SpyOnSounds : ConsoleCommand {
      public override string Name => "spyonsounds";
      public override string Help => "Prints the names of sounds as the game plays them.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        PrintSoundNames = !PrintSoundNames;
        Debug.Log($"Printing sound names: {PrintSoundNames}");
      }
    }

    private static bool SpawnsEnabled = true;

    [RegisterCommand]
    class NoSpawns : ConsoleCommand {
      public override string Name => "nospawns";
      public override string Help => "Disable or re-enable random spawns.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        SpawnsEnabled = !SpawnsEnabled;
        Debug.Log($"Spawns enabled: {SpawnsEnabled}");
      }
    }

    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawnList))]
    class DisableSpawns_Patch {
      static bool Prefix() {
        // If enabled, let them happen.  Otherwise, inhibit the original method.
        return SpawnsEnabled;
      }
    }

    [RegisterCommand]
    class FindLocation : ConsoleCommand {
      public override string Name => "findlocation";
      public override string Help => "[name] - Find all instances of a certain location.  With no name, lists what this world has.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        if (args.Length < 1) {
          ListLocations();
          return;
        }

        string name = args[0];

        // Arbitrary pin type, not boss pin, easily differentiable from what
        // Vegvisir adds.
        var pinType = Minimap.PinType.Icon0;
        var pinName = $"Found: {name}";
        var showMap = false;

        foreach (var location in ZoneSystem.instance.m_locationInstances.Values) {
          var position = location.m_position;
          if (location.m_location.m_prefabName == name) {
            var found = false;

            foreach (var pin in Minimap.instance.m_pins) {
			        if (Utils.DistanceXZ(position, pin.m_pos) < 1f) {
                found = true;
              }
            }

            if (!found) {
              Minimap.instance.DiscoverLocation(
                  position, pinType, pinName, showMap);
            }
          }
        }
        Debug.Log($"Added pins for all {name} locations.");
      }

      // Matching is by exact prefab name, which is not the name of anything you
      // can see in game: the trader is "Haldor", but his location is
      // "Vendor_BlackForest".  List what this world actually placed so there is
      // nothing to guess at.
      private static void ListLocations() {
        var counts = new Dictionary<string, int>();
        foreach (var instance in ZoneSystem.instance.m_locationInstances.Values) {
          var name = instance.m_location.m_prefabName;
          int count;
          counts.TryGetValue(name, out count);
          counts[name] = count + 1;
        }

        var names = new List<string>(counts.Keys);
        names.Sort();

        Logger.LogInfo($"This world has {names.Count} kinds of location:");
        foreach (var name in names) {
          Logger.LogInfo($"    {name} x{counts[name]}");
        }
        Debug.Log(
            $"Listed {names.Count} kinds of location in the log.  " +
            "Pass one to findlocation to pin it.");
      }
    }
  }
}
#endif
