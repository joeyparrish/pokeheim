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
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using MountUp;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using Logger = Jotunn.Logger;

// TODO: Make it easier to mount tall things (Troll, gd_king, Dragon)
// TODO: Prevent player riding another player's monster?
namespace Pokeheim {
  public static class Riding {
    public static ItemDrop UniversalSaddleItem = null;
    private static GameObject SaddlePrefab = null;
    private const string SaddleName = "$item_saddle_pokeheim";

    [PokeheimInit]
    public static void Init() {
      // This is the main thing we need from MountUp.
      SaddlePrefab = MountUp.Setup.genericSaddlePrefab;

      Utils.OnVanillaPrefabsAvailable += delegate {
        // Create a custom, universal saddle item.
        CustomItem customItem = new CustomItem(
            "SaddleUniversal", "SaddleLox", new ItemConfig {
          Name = SaddleName,
          Description = "$item_saddle_pokeheim_description",
          Amount = 1,
          Requirements = new[] {
            new RequirementConfig {
              Item = "LeatherScraps",
              Amount = 10,
            },
          },
        });
        // Shrink the item drop visually.  It's huge otherwise.
        customItem.ItemPrefab.transform.localScale *= 0.6f;

        ItemManager.Instance.AddItem(customItem);
        UniversalSaddleItem = customItem.ItemDrop;

        // Undo the effects of MountUp.SetupMountable.  We have our own
        // Mountable class, and we would rather suppress this aspect of MountUp.
        var tameables = Resources.FindObjectsOfTypeAll<Tameable>();
        foreach (var tameable in tameables) {
          var mountUpMountable = tameable.GetComponent<MountUp.Mountable>();
          if (mountUpMountable != null) {
            Logger.LogDebug($"Removed MountUp class from {tameable}");
            UnityEngine.Object.Destroy(mountUpMountable);
          }
        }

        // Undo the effects of MountUp.CreateSaddles.  We have our own
        // universal saddle item, and we don't want the ones from MountUp.
        ItemManager.Instance.RemoveItem("SaddleBoar");
        ItemManager.Instance.RemoveItem("SaddleWolf");
      };
    }

    public class Mountable : MonoBehaviour {
      // The offset from the saddle object to the rider, so that the player's
      // hands are on the pommel.  With a consistent, single saddle size, this
      // is a constant.
      private static readonly Vector3 riderOffset =
          new Vector3(0.05f, -0.20f, 0.25f);

      public void Awake() {
        var prefabName = this.GetPrefabName();
        var metadata = MonsterMetadata.Get(prefabName);
        var tameable = GetComponent<Tameable>();
        if (metadata == null) {
          Logger.LogError($"Unable to find metadata for monster {prefabName}");
          return;
        } else if (metadata.MountPointPath == null) {
          Logger.LogError($"Monster {prefabName} has no defined mount point!");
          return;
        }

        // This is a monster body part where we will base the mount point.
        var visualTransform = transform.Find("Visual");
        var mountPointBase = visualTransform.Find(metadata.MountPointPath);
        if (mountPointBase == null) {
          // Deal with this case now.  Otherwise, we may lose saddles attached
          // to this monster!
          Logger.LogError($"Monster {prefabName} has nonexistent mount point: {metadata.MountPointPath}!");
          tameable.m_saddleItem = null;
          return;
        }

        // Make sure saddle instantiation doesn't throw an exception if these
        // are already registered.  Not sure how this happens, but it has
        // started.
        var nview = GetComponent<ZNetView>();
        nview.Unregister("RequestControl");
        nview.Unregister("ReleaseControl");
        nview.Unregister("RequestRespons");
        nview.Unregister("RemoveSaddle");
        nview.Unregister("Controls");

        // All monsters can be ridden with a universal saddle item.
        tameable.m_saddleItem = UniversalSaddleItem;

        // Monsters always drop their saddle on death/capture.
        tameable.m_dropSaddleOnDeath = true;

        // This will be the parent of the saddle object.
        // It will be attached exactly to the mount point base, and will reset
        // the global scale to 1x.  This will insulate the saddle object from
        // the scale of the monster body, so that adjustments to the saddle or
        // rider position will be consistent and accurate.  Further, the
        // insulator will be rotated so that the saddle's "forwards" and "up"
        // vectors will match the monster's.
        var saddleInsulator = new GameObject("saddleInsulator");
        saddleInsulator.transform.parent = mountPointBase;
        saddleInsulator.transform.localPosition = Vector3.zero;
        saddleInsulator.transform.SetGlobalScaleToOne();

        // This is the rotation of the mount point base relative to the entire
        // monster.  This will cancel out any rotation of the base, so that the
        // saddle will face the right direction no matter what we attach it to.
        var resetRotation =
            Quaternion.Inverse(mountPointBase.transform.rotation) *
            transform.rotation;
        saddleInsulator.transform.localRotation = resetRotation;

        // The saddle object itself, where we apply a positioning offset that
        // differs per monster.
        var saddleGameObject = UnityEngine.Object.Instantiate(
            SaddlePrefab, visualTransform);
        saddleGameObject.transform.parent = saddleInsulator.transform;
        saddleGameObject.transform.localPosition = metadata.SaddleOffset;
        saddleGameObject.transform.localScale = Vector3.one;
        saddleGameObject.transform.localRotation =
            Quaternion.Euler(metadata.SaddleRotation);

        // Initially inactive until the saddle item is used on the monster.
        saddleGameObject.SetActive(value: false);

        // This is the rider's position, which is offset somewhat from the
        // saddle position by a fixed amount.
        var riderPosition = new GameObject("riderPosition");
        riderPosition.transform.parent = saddleGameObject.transform;
        riderPosition.transform.localPosition = riderOffset;
        riderPosition.transform.localScale = Vector3.one;
        riderPosition.transform.localRotation = Quaternion.identity;

        // The saddle object has a Sadle [sic] component, where we need to set
        // the rider's position explicitly.
        var saddle = saddleGameObject.GetComponent<Sadle>();
        saddle.m_attachPoint = riderPosition.transform;

        // Set the saddle object for this monster.
        var oldSaddle = tameable.m_saddle;
        tameable.m_saddle = saddle;

        // If the monster was not already mountable, these didn't get added in
        // Tameable.Awake.  Add them now.  If m_nview is null, Tameable.Awake
        // hasn't been called yet.
        if (oldSaddle == null && tameable.m_nview != null) {
          tameable.m_nview.Register("AddSaddle", (Action<long>)tameable.RPC_AddSaddle);
          tameable.m_nview.Register("SetSaddle", (Action<long, bool>)tameable.RPC_SetSaddle);
          tameable.SetSaddle(tameable.HaveSaddle());
        }
      }
    }

