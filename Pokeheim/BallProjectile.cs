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
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class BallProjectile {
    private const float CaptureRadius = 2.0f;

    // We hijack the "Hide" button ("R" by default) to cause monsters to return.
    private const string HideButtonName = "Hide";

    public static bool IsBall(this Projectile ball) {
      return ball.m_skill == BallItem.Skill;
    }

    public static float BallFactor(this Projectile ball) {
      if (!ball.IsBall()) {
        return 0f;
      }

      var originalItem = ball.m_spawnItem;
      return originalItem.BallFactor();
    }

    public static Inhabitant GetInhabitant(this Projectile ball) {
      if (!ball.IsBall()) {
        return null;
      }

      var originalItem = ball.m_spawnItem;
      return originalItem.GetInhabitant();
    }

    public static bool Capture(this Projectile ball, Ragdoll ragdoll) {
      if (!ball.IsBall()) {
        return false;
      }

      DoCapture(ball, ragdoll);
      return true;
    }

    public static bool Capture(this Projectile ball, Character monster) {
      if (!ball.IsBall()) {
        return false;
      }

      if (BaseAI.IsEnemy(monster, ball.m_owner)) {
        var catchRate = monster.GetCatchRate(ball.BallFactor());
        Logger.LogDebug($"Monster: {monster} catchRate: {catchRate}");

        if (UnityEngine.Random.value > catchRate) {
          Logger.LogDebug($"Failed to capture {monster}");
          return false;
        }
      }

      DoCapture(ball, monster);
      return true;
    }

    // T may be a Ragdoll or Character (monster).
    private static void DoCapture<T>(
        Projectile ball, T thing, bool withSound = true) {
      Inhabitant inhabitant = null;
      var ragdoll = thing as Ragdoll;
      var monster = thing as Character;

      if (ragdoll != null) {
        inhabitant = new Inhabitant(ragdoll.GetMonsterData());
      } else {
        inhabitant = new Inhabitant(monster);
      }
      Logger.LogDebug($"Capturing: {inhabitant}");

      // Create an inhabited ball containing this monster.
      var originalBallItem = ball.m_spawnItem;
      var inhabitedBall = originalBallItem.InhabitWith(inhabitant);

      // Now spawn the inhabited ball.
      Vector3 position = ball.transform.position;
      Quaternion rotation = Quaternion.identity;
      var drop = ItemDrop.DropItem(inhabitedBall, 1, position, rotation);
      if (withSound) {
        Sounds.SoundType.Poof.PlayAt(position);
      }
      Logger.LogDebug($"Inhabited ball spawned: {drop}");

      var player = ball.m_owner as Player;
      var previouslyCaptured = false;
      var ownerName = "";
      if (ragdoll != null) {
        previouslyCaptured = ragdoll.IsCaptured();
        ownerName = ragdoll.GetOwnerName();
      } else {
        previouslyCaptured = monster.IsCaptured();
        ownerName = monster.GetOwnerName();
      }

      if (previouslyCaptured && ownerName == player.GetPlayerName()) {
        var name = inhabitant.Name;
        player.Message(MessageHud.MessageType.Center,
            Localization.instance.Localize("$monster_return", name));
      } else {
        player.LogCapture(inhabitant.PrefabName);
        player.Message(MessageHud.MessageType.Center, "$monster_caught");
        if (MonsterMetadata.PokedexFullness() == 1f) {
          player.PokeheimTutorial("caught_em_all", immediate: true);
        } else {
          player.PokeheimTutorial("caught");
        }
      }

      // Destroy the original game object.  NOTE: This must come after all
      // other logic.
      if (ragdoll != null) {
        ragdoll.ZDestroy();
      } else {
        var tameable = monster.GetTameable();
        // Make sure the monster drops its saddle, if any.
        if (tameable != null) {
          tameable.OnDeath();
        }
        monster.ZDestroy();
      }
    }

    public static bool Release(this Projectile ball) {
      Inhabitant inhabitant = ball.GetInhabitant();
      if (inhabitant == null) {
        return false;
      }

      var player = ball.m_owner as Player;
      inhabitant.Recreate(ball.transform.position, player);
      var name = inhabitant.Name;
      player.Message(MessageHud.MessageType.Center,
          Localization.instance.Localize("$monster_release", name));
      return true;
    }

    private class PreferentialCollisionTracker {
      private List<Ragdoll> TameRagdolls = new List<Ragdoll>();
      private List<Ragdoll> WildRagdolls = new List<Ragdoll>();
      private List<Character> TameMonsters = new List<Character>();
      private List<Character> WildMonsters = new List<Character>();

      // NOTE: directHit is only used for logging purposes
      public void Track(Collider collider, bool directHit) {
        if (collider == null) {
          return;
        }

        GameObject gameObject = Projectile.FindHitObject(collider);
        if (gameObject == null) {
          return;
        }

        Ragdoll ragdoll = gameObject.GetComponentInParent<Ragdoll>();
        Character monster = gameObject.GetComponent<Character>();

        if (ragdoll != null) {
          Logger.LogDebug($"Collision with {gameObject} ragdoll {ragdoll} direct hit {directHit}");

          var monsterData = ragdoll.GetMonsterData();
          if (monsterData == null || monsterData.Length == 0) {
            // A Ragdoll, but not a fainted monster.  This probably won't
            // happen thanks to CombatMods, but we should check.
            return;
          }

          if (ragdoll.IsCaptured()) {
            TameRagdolls.Add(ragdoll);
          } else {
            WildRagdolls.Add(ragdoll);
          }
        } else if (monster != null) {
          Logger.LogDebug($"Collision with {gameObject} monster {monster} direct hit {directHit}");

          if (monster.IsPlayer()) {
            // Never capture a player.
          } else if (monster.IsCaptured()) {
            TameMonsters.Add(monster);
          } else {
            WildMonsters.Add(monster);
          }
        }
      }

      public bool CaptureBest(Projectile ball) {
        // Prefer capturing a ragdoll (a "fainted" monster) over a moving
        // monster.  Within those groups, prefer a wild monster over a tame one.
        if (WildRagdolls.Count > 0) {
          return ball.Capture(WildRagdolls[0]);
        } else if (TameRagdolls.Count > 0) {
          return ball.Capture(TameRagdolls[0]);
        } else if (WildMonsters.Count > 0) {
          return ball.Capture(WildMonsters[0]);
        } else if (TameMonsters.Count > 0) {
          return ball.Capture(TameMonsters[0]);
        } else {
          return false;
        }
      }
    }

    private static void DoReturn<T>(Player player, IEnumerable<T> monsters) {
      string name;
      int numReturned;
      DoReturn(player, monsters, out name, out numReturned);
    }

    private static void DoReturn<T>(
        Player player, IEnumerable<T> monsters,
        out string name, out int numReturned) {
      var customItem = ItemManager.Instance.GetItem("Pokeball");
      var item = customItem.ItemDrop.m_itemData;

      var projectilePrefab = PrefabManager.Instance.GetPrefab("Pokeball_projectile");
      Vector3 position = player.transform.position;
      Quaternion rotation = Quaternion.identity;
      GameObject gameObject = UnityEngine.Object.Instantiate(
          projectilePrefab, position, rotation);

      var ball = gameObject.GetComponent<Projectile>();
      ball.m_owner = player;
      ball.m_spawnItem = item;
      ball.m_skill = BallItem.Skill;

      numReturned = 0;
      name = "";
      foreach (var thing in monsters) {
        var monster = thing as Character;
        var ragdoll = thing as Ragdoll;
        var willNotReturn = false;
        GameObject thingGameObject = null;

        if (monster != null) {
          name = monster.GetHoverName();
          willNotReturn = monster.WillNotReturn();
          thingGameObject = monster.gameObject;
        } else if (ragdoll != null) {
          willNotReturn = ragdoll.WillNotReturn();
          thingGameObject = ragdoll.gameObject;

          var monsterData = ragdoll.GetMonsterData();
          if (monsterData == "") {
            Logger.LogError($"Cannot return ragdoll with no monster data! {ragdoll}");
            willNotReturn = true;
          }
        }

        if (willNotReturn) {
          thingGameObject.ZDestroy();
        } else {
          // Use DoCapture() to bypass the random chance check in Capture().
          DoCapture(ball, thing, withSound: false);
          numReturned++;
        }
      }

      // Destroy the temporary projectile object so that it doesn't collide
      // with the player.
      ball.ZDestroy();
    }

    private static IEnumerable<Character> MonstersOwnedBy(Player player) {
      var playerName = player.GetPlayerName();
      var allCharacters = Character.GetAllCharacters();
      foreach (var character in allCharacters) {
        if (!character.IsPlayer() && character.GetOwnerName() == playerName &&
            // Fainted monsters don't return automatically when called.
            !character.IsFainted()) {
          yield return character;
        }
      }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
    class ThrownBall_Patch {
      static void Prefix(Projectile __instance, ref Collider collider, Vector3 hitPoint) {
        var ball = __instance;
        if (!ball.IsBall()) {
          return;
        }

        if (ball.Release()) {
          // We just released something.
          // Act as if nothing collided.
          collider = null;
          return;
        }

        var tracker = new PreferentialCollisionTracker();

        // Populate direct hits in the lists first, so that we prefer them to
        // AOE captures.
        tracker.Track(collider, directHit: true);

        // A simplified version of the original search code from DoAOE.
        Collider[] nearbyColliders = Physics.OverlapSphere(
            hitPoint, CaptureRadius);
        foreach (Collider nearbyCollider in nearbyColliders) {
          tracker.Track(nearbyCollider, directHit: false);
        }

        if (tracker.CaptureBest(ball)) {
          collider = null;
        }

        // Fall through to the original method, where we apply damage if
        // collider != null.
      }

      // Play sound on hit.
      static void Postfix(Projectile __instance) {
        var ball = __instance;
        if (ball.IsBall()) {
          Sounds.SoundType.Hit.PlayAt(ball.transform.position);
        }
      }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    class ListenForReturnButton_Patch {
      static void Prefix(Player __instance) {
        var player = __instance;
        if (player != Player.m_localPlayer) {
          return;
        }

        if (player.TakeInput()) {
          // Hijack the "hide" button (R by default).  By resetting the status,
          // the original method will never see that button active, so it will
          // only do what we want it to do.
          if (ZInput.GetButtonDown(HideButtonName)) {
            ZInput.ResetButtonStatus(HideButtonName);

            string name;
            int numReturned;
            DoReturn(player, MonstersOwnedBy(player),
                out name, out numReturned);

            if (numReturned != 0) {
              if (numReturned == 1) {
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$monster_return", name));
              } else {
                player.Message(MessageHud.MessageType.Center,
                    "$monster_return_all");
              }

              Sounds.SoundType.Poof.PlayAt(player.transform.position);
            }
          }
        }
      }
    }

    [HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
    class RenameHideButtonToReturnButton_Patch {
      static void Postfix(Settings __instance) {
        foreach (var setting in Settings.m_instance.m_keys) {
          if (setting.m_keyName == HideButtonName) {
            var text = setting.m_keyTransform.GetComponentInChildren<Text>();
            Utils.PatchUIText(text, Localization.instance.Localize(
                "$monster_recall_key_description"));
            break;
          }
        }
      }
    }

    [RegisterCommand]
    class CatchEmAll : ConsoleCommand {
      public override string Name => "catchemall";
      public override string Help => "Does the thing you gotta do.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var player = Player.m_localPlayer;
        DoReturn(player, AllMonsters());
        DoReturn(player, AllRagdolls());
        Sounds.SoundType.Poof.PlayAt(player.transform.position);
      }

      private IEnumerable<Character> AllMonsters() {
        var allCharacters = Character.GetAllCharacters();
        foreach (var character in allCharacters) {
          if (!character.IsPlayer()) {
            yield return character;
          }
        }
      }

      private IEnumerable<Ragdoll> AllRagdolls() {
        return UnityEngine.Object.FindObjectsOfType<Ragdoll>();
      }
    }
  }
}
