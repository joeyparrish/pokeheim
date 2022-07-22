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
using UnityEngine;

using Logger = Jotunn.Logger;

// Monsters LOVE berries.  Everyone in Pokeheim knows this.
namespace Pokeheim {
  public static class Berries {
    private static readonly Dictionary<string, float> BerryCatchRates =
        new Dictionary<string, float> {
      {"Raspberry",   1.5f},
      {"Blueberries", 2.0f},
      {"Cloudberry",  2.5f},
    };

    private static EffectList EatEffect = null;

    [PokeheimInit]
    public static void Init() {
      Utils.OnVanillaPrefabsAvailable += delegate {
        // How is a boar a humanoid?  So weird.
        EatEffect = Utils.StealFromPrefab<Humanoid, EffectList>(
            "Boar", humanoid => humanoid.m_consumeItemEffects);
      };
    }

    public static bool IsBerry(this ItemDrop item) {
      return BerryCatchRates.ContainsKey(item.GetPrefabName());
    }

    public static float BerryCatchRate(this ItemDrop item) {
      if (BerryCatchRates.TryGetValue(item.GetPrefabName(), out var rate)) {
        return rate;
      }
      return 0f;
    }

    private static void RegisterCustomRPCs(this BaseAI baseAI) {
      baseAI.m_nview.Register(
          "PokeheimRelax",
          sender => baseAI.RPC_Relax(sender));
    }

    private static void Relax(this BaseAI baseAI) {
      if (baseAI.m_nview.IsValid() && baseAI.IsAlerted()) {
        if (baseAI.m_nview.IsOwner()) {
          baseAI.SetAlerted(alert: false);
        } else {
          // Send an RPC to the owner.
          baseAI.m_nview.InvokeRPC("PokeheimRelax");
        }
      }
    }

    private static void RPC_Relax(this BaseAI baseAI, long sender) {
      if (baseAI.m_nview.IsOwner()) {
        baseAI.SetAlerted(alert: false);
      }
    }

    public class BerryEater : MonoBehaviour {
      public const float BerrySearchInterval = 10f;  // seconds
      public const float BerrySearchRadius = 10f;  // meters
      public const float MaxBerryEatAngle = 20f;

      private ItemDrop Target = null;
      private float SearchTimer = 0f;
      private Character Monster;
      private float BerryCatchRate = 1f;
      private float BerryEatRadius = 1f;  // meters, filled in later

      public void Awake() {
        Monster = GetComponent<Character>();

        // | monster | eating distance | radius | ratio |
        // | ------- | --------------- | ------ | ----- |
        // | boar    | 1.0             | 0.5    | 2.0   |
        // | wolf    | 1.4             | 0.65   | 2.15  |
        // | lox     | 4.0             | 1.5    | 2.666 |
        //
        // A fixed ratio of 2.2 seems to look good for all of those monsters.
        // Also tested on necks and trolls, and seems fine generally.
        BerryEatRadius = Monster.GetRadius() * 2.2f;
      }

      public void NoticeBerries(ItemDrop item) {
        if (Target == null) {
          Target = item;
        }
      }

      // Based on the most recent berry this monster ate.
      public float GetBerryCatchRate() {
        return BerryCatchRate;
      }

      public bool EatBerries() {
        SearchTimer += Time.fixedDeltaTime;

        if (SearchTimer > BerrySearchInterval) {
          SearchTimer = 0;
          var oldTarget = Target;

          // If we still have a reachable target, don't change it.
          if (Target == null || !CanReach(Target)) {
            Target = FindNearestBerry();
          }
        }

        if (Target == null) {
          return false;
        }

        // If there's a berry to eat, stop fighting.
        Monster.SetTarget(null);
        var baseAI = Monster.m_baseAI;
        baseAI.Alert();

        // Seek the berry!
        var targetPosition = Target.transform.position;
        if (baseAI.MoveTo(
            Time.fixedDeltaTime, targetPosition, BerryEatRadius, run: false)) {
          // Eat the berry!
          baseAI.LookAt(targetPosition);
          if (baseAI.IsLookingAt(targetPosition, MaxBerryEatAngle) &&
              Target.RemoveOne()) {
            EatEffect.Create(Monster.transform.position, Quaternion.identity);
            Monster.m_animator.SetTrigger("consume");
            BerryCatchRate = Target.BerryCatchRate();
            // Drop the target after eating, so the monster must wait another
            // interval before eating another berry from a stack.
            Target = null;
            baseAI.Relax();
          }
        }

        return true;
      }

      private bool CanReach(ItemDrop target) {
        return Monster.m_baseAI.HavePath(target.transform.position);
      }

      private ItemDrop FindNearestBerry() {
        var mask = LayerMask.GetMask("item");
        var colliders = Physics.OverlapSphere(
            Monster.transform.position, BerrySearchRadius, mask);

        ItemDrop closest = null;
        float closestDistance = 1e12f;

        foreach (var collider in colliders) {
          if (!collider.attachedRigidbody) {
            continue;
          }

          ItemDrop item = collider.attachedRigidbody.GetComponent<ItemDrop>();
          if (!item.m_nview.IsValid() ||
              !item.IsBerry() ||
              !CanReach(item)) {
            continue;
          }

          var distance = Vector3.Distance(
              item.transform.position, Monster.transform.position);
          if (distance < closestDistance) {
            closestDistance = distance;
            closest = item;
          }
        }

        return closest;
      }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    class MonstersAreBerryEaters_Patch {
      static void Postfix(Character __instance) {
        var monster = __instance;
        if (!monster.IsPlayer()) {
          monster.gameObject.AddComponent<BerryEater>();
        }
      }
    }

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FixedUpdate))]
    class WildMonstersWantToEatBerries_Patch {
      static bool Prefix(BaseAI __instance) {
        var monster = __instance.m_character;
        // TODO: GetComponent on FixedUpdate... how to avoid?
        var berryEater = monster.GetComponent<BerryEater>();

        if (!monster.IsCaptured() &&
            !monster.IsFainted() &&
            (berryEater?.EatBerries() ?? false)) {
          // Skip the original.
          return false;
        }
        return true;
      }
    }

    // Disable the normal consumption algorithm for monsters, in favor of our
    // own EatBerries() method.
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateConsumeItem))]
    class MonstersOnlyEatBerries_Patch {
      static bool Prefix(ref bool __result) {
        __result = false;
        return false;
      }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.OnPlayerDrop))]
    class MonstersNoticeWhenYouDropBerries_Patch {
      static void Postfix(ItemDrop __instance) {
        var item = __instance;

        if (!item.IsBerry()) {
          return;
        }

        foreach (var monster in Character.GetAllCharacters()) {
          if (monster.IsFainted()) {
            continue;
          }
          var berryEater = monster.GetComponent<BerryEater>();
          if (berryEater == null) {
            continue;
          }

          var distance = Utils.DistanceXZ(
              item.transform.position, monster.transform.position);
          if (distance < BerryEater.BerrySearchRadius) {
            berryEater.NoticeBerries(item);
          }
        }
      }
    }

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.Awake))]
    class SetupBerryRPCs_Patch {
      static void Postfix(BaseAI __instance) {
        var baseAI = __instance;
        baseAI.RegisterCustomRPCs();
      }
    }
  }
}