    // The FindHoverObject method finds the nearest object in the path of the
    // cursor.  However, for some monsters, the monster "extends" beyond the
    // saddle, making it extremely difficult to use or remove the saddle.
    // This patch fudges the "distance" measurement for saddle objects so that
    // they are easier to interact with.
    [HarmonyPatch(typeof(Player), nameof(Player.FindHoverObject))]
    class PreferToHoverOverSaddles_Patch {
      // The "advantage" we give to saddles in sorting objects by distance.
      // Effectively, for the purposes of hovering, a saddle is this much
      // closer than it really is (in meters).
      private const float SaddleAdvantage = 20f;

      // This is the same logic FindHoverObject uses to decide what object to
      // return for each hit.
      private static T HitComponent<T>(RaycastHit hit) {
        var hoverable = hit.collider.GetComponent<Hoverable>();
        if (hoverable != null) {
          return hit.collider.gameObject.GetComponent<T>();
        }
        if (hit.collider.attachedRigidbody != null) {
          return hit.collider.attachedRigidbody.gameObject.GetComponent<T>();
        }
        return hit.collider.gameObject.GetComponent<T>();
      }

      // Sort the hits as FindHoverObject would, but with an advantage given to
      // saddles.
      public static void CustomSort(RaycastHit[] array) {
        Array.Sort(array, (RaycastHit x, RaycastHit y) => {
          float dx = x.distance;
          float dy = y.distance;

          // Give saddles an advantage in sorting by distance.
          if (HitComponent<Sadle>(x) != null) {
            dx -= SaddleAdvantage;
          }
          if (HitComponent<Sadle>(y) != null) {
            dy -= SaddleAdvantage;
          }
          return dx.CompareTo(dy);
        });
      }

      // Patch in our custom sort method so we prefer to hover on saddles.
      static IEnumerable<CodeInstruction> Transpiler(
          IEnumerable<CodeInstruction> instructions,
          ILGenerator generator) {
        var sortMethod = typeof(PreferToHoverOverSaddles_Patch).GetMethod(
            nameof(PreferToHoverOverSaddles_Patch.CustomSort));

        foreach (var code in instructions) {
          var methodInfo = code.operand as MethodInfo;

          // Seeking Array::Sort(
          //   UnityEngine.RaycastHit[] array,
          //   System.Comparison<UnityEngine.RaycastHit> comparison)
          if (code.opcode == OpCodes.Call &&
              methodInfo != null && methodInfo.Name == "Sort") {
            yield return code;
            // The top of the stack is the sorted array.

            // Duplicate the array ref on the stack.
            yield return new CodeInstruction(OpCodes.Dup);
            // Call our custom sorting method, which consumes one stack element.
            yield return new CodeInstruction(OpCodes.Call, sortMethod);
            // Now the array has been sorted again using our custom method.

            // We leave one array ref on the stack, which is where we started.
          } else {
            yield return code;
          }
        }
      }
    }

