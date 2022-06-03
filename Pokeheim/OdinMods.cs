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
using UnityEngine;

using Logger = Jotunn.Logger;

// TODO: Can all Players see Odin when he spawns for just one?
namespace Pokeheim {
  public static class OdinMods {
    // These are keys which will be used to store additional fields in ZDO.
    private const string IsStaticKey = "com.pokeheim.IsStatic";

    private static Odin staticOdin = null;

    public class OdinInteraction : MonoBehaviour, Hoverable, Interactable {
      private Player activatingPlayer = null;

      private const float textOffset = 1.5f;
      private const float textCullDistance = 20f;
      private const float dialogVisibleTime = 10f;

      private void Awake() {
        staticOdin = GetComponent<Odin>();
      }

      private void OnDestroy() {
        staticOdin = null;
      }

      private void Update() {
        if (activatingPlayer == null) {
          // Not activated yet.
          return;
        }

        var distanceToPlayer = Vector3.Distance(
            transform.position, activatingPlayer.transform.position);
        if (distanceToPlayer > textCullDistance) {
          // The player ran away!
          DoMurder();
        }
      }

      public string GetHoverName() {
        return Localization.instance.Localize("$odin");
      }

      public string GetHoverText() {
        return Localization.instance.Localize("$odin\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact");
      }

      public bool Interact(Humanoid character, bool hold, bool alt) {
        if (hold) {
          return false;
        }

        if (character == activatingPlayer) {
          // The player is dismissing the chat dialog.
          DoMurder();
        } else if (activatingPlayer == null) {
          // The player is activating the chat dialog.
          activatingPlayer = character as Player;

          Chat.instance.SetNpcText(
              gameObject,
              Vector3.up * textOffset,
              textCullDistance,
              dialogVisibleTime,
              "$odin_congratulations_topic",
              "$odin_congratulations_text",
              /* large */ true);
        }

        return false;
      }

      public bool UseItem(Humanoid user, ItemDrop.ItemData item) {
        return false;
      }

      private void DoMurder() {
        Chat.instance.ClearNpcText(gameObject);

        // Kill the player, but suppress the "death" tutorial if it hasn't
        // been seen before.
        activatingPlayer.SetSeenTutorial("death");
        activatingPlayer.Damage(new HitData {
          m_damage = {
            m_damage = 1E+10f,
          },
        });

        // Wait for dramatic effect...
        this.DelayCall(10f /* seconds */, delegate {
          Credits.Roll(withOutro: true);

          // Despawn Odin.  Since this delayed call is attached to him, this
          // step must come last.
          Despawn();
        });
      }

      private void Despawn() {
        var odin = GetComponent<Odin>();
        odin.m_despawn.Create(transform.position, transform.rotation);
        odin.m_nview.Destroy();
      }
    }

    public static void SpawnStaticOdin() {
      // Just because he's the All-Father doesn't mean you can have as many of
      // him as you want.
      if (staticOdin != null) {
        return;
      }

      Vector3 templePosition;
      ZoneSystem.instance.GetLocationIcon("StartTemple", out templePosition);

      var odinPrefab = PrefabManager.Instance.GetPrefab("odin");
      var rotation = Quaternion.identity;
      var clone = UnityEngine.Object.Instantiate(
          odinPrefab, templePosition, rotation);

      clone.GetComponent<Odin>().SetStatic();
      Logger.LogDebug($"Spawned static Odin: {clone}");
    }

    public static void SetStatic(this Odin odin) {
      // Set the static flag.
      odin.SetExtraData(IsStaticKey, true);

      // Make other mods for static Odin.  These may need to be reapplied on
      // reload, so take care to make this method work when called more than
      // once.

      // Make static Odin impossible to push around by removing his physics.
      var body = odin.GetComponent<Rigidbody>();
      if (body != null) {
         UnityEngine.Object.Destroy(body);
      }

      // Make Odin interactable if he's not already.
      if (odin.GetComponent<OdinInteraction>() == null) {
        odin.gameObject.AddComponent<OdinInteraction>();
      }
    }

    public static bool IsStatic(this Odin odin) {
      return odin.GetExtraData(IsStaticKey, false);
    }

    // Find existing static Odin characters when we reload the game.
    [HarmonyPatch(typeof(Odin), nameof(Odin.Awake))]
    class TrackOdin_Patch {
      static void Postfix(Odin __instance) {
        Odin odin = __instance;

        if (odin.IsStatic()) {
          // The flag is already set, but we may need to reapply other mods to
          // static Odin.
          odin.SetStatic();
        }
      }
    }

    // Change the Update() logic for a static Odin.
    [HarmonyPatch(typeof(Odin), nameof(Odin.Update))]
    class OdinCanBeStatic_Patch {
      static bool Prefix(Odin __instance) {
        Odin odin = __instance;

        if (odin.IsStatic() == false) {
          // Call the original method.
          return true;
        }

        // This is a fork of Odin's Update() method that does everything except
        // disappear.
        if (odin.m_nview != null && odin.m_nview.IsOwner()) {
          // Face the closest player.
          Player closestPlayer = Player.GetClosestPlayer(
              odin.transform.position, 100f);
          if (closestPlayer != null) {
            Vector3 forward =
                closestPlayer.transform.position - odin.transform.position;
            forward.y = 0f;
            forward.Normalize();
            odin.transform.rotation = Quaternion.LookRotation(forward);
          }
        }

        // Suppress the original.
        return false;
      }
    }
  }
}
