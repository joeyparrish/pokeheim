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

namespace Pokeheim {
  public static class OdinMods {
    // These are keys which will be used to store additional fields in ZDO.
    private const string IsStaticKey = "com.pokeheim.IsStatic";

    private static Odin staticOdin = null;

    public class OdinInteraction : MonoBehaviour, Hoverable, Interactable {
      private bool readyToMurder = false;

      private const float textOffset = 1.5f;
      private const float textCullDistance = 20f;
      private const float dialogVisibleTime = 10f;
      private const float templeRadius = 8f;

      private void Awake() {
        staticOdin = GetComponent<Odin>();

        ZRoutedRpc.instance.Register<string, string>(
            "PokeheimOdinMessage", RPC_OdinMessage);
        ZRoutedRpc.instance.Register(
            "PokeheimOdinMurders", RPC_OdinMurders);
      }

      private void SayToAll(string topic, string text) {
        ZRoutedRpc.instance.InvokeRoutedRPC(
            ZRoutedRpc.Everybody, "PokeheimOdinMessage", topic, text);
      }

      private void RPC_OdinMessage(long sender, string topic, string text) {
        Chat.instance.SetNpcText(
            gameObject,
            Vector3.up * textOffset,
            textCullDistance,
            dialogVisibleTime,
            topic,
            text,
            /* large */ true);
      }

      private void OnDestroy() {
        staticOdin = null;
      }

      private void Update() {
        if (readyToMurder == false) {
          // Not activated yet.
          return;
        }

        var distanceToLocalPlayer = Vector3.Distance(
            transform.position, Player.m_localPlayer.transform.position);
        if (distanceToLocalPlayer > templeRadius) {
          // The local player ran away!
          DoMurders();
        }
      }

      public string GetHoverName() {
        return Localization.instance.Localize("$odin");
      }

      public string GetHoverText() {
        return Localization.instance.Localize(
            "$odin\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact");
      }

      public bool Interact(Humanoid character, bool hold, bool alt) {
        if (hold) {
          return false;
        }

        if (readyToMurder) {
          // The player is dismissing the chat dialog.
          DoMurders();
        } else {
          // The player is activating the chat dialog.
          var activatingPlayer = character as Player;

          var allPlayers = Player.GetAllPlayers();

          readyToMurder = true;
          foreach (var player in allPlayers) {
            var distance = Vector3.Distance(
                transform.position, player.transform.position);
            if (distance > templeRadius) {
              readyToMurder = false;
            }
          }

          if (readyToMurder) {
            var multiple = allPlayers.Count > 1;
            SayToAll(
                "$odin_congratulations_topic",
                "$odin_congratulations_text_" +
                (multiple ? "multi" : "single"));
          } else {
            SayToAll("$odin_wait_topic", "$odin_wait_text");
          }
        }

        return false;
      }

      public bool UseItem(Humanoid user, ItemDrop.ItemData item) {
        return false;
      }

      private void DoMurders() {
        ZRoutedRpc.instance.InvokeRoutedRPC(
            ZRoutedRpc.Everybody, "PokeheimOdinMurders");
      }

      private void RPC_OdinMurders(long sender) {
        Chat.instance.ClearNpcText(gameObject);

        var player = Player.m_localPlayer;

        // Kill the player, but suppress the "death" tutorial if it hasn't
        // been seen before.
        player.SetSeenTutorial("death");
        player.Damage(new HitData {
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
          // TODO: Should the despawn be called by one specific player?
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

    // This is a fork of Odin's Update() method.  Static Odin won't disappear,
    // and he'll face the weighted center of all Players instead of the closest
    // one.
    public static void UpdateStaticOdin(this Odin odin) {
      if (odin.m_nview == null || !odin.m_nview.IsOwner()) {
        return;
      }

      var allPlayers = Player.GetAllPlayers();
      if (allPlayers.Count == 0) {
        return;
      }

      var totalWeight = 0f;
      var totalWeightedPosition = Vector3.zero;

      foreach (var player in allPlayers) {
        var distance = Vector3.Distance(
            odin.transform.position,
            player.transform.position);

        var weight = 1f / distance;
        totalWeight += weight;
        totalWeightedPosition += player.transform.position * weight;
      }

      var averagePosition = totalWeightedPosition / totalWeight;

      var forward = averagePosition - odin.transform.position;
      forward.y = 0f;
      forward.Normalize();

      odin.transform.rotation = Quaternion.LookRotation(forward);
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

        // Call a modified version.
        odin.UpdateStaticOdin();

        // Suppress the original.
        return false;
      }
    }
  }
}