    [HarmonyPatch(typeof(Sadle))]
    class DisableSaddleRestrictions_Patch {
      [HarmonyPrefix]
      [HarmonyPatch(nameof(Sadle.UpdateDrown))]
      static bool disableDrowning() { return false; }

      [HarmonyPrefix]
      [HarmonyPatch(nameof(Sadle.UpdateStamina))]
      static bool disableStaminaUpdate() { return false; }

      [HarmonyPrefix]
      [HarmonyPatch(nameof(Sadle.UseStamina))]
      static bool disableStaminaUse() { return false; }
    }

    [HarmonyPatch]
    class DismountIsAlwaysSafe_Patch {
      static bool dismounting = false;
      static bool complete = false;

      /* TODO: This doesn't work, because when we "land" from a normal
       * dismount, it's the monster we land on, not the ground.  So this patch
       * doesn't really save us in this case.  We are at least saved from a
       * "dismount" triggered by recalling the monster (via OnDeath below).
      [HarmonyPrefix]
      [HarmonyPatch(typeof(Sadle), nameof(Sadle.OnUseStop))]
      static void onDismount(Sadle __instance, Player player) {
        if (player == Player.m_localPlayer) {
          dismounting = true;
        }
      }
      */

      [HarmonyPrefix]
      [HarmonyPatch(typeof(Tameable), nameof(Tameable.OnDeath))]
      static void onMountDeathOrCapture(Tameable __instance) {
        var tameable = __instance;
        if (tameable.HaveSaddle() && tameable.m_saddle.IsLocalUser()) {
          dismounting = true;
        }
      }

      [HarmonyPrefix]
      [HarmonyPatch(typeof(Character), nameof(Character.UpdateGroundContact))]
      static void trackGroundPrefix(Character __instance) {
        var character = __instance;
        if (character as Player == Player.m_localPlayer &&
            dismounting && character.m_groundContact) {
          // The game logic is about to register a hit on the ground.  Reset
          // the altitude so that it appears to be from a distance of zero and
          // doesn't hurt.
          character.m_maxAirAltitude = character.transform.position.y;
          complete = true;
        }
      }

      [HarmonyPostfix]
      [HarmonyPatch(typeof(Character), nameof(Character.UpdateGroundContact))]
      static void trackGroundPostfix(Character __instance) {
        var character = __instance;
        if (character as Player == Player.m_localPlayer &&
            dismounting && complete) {
          // The game logic has just completed dealing damage (if any) for a
          // fall.  Now we can reset our flags.
          dismounting = false;
          complete = false;
        }
      }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    class FlyAndAttackControls_Patch {
      static void Prefix(
          Player __instance,
          ref Vector3 movedir,
          ref bool jump, ref bool crouch, ref bool attack) {
        var player = __instance;
        var saddle = player.m_doodadController as Sadle;
        var steed = saddle?.m_character;
        var canFly = (steed?.m_baseAI.m_randomFly ?? false) ||
                     (steed?.IsFlying() ?? false);

        if (canFly) {
          // If the player is riding a flying monster with a saddle, use jump
          // and crouch to direct that monster up or down.
          if (jump) {
            MoveSteed(saddle, player.GetLookDir() + Vector3.up);
          } else if (crouch) {
            MoveSteed(saddle, player.GetLookDir() + Vector3.down);
          }

          // Turn these off now so that the player isn't forced to dismount.
          jump = false;
          crouch = false;
        }

        if (steed != null && attack) {
          // Make the steed attack for us.
          steed.StartAttack(null, false);

          // Turn this off so that the player isn't forced to dismount.
          attack = false;
        }
      }

      // This bypasses the game logic in Sadle.ApplyControlls() that would
      // remove the "y" component from the direction.
      static void MoveSteed(Sadle saddle, Vector3 direction) {
        saddle.m_nview.InvokeRPC(
            "Controls", direction, (int)Sadle.Speed.Walk, saddle.m_rideSkill);
      }
    }

