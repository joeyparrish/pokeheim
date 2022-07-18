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
  public static class Debugging {
    private const string IsFrozenKey = "com.pokeheim.IsFrozen";

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

        character.SetExtraData(IsFrozenKey, true);
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

        character.SetExtraData(IsFrozenKey, false);
      }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.UpdateMotion))]
    class FreezeCharacter_Patch {
      static bool Prefix(Character __instance) {
        var character = __instance;
        if (character.GetExtraData(IsFrozenKey, false)) {
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

    [RegisterCommand]
    class RenderAndDump : ConsoleCommand {
      public override string Name => "renderanddump";
      public override string Help => "[name] [size=128] - Instantiate a prefab, render it, and dump it to disk";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var name = args[0];
        var gameObject = Find(name);
        if (gameObject != null) {
          Debug.Log($"Found existing object for {name}");
        } else {
          var prefab = PrefabManager.Instance.GetPrefab(name);
          if (prefab == null) {
            Debug.Log($"Unable to find prefab named {name} to dump");
            return;
          }
          gameObject = UnityEngine.Object.Instantiate(prefab);
          Debug.Log($"Loaded prefab for {name}");
        }

        var size = 128;
        if (args.Length > 1) {
          size = int.Parse(args[1]);
        }

        var request = new RenderManager.RenderRequest(gameObject) {
          Width = size,
          Height = size,
        };

        var sprite = RenderManager.Instance.Render(request);
        var tex = sprite.texture;
        var bytes = tex.EncodeToPNG();
        var path = $"{Application.persistentDataPath}/renders/{name}.png";
			  Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, bytes);
        Debug.Log($"Sprite saved to {path}");
      }
    }

    [RegisterCommand]
    class DumpComponents : ConsoleCommand {
      public override string Name => "dumpcomponents";
      public override string Help => "[name] - Dump a list of components in an object";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var gameObject = Find(args[0]);
        if (gameObject == null) {
          Debug.Log($"Unable to find object named {args[0]} to dump");
          return;
        }

        Logger.LogInfo($"Object: {gameObject}");
        foreach (var component in gameObject.GetComponents<Component>()) {
          Logger.LogInfo($"  Direct component: {component}");
        }
        foreach (var component in gameObject.GetComponentsInChildren<Component>()) {
          Logger.LogInfo($"  Child component: {component}");
        }
      }
    }

    [RegisterCommand]
    class DumpBodyParts : ConsoleCommand {
      public override string Name => "dumpbodyparts";
      public override string Help => "[name] - Dump a list of body parts in a character";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var monster = FindCharacter(args[0]);
        if (monster == null) {
          Debug.Log($"Unable to find object named {args[0]} to dump");
          return;
        }
        var visual = monster.transform.Find("Visual");
        var children = visual.gameObject.GetComponentsInChildren<Transform>();

        Logger.LogInfo($"Monster: {monster}");
        foreach (var child in children) {
          var path = child.PathFrom(visual);
          // localPosition is local to the immediate parent.  Instead, this is
          // local to the overall monster.
          var position = child.position - monster.transform.position;
          var localRotation = child.localRotation.eulerAngles;
          var rotation = Quaternion.FromToRotation(
              child.forward, monster.transform.forward).eulerAngles;
          Logger.LogInfo($"  Body part: {path} - position {position} rotation {rotation} local rotation {localRotation}");
        }

        var tameable = monster.GetComponent<Tameable>();
        if (tameable != null && tameable.m_saddle != null) {
          Logger.LogInfo($"  Saddle: {tameable.m_saddle}");
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

    [RegisterCommand]
    class StaggerAll : ConsoleCommand {
      public override string Name => "staggerall";
      public override string Help => "Stagger all nearby monsters.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var allCharacters = Character.GetAllCharacters();
        var playerPosition = Player.m_localPlayer.transform.position;

        foreach (var monster in allCharacters) {
          if (!monster.IsPlayer() && !monster.IsFainted()) {
            var hitDirection = monster.transform.position - playerPosition;

            monster.Stagger(hitDirection);
            Debug.Log($"Staggered {monster}");
          }
        }
      }
    }

    [RegisterCommand]
    class Saddle : ConsoleCommand {
      public override string Name => "saddle";
      public override string Help => "Saddle a nearby tame creature.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var allCharacters = Character.GetAllCharacters();
        foreach (var monster in allCharacters) {
          var tameable = monster.GetComponent<Tameable>();
          if (monster.IsTamed() && !tameable.HaveSaddle()) {
            Logger.LogInfo($"Saddling {monster}");
            tameable.RPC_AddSaddle(0L);
            return;
          }
        }

        Logger.LogInfo($"Found nothing to saddle.");
      }
    }

    [RegisterCommand]
    class Mount : ConsoleCommand {
      public override string Name => "mount";
      public override string Help => "Mount a nearby saddled creature.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var allCharacters = Character.GetAllCharacters();
        foreach (var monster in allCharacters) {
          var tameable = monster.GetComponent<Tameable>();
          if (tameable?.HaveSaddle() ?? false) {
            Logger.LogInfo($"Mounting {monster}");
            var saddle = tameable.m_saddle;
            var maxUseRange = saddle.m_maxUseRange;
            saddle.m_maxUseRange = 9999f;
            saddle.Interact(Player.m_localPlayer, repeat: false, alt: false);
            saddle.m_maxUseRange = maxUseRange;
            return;
          }
        }

        Logger.LogInfo($"Found nothing to mount.");
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
      public override string Help => "[name] - Find all instances of a certain location.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        string name = args[0];

        // Arbitrary pin type, not boss pin, easily differentiable from what
        // Vegvisir adds.
        var pinType = Minimap.PinType.Icon0;
        var pinName = $"Found: {name}";
        var showMap = false;

        foreach (var location in ZoneSystem.instance.m_locationInstances.Values) {
          var position = location.m_position;
          if (location.m_location.m_prefabName == name) {
            Minimap.instance.DiscoverLocation(
                position, pinType, pinName, showMap);
          }
        }
        Debug.Log($"Added pins for all {name} locations.");
      }
    }

    [RegisterCommand]
    class FindBoss : FindLocation {
      public override string Name => "findboss";
      public override string Help => "[name or index] - Find all altars for a given boss.";
      public override bool IsCheat => true;

      private string[] bossLocationNames = new string[] {
        "Eikthyrnir",
        "GDKing",
        "Bonemass",
        "Dragonqueen",
        "GoblinKing",
      };

      public override void Run(string[] args) {
        string name = args[0];
        try {
          var index = int.Parse(args[1]);
          if (index < 0 || index >= bossLocationNames.Length) {
            Debug.Log($"Bad index: {index}");
            return;
          }

          name = bossLocationNames[index];
        } catch (Exception) {}

        base.Run(new string[] {name});
      }
    }
  }
}
#endif
