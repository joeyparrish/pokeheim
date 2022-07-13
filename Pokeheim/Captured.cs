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
using System.Reflection.Emit;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class Captured {
    // These are keys which will be used to store additional fields in ZDO.
    private const string IsCapturedKey = "com.pokeheim.IsCaptured";
    private const string OwnerKey = "com.pokeheim.Owner";
    private const string NoReturnKey = "com.pokeheim.NoReturn";

    private const int MaxPetNameLength = 20;
    private static EffectList PetEffect = null;

    public const float HealthBump = 1.25f;

    [PokeheimInit]
    public static void Init() {
      Utils.OnVanillaPrefabsAvailable += delegate {
        // Steal effects and cache them.
        PetEffect = Utils.StealFromPrefab<Tameable, EffectList>(
            "Boar", tameable => tameable.m_petEffect);
      };

      // The nested class that implements the generator in SpawnAbility.Spawn.
      // To patch the body of that method, we need to patch this nested class.
      // It can't be done with a HarmonyPatch annotation.
      var spawnAbilityNested = typeof(SpawnAbility).GetNestedType(
          "<Spawn>d__2", BindingFlags.NonPublic | BindingFlags.Instance);
      if (spawnAbilityNested != null) {
        Logger.LogDebug($"Nested class found in SpawnAbility.");

        var moveNextMethod = spawnAbilityNested.GetMethod(
            "MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);
        var spawnAbilityTranspiler = typeof(Captured).GetMethod(
            "SpawnAbilityTranspiler");

        PokeheimMod.harmony.Patch(
            moveNextMethod,
            transpiler: new HarmonyMethod(spawnAbilityTranspiler));
      } else {
        Logger.LogError($"No nested class found to patch in SpawnAbility!");
      }
    }

    public static bool IsCaptured(this Character monster) {
      return monster.GetExtraData(IsCapturedKey, false);
    }

    public static bool IsCaptured(this Ragdoll ragdoll) {
      return ragdoll.GetExtraData(IsCapturedKey, false);
    }

    public static bool WillNotReturn(this Character monster) {
      return monster.GetExtraData(NoReturnKey, false);
    }

    public static bool WillNotReturn(this Ragdoll ragdoll) {
      return ragdoll.GetExtraData(NoReturnKey, false);
    }

    public static void SetCaptured(this Character monster, bool value) {
      monster.SetExtraData(IsCapturedKey, value);
    }

    public static void SetCaptured(this Ragdoll ragdoll, bool value) {
      ragdoll.SetExtraData(IsCapturedKey, value);
    }

    public static void SetNoReturn(this Character monster, bool value) {
      monster.SetExtraData(NoReturnKey, value);
    }

    public static void SetNoReturn(this Ragdoll ragdoll, bool value) {
      ragdoll.SetExtraData(NoReturnKey, value);
    }

    // NOTE: Character has a GetOwner already, which returns a ZDO ID as a long
    public static string GetOwnerName(this Character monster) {
      return monster.GetExtraData(OwnerKey, "");
    }

    public static string GetOwnerName(this Ragdoll ragdoll) {
      return ragdoll.GetExtraData(OwnerKey, "");
    }

    public static Player GetOwnerPlayer(this Character monster) {
      return Utils.GetPlayerByName(monster.GetOwnerName());
    }

    public static Player GetOwnerPlayer(this Ragdoll ragdoll) {
      return Utils.GetPlayerByName(ragdoll.GetOwnerName());
    }

    public static void SetOwner(this Character monster, string value) {
      monster.SetExtraData(OwnerKey, value);
    }

    public static void SetOwner(this Ragdoll ragdoll, string value) {
      ragdoll.SetExtraData(OwnerKey, value);
    }

    public static void SetOwner(this Character monster, Player player) {
      monster.SetOwner(player.GetPlayerName());
    }

    public static void SetOwner(this Ragdoll ragdoll, Player player) {
      ragdoll.SetOwner(player.GetPlayerName());
    }

    public static bool IsSaddenedBy(this Character monster, HitData hit) {
      // Monsters are super sad when they get attacked by their owners.
      var owner = monster.GetOwnerPlayer();
      return owner != null && hit.m_attacker == owner.GetZDOID();
    }

    public static string GetPetName(this Character monster) {
      var tameable = monster.GetComponent<Tameable>();
      return tameable?.GetText() ?? "";
    }

    public static void SetPetName(this Character monster, string value) {
      var tameable = monster.GetComponent<Tameable>();
      if (tameable == null) {
        Logger.LogError($"Unable to set pet name {value} on {monster}.  Not tameable!");
      } else {
        tameable.SetText(value);
      }
    }

    public static void SetTarget(this Character monster, Character target) {
      var monsterAI = monster.m_baseAI as MonsterAI;
      monsterAI.m_targetCreature = target;
      if (target != null) {
        monster.m_baseAI.Alert();
      }
    }

    public static Character GetTarget(this Character monster) {
      var monsterAI = monster.m_baseAI as MonsterAI;
      return monsterAI?.m_targetCreature;
    }

    public static void StopBeingTargetted(this Character monster) {
      // Stop getting attacked by other monsters on our team.
      foreach (Character other in Character.GetAllCharacters()) {
        if (other.GetTarget() == monster) {
          other.SetTarget(null);
        }
      }
    }

    public static void ObeyMe(this Character monster, string ownerName) {
      monster.SetCaptured(true);
      monster.SetOwner(ownerName);

      // For all intents and purposes, this is now a tame monster.
      // This also sets up MonsterAI if it's missing.
      monster.SetTamed(true);

      BaseAI baseAI = monster.m_baseAI;
      MonsterAI monsterAI = baseAI as MonsterAI;
      Player owner = monster.GetOwnerPlayer();

      // Follow us!
      monsterAI.ResetPatrolPoint();
      monsterAI.SetFollowTarget(owner?.gameObject ?? null);

      // Surprising behavior for m_alertRange, so we need to set that, too.
      // Is your target beyond this distance?  Give up.
      // Is your owner beyond this distance?  Give up _on the target_ to follow
      // the owner instead.
      // This won't give the monster enhanced senses, though.  They still need
      // to be able to see a target to start attacking it.
      monsterAI.m_alertRange = 9999f;

      // Fight for us!
      monster.m_faction = Character.Faction.Players;
      monsterAI.m_enableHuntPlayer = false;
      monsterAI.m_attackPlayerObjects = false;

      // And fearlessly.
      baseAI.m_afraidOfFire = false;
      baseAI.m_avoidFire = false;
      baseAI.m_avoidWater = false;
      monsterAI.m_avoidLand = false;
      monsterAI.m_circulateWhileCharging = false;
      monsterAI.m_circulateWhileChargingFlying = false;
      monsterAI.m_circleTargetInterval = 0f;
      monsterAI.m_fleeIfLowHealth = 0;
      monsterAI.m_fleeIfHurtWhenTargetCantBeReached = false;
      monsterAI.m_fleeIfNotAlerted = false;

      // And don't die on a timer.  I'm looking at you, TentaRoot!
      UnityEngine.Object.Destroy(
          monster.GetComponent<CharacterTimedDestruction>());

      // Wake up monsters that are initially asleep (StoneGolem, etc.)
      monsterAI.Wakeup();

      // Don't waste time eating on the job.
      monsterAI.m_consumeItems?.Clear();

      // Beef up the monster to make the game a bit better balanced.
      // Victory is life!
      monster.m_health *= HealthBump;
      monster.SetMaxHealth(monster.m_health);
    }

    public static void ObeyMe(this Character monster, Player player) {
      monster.ObeyMe(player.GetPlayerName());
    }

    public static bool AlliedWith(this Character monster, Player player) {
      if (!monster.IsCaptured()) {
        return false;
      }

      var owner = monster.GetOwnerPlayer();
      // Monsters are always allied with their owners.
      if (player == owner) {
        return true;
      }

      // If PVP is on, then don't trust other players.
      var pvpOn = (owner?.IsPVPEnabled() ?? false) ||
                  (player?.IsPVPEnabled() ?? false);
      return !pvpOn;
    }

    // Re-apply behavioral patches on creatures when they reload.
    [HarmonyPatch(typeof(Character), nameof(Character.Start))]
    class ReloadBehavior_Patch {
      static void Postfix(Character __instance) {
        var monster = __instance;

        try {
          if (monster.IsCaptured() && !monster.IsFainted()) {
            var owner = monster.GetOwnerName();
            monster.ObeyMe(owner);
          } else if (monster.IsFainted()) {
            // Other aspects of fainting behavior remain after reloading, but
            // this must be re-applied.
            monster.StopAnimation();
          }
        } catch (Exception ex) {
          // We can't afford to throw from Character.Awake().  It will break
          // everything in crazy ways, like removing friction from monsters so
          // that they slide around the landscape as if on ice.  So we have to
          // catch exceptions here, just in case.
          Logger.LogError($"Exception in Character.Awake patch: {ex}");
        }
      }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetTamed))]
    class MakeTameable_Patch {
      static void Prefix(Character __instance) {
        var monster = __instance;
        var monsterAI = monster.GetComponent<MonsterAI>();

        // Tameable requires MonsterAI.  Hack one in if it's missing.
        if (monsterAI == null) {
          // Unregister RPCs from the old BaseAI, so the new one can take over.
          monster.m_baseAI.m_nview.Unregister("Alert");
          monster.m_baseAI.m_nview.Unregister("OnNearProjectileHit");
          UnityEngine.Object.Destroy(monster.GetComponent<BaseAI>());

          // Create a MonsterAI component, and set sensible defaults based on a
          // Boar.
          monsterAI = monster.gameObject.AddComponent<MonsterAI>();
          monsterAI.m_alertRange = 6f;
          monsterAI.m_interceptTimeMin = 0f;
          monsterAI.m_interceptTimeMax = 1f;
          monsterAI.m_hearRange = 20f;
          monsterAI.m_viewRange = 20f;
          monsterAI.m_viewAngle = 90f;
          monsterAI.m_smoothMovement = true;
          monsterAI.m_serpentMovement = true;
          monsterAI.m_randomMoveInterval = 30f;
          monsterAI.m_randomMoveRange = 10f;

          // Update all references to the AI.
          monster.m_baseAI = monsterAI;
          var characterAnimEvent = monster.GetComponent<CharacterAnimEvent>();
          if (characterAnimEvent != null) {
            characterAnimEvent.m_monsterAI = monsterAI;
          }
        }

        var tameable = monster.GetComponent<Tameable>();
        if (tameable == null) {
          // If we have to add Tameable, add a pet effect, too.
          tameable = monster.gameObject.AddComponent<Tameable>();
          tameable.m_petEffect = PetEffect;
          monster.m_baseAI.m_tamable /* [sic] */ = tameable;
        }

        // Even natively-tameable monsters should not be "commandable".
        // Instead, they always follow you.
        tameable.m_commandable = false;

        // All monsters can be ridden.
        tameable.gameObject.AddComponent<Riding.Mountable>();
      }
    }

    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.SelectBestAttack))]
    class SomeMonstersHaveNoWeapons_Patch {
      static bool Prefix(ref ItemDrop.ItemData __result, Humanoid humanoid) {
        // Some monsters have MonsterAI tacked on.  These are not Humanoids,
        // have no weapons, and can't attack.  In these cases, we should skip
        // SelectBestAttack and return null.  Otherwise, the method will throw
        // an exception.
        if (humanoid == null) {
          __result = null;
          return false;
        }
        return true;
      }
    }

    // Make captured monsters follow their owner if they aren't already.
    // This might occur if the player logs out and back in.
    [HarmonyPatch(typeof(Character), nameof(Character.FixedUpdate))]
    class CapturedMonstersFollow_Patch {
      static void Postfix(Character __instance) {
        var monster = __instance;
        var monsterAI = monster.m_baseAI as MonsterAI;

        if (monsterAI != null && monsterAI.GetFollowTarget() == null &&
            monster.IsCaptured()) {
          var owner = monster.GetOwnerPlayer();
          if (owner != null) {
            monsterAI.ResetPatrolPoint();
            monsterAI.SetFollowTarget(owner.gameObject);
          }
        }
      }
    }

    // Some monsters are gigantic and can't get close enough to you as measured
    // from center point to center point.  This increases the follow distance
    // so that these monsters don't constantly push you around.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.Follow))]
    class IncreaseFollowDistance_Patch {
      static IEnumerable<CodeInstruction> Transpiler(
          IEnumerable<CodeInstruction> instructions) {
        foreach (var code in instructions) {
          if (code.opcode == OpCodes.Ldc_R4) {
            // Double the values of constants.  This should make it 20 meters
            // to start running, and 6 meters to stop.  If the values change
            // upstream, this patch should still function.
            var value = (float)code.operand;
            yield return new CodeInstruction(OpCodes.Ldc_R4, value * 2f);
          } else {
            yield return code;
          }
        }
      }
    }

    // Normally, a tamed beast won't fight its own species, and no monster will
    // fight a boss.  This defines any non-captured monster as an enemy of a
    // captured one, and nobody as the enemy of a fainted monster.  Captured
    // monsters will also have allegiences based on their owner if PVP is
    // enabled.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsEnemy))]
    class WhoFightsWhom_Patch {
      static bool Postfix(bool originalResult, Character a, Character b) {
        // If anyone is fainted, there's no fight here.
        if (a.IsFainted() || b.IsFainted()) {
          return false;
        }

        // Cache these values for the logic below.
        var aIsCaptured = a.IsCaptured();
        var bIsCaptured = b.IsCaptured();
        var aOwner = a.GetOwnerName();
        var bOwner = b.GetOwnerName();
        var aAsPlayer = a as Player;
        var bAsPlayer = b as Player;

        // Captured vs Owner: not an enemy.
        if (aIsCaptured && aOwner == bAsPlayer?.GetPlayerName()) {
          return false;
        }
        if (bIsCaptured && bOwner == aAsPlayer?.GetPlayerName()) {
          return false;
        }

        // Captured vs other Player: depends on PVP setting
        if (aIsCaptured && bAsPlayer != null) {
          var aPvp = a.GetOwnerPlayer()?.IsPVPEnabled() ?? false;
          var bPvp = bAsPlayer.IsPVPEnabled();
          return aPvp || bPvp;
        }
        if (bIsCaptured && aAsPlayer != null) {
          var aPvp = aAsPlayer.IsPVPEnabled();
          var bPvp = b.GetOwnerPlayer()?.IsPVPEnabled() ?? false;
          return aPvp || bPvp;
        }

        if (aIsCaptured && bIsCaptured) {
          if (aOwner == bOwner) {
            // Captured vs Captured on same team: not an enemy.
            return false;
          } else {
            // Captured vs Captured on different team: depends on PVP setting
            var aPvp = a.GetOwnerPlayer()?.IsPVPEnabled() ?? false;
            var bPvp = b.GetOwnerPlayer()?.IsPVPEnabled() ?? false;
            return aPvp || bPvp;
          }
        }

        // Captured vs Non-Captured: always an enemy.
        if (aIsCaptured != bIsCaptured) {
          return true;
        }

        // Otherwise, let the original algorithm's result stand, and keep
        // normal allegiences.
        return originalResult;
      }
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    class CapturedMonstersHoverText_Patch {
      static string Postfix(string originalResult, Tameable __instance) {
        var tameable = __instance;
        var monster = tameable.m_character;
        var localPlayer = Player.m_localPlayer;

        if (monster.IsCaptured()) {
          var owner = monster.GetOwnerName();
          var text = $"{monster.m_name} ( Owner: {owner} )";

          // In PVP, monster can only be petted or renamed by their owner.
          // In non-PVP, any player can pet or rename a monster.
          if (monster.AlliedWith(localPlayer)) {
            text += "\n[<color=yellow><b>$KEY_Use</b></color>] $hud_pet";
            text += "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $hud_rename";
          }

          return Localization.instance.Localize(text);
        }

        return originalResult;
      }
    }

    // Not all captured monsters can be pet or renamed by just anyone.
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
    class CapturedMonstersPettable_Patch {
      static bool Prefix(Tameable __instance, ref bool __result, Humanoid user, bool hold, bool alt) {
        var tameable = __instance;
        var monster = tameable.m_character;

        if (monster.AlliedWith(user as Player) == false) {
          __result = false;
          return false;
        }

        return true;
      }
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.SetName))]
    class IncreasePetNameLength_Patch {
      static bool Prefix(Tameable __instance) {
        var tameable = __instance;
        if (tameable.m_character.IsTamed()) {
          TextInput.instance.RequestText(
              tameable, "$hud_rename", MaxPetNameLength);
        }
        return false;
      }
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.SetText))]
    class SettingPetNameToOriginalMonsterNameClearsThePetName_Patch {
      static void Prefix(Tameable __instance, ref string text) {
        var tameable = __instance;
        var genericName = tameable.m_character.m_name;
        var localizedGenericName = Localization.instance.Localize(genericName);

        // If the name set is the original generic name of the species, clear
        // the special name.  This matches Pokemon Go behvaior.
        if (text.ToLower() == localizedGenericName.ToLower()) {
          text = "";
        }
      }
    }

    // Hide the boss HUD is the boss is captured.  Do this by pretending it's
    // not a boss, therefore forcing the game to use the regular enemy HUD for
    // this monster.
    [HarmonyPatch(typeof(Character), nameof(Character.IsBoss))]
    class CapturedMonstersHud_Patch {
      static void Postfix(Character __instance, ref bool __result) {
        var monster = __instance;
        if (monster.IsCaptured()) {
          __result = false;
        }
      }
    }

    // If we disable PVP, other players' monsters should stop attacking.
    [HarmonyPatch(typeof(Player), nameof(Player.SetPVP))]
    class CapturedMonstersStopAttackingWhenPVPDisabled_Patch {
      static void Postfix(Player __instance, bool enabled) {
        var player = __instance;

        if (!enabled) {
          foreach (Character monster in Character.GetAllCharacters()) {
            if (monster.IsCaptured()) {
              var target = monster.GetTarget();
              if (target != null && BaseAI.IsEnemy(monster, target) == false) {
                monster.SetTarget(null);
              }
            }
          }
        }
      }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    class MonstersDoNotProcreate_Patch {
      static void Postfix(Character __instance) {
        var monster = __instance;
        var procreation = monster.GetComponent<Procreation>();
        if (procreation != null) {
          UnityEngine.Object.Destroy(procreation);
        }
      }
    }

    public static void SyncCapturedStatus(
        GameObject newSpawn, SpawnAbility spawner) {
      var spawnCharacter = newSpawn.GetComponent<Character>();

      if (spawnCharacter != null && spawner.m_owner.IsCaptured()) {
        Logger.LogDebug($"Syncing captured status, owner: {spawner.m_owner}, spawn: {spawnCharacter}");
        var owner = spawner.m_owner.GetOwnerName();
        spawnCharacter.ObeyMe(owner);

        // This subordinate monster will not return when called.  Instead, it
        // will disappear.
        spawnCharacter.SetNoReturn(true);
      }
    }

    // Modifies SpawnAbility.Spawn.  Can't be done with HarmonyPatch
    // annotations because the original implementation involves a nested class
    // without a real name.
    public static IEnumerable<CodeInstruction> SpawnAbilityTranspiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator) {
      var syncCapturedStatus = typeof(Captured).GetMethod("SyncCapturedStatus");

      var phases = new TranspilerSequence.Phase[] {
        new TranspilerSequence.Phase {
          matcher = code => (code.opcode == OpCodes.Call &&
                             (code.operand as MethodInfo).Name == "Instantiate"),
          replacer = code => new CodeInstruction[] {
            // Call Instantiate().
            code,

            // Right after the spawned object is instantiated, call out to our
            // method to make sure that spawns of captured monsters are also
            // captured.
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Ldloc_1),
            new CodeInstruction(OpCodes.Call, syncCapturedStatus),
          },
        },
      };
      return TranspilerSequence.Execute("SpawnAbility", phases, instructions);
    }

    // Don't make noise about your arrival.  I'm looking at you, bosses!
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.Awake))]
    class MonstersDoNotAnnounceThemselves_Patch {
      static void Prefix(BaseAI __instance) {
        __instance.m_spawnMessage = "";
        __instance.m_deathMessage = "";
      }
    }
  }
}