    // The HUD for riding has a hard-coded icon for Lox.  We should fix that,
    // and make some other tweaks for Pokeheim.
    [HarmonyPatch]
    class TweakMountHud_Patch {
      [HarmonyPostfix]
      [HarmonyPatch(typeof(Sadle), nameof(Sadle.RPC_RequestRespons))]
      static void Postfix(Sadle __instance, bool granted) {
        var saddle = __instance;
        if (granted && saddle.IsLocalUser()) {
          var steed = saddle.GetCharacter();
          var metadata = MonsterMetadata.Get(steed.GetPrefabName());
          var panel = Hud.instance.m_mountPanel;
          var steedIcon = panel.transform.Find("MountPanel/MountIcon");
          var staminaBar = panel.transform.Find("MountPanel/Stamina") as RectTransform;

          // One-time changes applied when the user mounts the monster.
          // 1. Change the icon to show the correct monster.
          steedIcon.GetComponent<Image>().sprite = metadata.TrophyIcon;
          // 2. Hide the stamina bar, since we mounted monsters have infinite
          //    stamina.
          staminaBar.gameObject.SetActive(value: false);
          // 3. Center the name over the health bar instead of the entire panel.
          var namePanel = panel.transform.Find("MountPanel/Name") as RectTransform;
          namePanel.anchoredPosition = new Vector2(
              staminaBar.anchoredPosition.x / 2, namePanel.anchoredPosition.y);
        }
      }

      [HarmonyPostfix]
      [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateMount))]
      static void tweakMountText() {
        var saddle = Player.m_localPlayer.GetDoodadController() as Sadle;
        if (saddle != null) {
          var steed = saddle.GetCharacter();
          // Every time the panel is updated, tweak the text to show only the
          // name.  A status such as "hungry" is meaningless for us.
          Hud.instance.m_mountNameText.text = steed.GetHoverName();
        }
      }
    }

#if DEBUG
    // X, Y, and Z keys adjust in those axes.
    // +X is to the monster's right, +Z is the direction it faces, and +Y is
    // toward the sky.
    // Left-Alt means a position change of 0.05 m.
    // Right-Alt means a rotation of 5.00 degrees.
    // Left-Control means to invert the change.
    // Left-Command/Left-Windows means to make the change small (20% normal).
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    class AdjustMount_Patch {
      static void Postfix() {
        var hit = false;
        var posAdjustment = Vector3.zero;
        var rotAdjustment = Vector3.zero;

        if (Input.GetKey(KeyCode.LeftAlt)) {
          if (Input.GetKeyDown(KeyCode.X)) {
            posAdjustment = new Vector3(0.05f, 0, 0);
            hit = true;
          }
          if (Input.GetKeyDown(KeyCode.Y)) {
            posAdjustment = new Vector3(0, 0.05f, 0);
            hit = true;
          }
          if (Input.GetKeyDown(KeyCode.Z)) {
            posAdjustment = new Vector3(0, 0, 0.05f);
            hit = true;
          }
        } else if (Input.GetKey(KeyCode.RightAlt)) {
          if (Input.GetKeyDown(KeyCode.X)) {
            rotAdjustment = new Vector3(5f, 0, 0);
            hit = true;
          }
          if (Input.GetKeyDown(KeyCode.Y)) {
            rotAdjustment = new Vector3(0, 5f, 0);
            hit = true;
          }
          if (Input.GetKeyDown(KeyCode.Z)) {
            rotAdjustment = new Vector3(0, 0, 5f);
            hit = true;
          }
        }

        if (hit) {
          if (Input.GetKey(KeyCode.LeftControl)) {
            rotAdjustment *= -1f;
            posAdjustment *= -1f;
          }
          if (Input.GetKey(KeyCode.LeftCommand) ||
              Input.GetKey(KeyCode.LeftWindows)) {
            rotAdjustment *= 0.2f;
            posAdjustment *= 0.2f;
          }

          Mountable saddled = null;

          foreach (var monster in Character.GetAllCharacters()) {
            var tameable = monster.GetComponent<Tameable>();
            var mountable = monster.GetComponent<Mountable>();
            if (mountable != null && tameable != null &&
                tameable.HaveSaddle()) {
              saddled = mountable;
              break;
            }
          }

          if (saddled == null) {
            Logger.LogDebug($"No mount found");
          } else {
            var tameable = saddled.GetComponent<Tameable>();
            var saddle = tameable.m_saddle.gameObject;
            var transform = saddle.transform;
            transform.localPosition += posAdjustment;
            transform.localRotation *= Quaternion.Euler(rotAdjustment);
            Logger.LogDebug(
                $"Mounted: {saddled}" +
                $" new position: {transform.localPosition.ToString("F2")}" +
                $" new rotation: {transform.localRotation.eulerAngles.ToString("F2")}");
          }
        }
      }
    }
#endif
  }
}
