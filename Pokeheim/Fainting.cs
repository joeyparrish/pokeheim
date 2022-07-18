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
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class Fainting {
    // These are keys which will be used to store additional fields in ZDO.
    private const string IsFaintedKey = "com.pokeheim.IsFainted";
    private const string RagdollMonsterDataKey = "com.pokeheim.RagdollMonsterData";

    private const float DefaultFlipAngle = -90f;

    private static readonly Dictionary<string, float> SpecialFlipAngles =
        new Dictionary<string, float> {
      {"Boar_piggy", 0f},
      {"Wolf_cub", 0f},
      {"Blob", 0f},
      {"Leech", 0f},
      {"Serpent", 0f},
      {"Wraith", 90f},
      {"Deathsquito", 180f},
      {"Bat", 0f},
      {"Bonemass", 0f},
      {"Dragon", 0f},
    };

    public static bool IsFainted(this Character monster) {
      return monster.GetExtraData(IsFaintedKey, false);
    }

    public static void SetFainted(this Character monster, bool value) {
      monster.SetExtraData(IsFaintedKey, value);
    }

    public static string GetMonsterData(this Ragdoll ragdoll) {
      return ragdoll.GetExtraData(RagdollMonsterDataKey, "");
    }

    public static void SetMonsterData(this Ragdoll ragdoll, string data) {
      ragdoll.SetExtraData(RagdollMonsterDataKey, data);
    }

    private static EffectList GetRagdollEffect(this Character monster) {
      EffectList.EffectData[] data = monster.m_deathEffects.m_effectPrefabs;
      for (int i = 0; i < data.Length; ++i) {
        var prefab = data[i].m_prefab;
        var ragdoll = prefab.GetComponent<Ragdoll>();
        if (data[i].m_enabled && ragdoll != null) {
          // Make an effect list containing only this one effect.
          EffectList list = new EffectList();
          list.m_effectPrefabs = new EffectList.EffectData[]{ data[i] };
          return list;
        }
      }
      return null;
    }

    private static void RPC_DestroyEnemyHud(
          this Character monster, long sender) {
      // Hide the health HUD if it already exists.  A patch will keep a new
      // one from being created.
      if (EnemyHud.instance != null) {
        if (EnemyHud.instance.m_huds.TryGetValue(monster, out var hud)) {
          UnityEngine.Object.Destroy(hud.m_gui);
          EnemyHud.instance.m_huds.Remove(monster);
        }
      }
    }

    private static void RegisterCustomRPCs(this Character monster) {
      // Since the RPCs are themselves extension methods, we have to wrap them
      // in delegates to register them with the RPC system.
      monster.m_nview.Register(
          "PokeheimDestroyEnemyHud",
          sender => monster.RPC_DestroyEnemyHud(sender));
      monster.m_nview.Register(
          "PokeheimStopAnimation",
          sender => monster.RPC_StopAnimation(sender));
    }

    private static void DestroyEnemyHud(this Character monster) {
      // Send an RPC, because the Enemy HUD is per-player and not synced.
      monster.m_nview.InvokeRPC(ZNetView.Everybody, "PokeheimDestroyEnemyHud");
    }

    // A fainting effect for things like Skeletons which don't have a Ragdoll
    // implementation.
    private static bool FaintWithoutRagdoll(
        this Character monster, Vector3 hitDirection) {
      var baseAI = monster.m_baseAI;
      if (baseAI == null) {
        Logger.LogError($"No AI!  Failed to faint {monster}");
        return false;
      }

      var monsterAI = baseAI as MonsterAI;

      // Set a very low, but positive, health value.
      monster.SetHealth(1e-9f);

      // Stop paying attention to your targets.
      monster.SetTarget(null);

      // Other monsters stop targetting this monster.
      monster.StopBeingTargetted();

      // Hide the health HUD if it already exists.  A patch will keep a new
      // one from being created.
      monster.DestroyEnemyHud();

      // Don't vanish!
      if (monsterAI != null && monsterAI.m_nview != null) {
        monsterAI.SetDespawnInDay(false);
        monsterAI.SetEventCreature(false);
      }

      // Stop moving.
      monster.SetMoveDir(Vector3.zero);

      // Stop making noise.
      if (monsterAI != null) {
        monsterAI.m_idleSoundChance = 0f;
      }

      if (monster.m_flying) {
        // Stop flying and start falling.
        // UpdateMotion() will make these take effect.
        monster.m_flying = false;
        monster.m_zanim.SetBool(ZSyncAnimation.GetHash("flying"), value: false);

        // m_body.useGravity is cached and synced with ZSyncTransform, so be
        // sure to set both.
        monster.m_body.useGravity = true;
        var transform = monster.GetComponent<ZSyncTransform>();
        if (transform != null) {
          transform.m_useGravity = true;
        }

        // Not every monster has this trigger, but it helps with Moder.
        monster.m_zanim.SetTrigger("fly_land");
        monster.DelayCall(0.7f, delegate {
          monster.FallDown();
          monster.StopAnimation();
        });
      } else if (monster.GetPrefabName() == "Serpent") {
        // It looks super wrong to freeze the tail of a serpent, but then to
        // make its face turn to face the attack direction.  Since it doesn't
        // have a stagger animation anyway, skip all that for serpents.
        monster.StopAnimation();
      } else {
        // Add a stagger animation.  Not every monster has one, but this works
        // nicely for many humanoids.
        monster.Stagger(hitDirection);
        monster.DelayCall(0.7f, delegate {
          monster.FallDown();
          monster.StopAnimation();
        });
      }

      // Flag this monster as fainted.
      monster.SetFainted(true);

      return true;
    }

    public static void FallDown(this Character monster) {
      var prefabName = monster.GetPrefabName();

      float flipAngle = 0f;
      if (!SpecialFlipAngles.TryGetValue(prefabName, out flipAngle)) {
        flipAngle = DefaultFlipAngle;
      }

      if (flipAngle != 0f) {
        monster.transform.position += new Vector3(0f, 1f, 0f);
        monster.transform.Rotate(flipAngle, 0f, 0f);
      }
    }

    public static void StopAnimation(this Character monster) {
      monster.m_nview.InvokeRPC(ZNetView.Everybody, "PokeheimStopAnimation");
    }

    public static void RPC_StopAnimation(this Character monster, long sender) {
      // This stops the monster from animating or moving.
      monster.m_animator.enabled = false;
      // This stops the monster from oozing, flaming, or fuming.
      Utils.DisableParticleEffects(monster.gameObject);
      // This turns off the sound of a Deathsquito buzzing, for example.
      Utils.DisableSounds(monster.gameObject);
    }

    private static bool FaintWithRagdoll(
        this Character monster, EffectList ragdollEffect) {
      // Create and style a ragdoll.  Based on code in Character.OnDeath.
      Transform transform = monster.transform;
      GameObject[] effects = ragdollEffect.Create(
          transform.position, transform.rotation, transform);
      // Should just be the one effect, so just one game object.
      GameObject effect = effects[0];
      Ragdoll ragdoll = effect.GetComponent<Ragdoll>();

      LevelEffects levelEffects =
          monster.GetComponentInChildren<LevelEffects>();

      Vector3 velocity = monster.m_body.velocity;
      if (monster.m_pushForce.magnitude * 0.5f > velocity.magnitude) {
        velocity = monster.m_pushForce * 0.5f;
      }

      float hue = 0f;
      float saturation = 0f;
      float value = 0f;
      if ((bool)levelEffects) {
        levelEffects.GetColorChanges(out hue, out saturation, out value);
      }
      ragdoll.Setup(velocity, hue, saturation, value, null);

      // Encode the capture data into the ragdoll.
      ragdoll.SetMonsterData((new Inhabitant(monster)).ToString());
      ragdoll.SetCaptured(monster.IsCaptured());
      ragdoll.SetOwner(monster.GetOwnerName());
      ragdoll.SetNoReturn(monster.WillNotReturn());

      // Destroy the actual monster object.
      monster.ZDestroy();

      return true;
    }

    public static bool Faint(this Character monster, Vector3 hitDirection) {
      if (monster.IsPlayer()) {
        return false;
      }

      var tameable = monster.GetComponent<Tameable>();
      // Make sure the monster drops its saddle, if any.
      if (tameable != null) {
        tameable.OnDeath();
      }

      EffectList ragdollEffect = monster.GetRagdollEffect();
      if (ragdollEffect == null) {
        return monster.FaintWithoutRagdoll(hitDirection);
      } else {
        return monster.FaintWithRagdoll(ragdollEffect);
      }
    }

    // Make ragdolls interactable.  Way more fun.  Needs to happen on Awake
    // instead of FaintWithRagdoll for reloading a game with a ragdoll already
    // in it, and to sync this interactable state across clients.
    [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.Awake))]
    class AllRagdollsAreInteractable_Patch {
      static void Postfix(Ragdoll __instance) {
        var ragdoll = __instance;

        // Make the ragdoll interact with things.  Pokeballs can now hit it
        // directly, and the player can push it around, which is fun.
        foreach (var body in ragdoll.m_bodies) {
          var collider = body.GetComponent<Collider>();
          if (collider != null) {
            // Move the body parts into the "character" layer so that we can
            // efficiently find these in the projectile collision code.  This
            // also has the nice side-effect of making it possible to push
            // around the ragdolls.
            collider.gameObject.layer = LayerMask.NameToLayer("character");
          }
        }

        // Make the ragdoll durable by canceling its destruction call.
        ragdoll.CancelInvoke();
      }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    class RegisterMonsterRPCs_Patch {
      static void Postfix(Character __instance) {
        var monster = __instance;

        // Only patch non-players.
        if (!monster.IsPlayer()) {
          monster.RegisterCustomRPCs();
        }
      }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    class Fainting_Patch {
      static bool Prefix(Character __instance, HitData hit) {
        var monster = __instance;

        // Only patch attacks on non-players.
        if (monster.IsPlayer()) {
          // Let the original method run.
          return true;
        }

        var isFisticuffs = hit.m_skill == Skills.SkillType.Unarmed;
        var isDevConsole = hit.m_damage.m_damage > 1e6;
        var isOwnMonster = monster.IsSaddenedBy(hit);

        // Compute how much damage we are about to do.
        var health = monster.GetHealth();
        var damage = hit.GetTotalDamage();

        if (damage < health) {
          // Let the original method run.
          return true;
        }

        // Don't kill the monster, unless you've used the dev console or you've
        // beaten your own monster with your bare hands.  Why are your bare
        // hands deadly?  Because that poor monster died of SADNESS.
        if (isDevConsole || (isFisticuffs && isOwnMonster)) {
          // Let the original method run.
          return true;
        }

        if (monster.IsFainted()) {
          // Nothing to do.
          // Don't let the original method run.
          return false;
        }

        if (monster.Faint(hit.m_dir)) {
          // Don't let the original method run.
          return false;
        }

        Logger.LogError($"Failed to faint {monster}!  Letting it die instead.");
        return true;
      }
    }

    // The result of IsDead has implications for things like body friction, so
    // let's count fainted monsters as dead.
    [HarmonyPatch(typeof(Character), nameof(Character.IsDead))]
    class FaintedMonstersCountAsDead_Patch {
      static void Postfix(Character __instance, ref bool __result) {
        var monster = __instance;
        if (monster.IsFainted()) {
          __result = true;
        }
      }
    }

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FixedUpdate))]
    class FaintedMonstersDoNotThink_Patch {
      static bool Prefix(BaseAI __instance) {
        var monster = __instance.m_character;
        if (monster.IsFainted()) {
          // Don't run the FixedUpdate method, which suppresses UpdateAI in
          // this class and all subclasses.
          return false;
        }
        return true;
      }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetupContinousEffect))]
    class FaintedMonstersDoNotHaveEffects_Patch {
      static void Prefix(ref bool enabled) {
        enabled = false;
      }
    }

    [HarmonyPatch(typeof(Tail), nameof(Tail.LateUpdate))]
    class FaintedMonsterTailsDoNotMove_Patch {
      static bool Prefix(Tail __instance) {
        var tail = __instance;
        var monster = tail.m_character;
        if (monster != null && monster.IsFainted()) {
          // Make each tail segment hold its previous position.
          foreach (var tailSegment in tail.m_positions) {
            tailSegment.transform.position = tailSegment.pos;
            tailSegment.transform.rotation = tailSegment.rot;
          }

          // Zero the velocity of the tail body (if it exists).
          if (tail.m_tailBody != null) {
            tail.m_tailBody.velocity = Vector3.zero;
            tail.m_tailBody.angularVelocity = Vector3.zero;
          }

          // Don't run the LateUpdate method, to suppress new tail segment
          // movements.
          return false;
        }
        return true;
      }
    }

    [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.ShowHud))]
    class FaintedMonstersDoNotShowHealth_Patch {
      static bool Prefix(Character c) {
        if (c.IsFainted()) {
          return false;
        }
        return true;
      }
    }

    // Many parts of the game query a list of all Characters to iterate through
    // living creatures. (Ex: spawners) Fainted monsters should not count.
    [HarmonyPatch]
    static class ExcludeFainted_Patch {
      public static List<Character> filterOutFaintedCharacters(List<Character> all) {
        var filtered = new List<Character>();
        foreach (var character in all) {
          if (!character.IsFainted()) {
            filtered.Add(character);
          }
        }
        return filtered;
      }

      public static List<BaseAI> filterOutFaintedAIs(List<BaseAI> all) {
        var filtered = new List<BaseAI>();
        foreach (var baseAI in all) {
          var character = baseAI.m_character;
          if (!character.IsFainted()) {
            filtered.Add(baseAI);
          }
        }
        return filtered;
      }

      // Because these methods can be inlined at runtime, we need to patch
      // their callers.  The lists get long.
      [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.HaveFriendsInRange))]
      [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.HaveFriendInRange),
                    new Type[] { typeof(float) })]
      [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.HaveHurtFriendInRange),
                    new Type[] { typeof(float) })]
      [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FindEnemy))]
      [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.HaveEnemyInRange))]
      [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FindClosestEnemy))]
      [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FindRandomEnemy))]
      [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.GetNrOfInstances),
                    new Type[] { typeof(string) })]
      [HarmonyTranspiler]
      static IEnumerable<CodeInstruction> Patch1(
          IEnumerable<CodeInstruction> instructions,
          ILGenerator generator) {
        var allMethod = typeof(Character).GetMethod(
            nameof(Character.GetAllCharacters));
        var filterMethod = typeof(ExcludeFainted_Patch).GetMethod(
            nameof(ExcludeFainted_Patch.filterOutFaintedCharacters));

        var phases = new TranspilerSequence.Phase[] {
          new TranspilerSequence.Phase {
            matcher = code => (code.opcode == OpCodes.Call &&
                               (code.operand as MethodInfo) == allMethod),
            replacer = code => new CodeInstruction[] {
              // Emit the original call.
              code,
              // Call our filtering method on the list on the stack.
              // Only non-fainted monsters will be considered.
              new CodeInstruction(OpCodes.Call, filterMethod),
            },
          },
        };

        return TranspilerSequence.Execute(
            "GetAllCharacters", phases, instructions);
      }  // static IEnumerable<CodeInstruction> Patch1

      // Because these methods can be inlined at runtime, we need to patch
      // their callers.  The lists get long.
      [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.InStealthRange))]
      [HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.GetInstances))]
      [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.HaveInstanceInRange))]
      [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.GetNrOfInstances),
                    new Type[] {
                      typeof(GameObject), typeof(Vector3), typeof(float),
                      typeof(bool), typeof(bool),
                    })]
      [HarmonyTranspiler]
      static IEnumerable<CodeInstruction> Patch2(
          IEnumerable<CodeInstruction> instructions,
          ILGenerator generator) {
        var allMethod = typeof(BaseAI).GetMethod(
            nameof(BaseAI.GetAllInstances));
        var filterMethod = typeof(ExcludeFainted_Patch).GetMethod(
            nameof(ExcludeFainted_Patch.filterOutFaintedAIs));

        var phases = new TranspilerSequence.Phase[] {
          new TranspilerSequence.Phase {
            matcher = code => (code.opcode == OpCodes.Call &&
                               (code.operand as MethodInfo) == allMethod),
            replacer = code => new CodeInstruction[] {
              // Emit the original call.
              code,
              // Call our filtering method on the list on the stack.
              // Only non-fainted monsters will be considered.
              new CodeInstruction(OpCodes.Call, filterMethod),
            },
          },
        };

        return TranspilerSequence.Execute(
            "GetAllInstances", phases, instructions);
      }  // static IEnumerable<CodeInstruction> Patch2
    }  // class ExcludeFainted_Patch

    [RegisterCommand]
    class FaintAll : ConsoleCommand {
      public override string Name => "faintall";
      public override string Help => "Faint all nearby monsters.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var allCharacters = Character.GetAllCharacters();
        var playerPosition = Player.m_localPlayer.transform.position;

        foreach (var monster in allCharacters) {
          if (!monster.IsPlayer() && !monster.IsFainted()) {
            var hitDirection = monster.transform.position - playerPosition;

            if (monster.Faint(hitDirection)) {
              Debug.Log($"Fainted {monster}");
            } else {
              Debug.Log($"Failed to faint {monster}");
            }
          }
        }
      }
    }
  }
}
