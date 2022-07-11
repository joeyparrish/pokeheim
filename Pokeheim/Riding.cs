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

namespace Pokeheim {
  public static class Riding {
    public static ItemDrop UniversalSaddleItem = null;
    private static GameObject SaddlePrefab = null;
    public const string SaddleName = "$item_saddle_pokeheim";

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
              Amount = 5,
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

        // Monsters always drop their saddle on death/capture, but we implement
        // it ourselves.  See onMountDeathOrCapture patch below.
        tameable.m_dropSaddleOnDeath = false;

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

    // This is the same logic FindHoverObject uses to decide what object to
    // return for each hit.
    public static GameObject GetGameObject(this RaycastHit hit) {
      var hoverable = hit.collider.GetComponent<Hoverable>();
      if (hoverable != null) {
        return hit.collider.gameObject;
      }
      if (hit.collider.attachedRigidbody != null) {
        return hit.collider.attachedRigidbody.gameObject;
      }
      return hit.collider.gameObject;
    }

    public static T HitComponent<T>(this RaycastHit hit) {
      return hit.GetGameObject().GetComponent<T>();
    }

    // There are a few issues with saddles that we need to patch:
    //   1. Some monsters "extend" beyond the saddle, making it difficult to
    //      use or remove.  We need to prefer saddles when we hover the cursor
    //      over a saddled monster, so that the saddle is easier to interact
    //      with.
    //   2. Some monsters (like gd_king) are very tall.  We need to be able to
    //      interact with a saddle mounted on the boss's shoulder, so we should
    //      only consider the X-Z distance (parallel to the ground), and not
    //      the complete 3D distance (including the up-down Y axis).
    [HarmonyPatch]
    class SaddleDistance_Patch {
      // Sadle [sic] has its own filter for when a Player is in range to
      // interact with it.  If the Player is not in range, the hover text is
      // "too far", and it refuses interactions.  Only consider the X-Z
      // distance for this, to make it possible to use saddles on very tall
      // monsters.
      [HarmonyPatch(typeof(Sadle), nameof(Sadle.InUseDistance))]
      [HarmonyPrefix]
      static bool SaddleUseDistanceOnlyInXZ(Sadle __instance, ref bool __result, Humanoid human) {
        var saddle = __instance;
        var flatDistance = Utils.DistanceXZ(
		        human.transform.position, saddle.m_attachPoint.position);
        __result = flatDistance < saddle.m_maxUseRange;
        // Suppress the original.
        return false;
      }

      // The "advantage" we give to saddles in sorting objects by distance.
      // Effectively, for the purposes of hovering, a saddle is this much
      // closer than it really is (in meters).
      private const float SaddleAdvantage = 5f;

      // Sort by distance, with an advantage given to saddles.
      public static void CustomSort(RaycastHit[] array) {
        // Sort the hits as FindHoverObject would, but with an advantage given
        // to saddles.
        Array.Sort(array, (RaycastHit x, RaycastHit y) => {
          float dx = x.distance;
          float dy = y.distance;

          // Give saddles an advantage in sorting by distance.
          if (x.HitComponent<Sadle>() != null) {
            dx -= SaddleAdvantage;
          }
          if (y.HitComponent<Sadle>() != null) {
            dy -= SaddleAdvantage;
          }
          return dx.CompareTo(dy);
        });
      }

      // Only consider XZ distance for saddles, to make it easier to reach
      // saddles on tall monsters.
      public static float HitDistance(Vector3 a, Vector3 b, RaycastHit hit) {
        if (hit.HitComponent<Sadle>() != null) {
          return Utils.DistanceXZ(a, b);
        } else {
          return Vector3.Distance(a, b);
        }
      }

      // Patch in our custom sort and distance methods so we prefer to hover on
      // saddles and can interact with them on tall monsters.
      [HarmonyPatch(typeof(Player), nameof(Player.FindHoverObject))]
      [HarmonyTranspiler]
      static IEnumerable<CodeInstruction> HoverObjectTranspiler(
          IEnumerable<CodeInstruction> instructions,
          ILGenerator generator) {
        var hitBackupVar = generator.DeclareLocal(typeof(RaycastHit));

        var vector3DistanceMethod = typeof(Vector3).GetMethod(
            "Distance",
            BindingFlags.Static | BindingFlags.Public);

        var sortMethod = typeof(SaddleDistance_Patch).GetMethod(
            nameof(SaddleDistance_Patch.CustomSort));
        var distanceMethod = typeof(SaddleDistance_Patch).GetMethod(
            nameof(SaddleDistance_Patch.HitDistance));

        var phases = new TranspilerSequence.Phase[] {
          new TranspilerSequence.Phase {
            matcher = code => (code.opcode == OpCodes.Call &&
                               (code.operand as MethodInfo).Name == "Sort"),
            replacer = code => new CodeInstruction[] {
              // This is Array::Sort(
              //   UnityEngine.RaycastHit[] array,
              //   System.Comparison<UnityEngine.RaycastHit> comparison)
              code,
              // The top of the stack is the sorted array.

              // Duplicate the array ref on the stack.
              new CodeInstruction(OpCodes.Dup),
              // Call our custom sorting method, which consumes one stack
              // element.
              new CodeInstruction(OpCodes.Call, sortMethod),
              // Now the array has been sorted again using our custom
              // method.

              // We leave one array ref on the stack, which is where we
              // started.
            },
          },
          new TranspilerSequence.Phase {
            matcher = code => (code.opcode == OpCodes.Ldelem),
            replacer = code => new CodeInstruction[] {
              // This loads an element (RaycastHit) from an array.
              code,

              // After the original instruction, inject instructions to
              // back up the hit in our own local var.
              new CodeInstruction(OpCodes.Dup),
              new CodeInstruction(OpCodes.Stloc_S, hitBackupVar),
            },
          },
          new TranspilerSequence.Phase {
            matcher = code => (code.opcode == OpCodes.Call &&
                               (code.operand as MethodInfo) == vector3DistanceMethod),
            replacer = code => new CodeInstruction[] {
              // This is Vector3::Distance(Vector3, Vector3). We replace it.
              // Put one more argument on the stack: the hit itself.
              new CodeInstruction(OpCodes.Ldloc_S, hitBackupVar),

              // Now call _our_ distance method, which uses the hit context
              // to change the distance used for saddles.
              new CodeInstruction(OpCodes.Call, distanceMethod),
            },
          },
        };

        return TranspilerSequence.Execute(
            "FindHoverObject", phases, instructions);
      }  // static IEnumerable<CodeInstruction> HoverObjectTranspiler
    }  // class SaddleDistance_Patch

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
      static Rigidbody lastContact = null;

      [HarmonyPrefix]
      [HarmonyPatch(typeof(Sadle), nameof(Sadle.OnUseStop))]
      static void onDismount(Sadle __instance, Player player) {
        if (player == Player.m_localPlayer && !dismounting) {
          Logger.LogDebug($"Dismounting: {__instance.m_character}");
          dismounting = true;
        }
      }

      [HarmonyPrefix]
      [HarmonyPatch(typeof(Tameable), nameof(Tameable.OnDeath))]
      static void onMountDeathOrCapture(Tameable __instance) {
        var tameable = __instance;
        if (tameable.HaveSaddle() && tameable.m_saddle.IsLocalUser()) {
          Logger.LogDebug($"Dismounting on death: {__instance.m_character}");
          dismounting = true;

          // Force the player off.
          tameable.m_saddle.OnUseStop(Player.m_localPlayer);
          // Force the saddle off.  Note that since some monster Characters
          // don't stop existing when they faint (those without Ragdolls), we
          // have to implement this ourselves instead of using
          // m_dropSaddleOnDeath.  The built-in version assumes that the
          // Character actually dies, which leads to weird, broken behavior.
          // https://github.com/joeyparrish/pokeheim/issues/7
          tameable.DropSaddle(Vector3.zero);
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

          var contactObj = character.m_lowestContactCollider?.attachedRigidbody;
          if (contactObj &&
              (contactObj.GetComponent<Mountable>() != null ||
               contactObj.GetComponent<Sadle>() != null)) {
            // If we're touching the saddle or Mountable monster, don't count
            // this fall as complete.  We haven't hit actual ground yet.

            if (contactObj != lastContact) {
              lastContact = contactObj;
              Logger.LogDebug($"Fall incomplete: {contactObj}");
            }
          } else {
            // We're on the actual ground (or something else that isn't the
            // Mountable monster or the saddle), so count this fall as
            // "complete" and disable the safeguards.
            complete = true;
            Logger.LogDebug($"Fall complete: {contactObj}");
          }
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
      static bool Prefix(
          Player __instance,
          Vector3 movedir,
          ref bool attack, ref bool secondaryAttack, bool block, bool jump,
          bool crouch, bool run) {
        var player = __instance;
        var saddle = player.m_doodadController as Sadle;
        var steed = saddle?.m_character;
        var canFly = (steed?.m_baseAI.m_randomFly ?? false) ||
                     (steed?.IsFlying() ?? false);
        var monsterWithWeapons =
            steed?.GetComponent<MonsterWithWeapons>() ?? null;

        if (steed != null && (attack || secondaryAttack)) {
          // Make the steed attack for us.

          // Note that no monster "weapon" has a "secondary attack" that I've
          // seen.  Instead, we map the secondaryAttack flag to a separate
          // "weapon" (which for monsters, is just a form of attack).
          bool equipped =
              monsterWithWeapons?.EquipWeapon(secondaryAttack) ?? false;

          // If we asked for a secondary attack, but there is no second weapon,
          // it will fail to equip and we will not carry out an attack.
          if (equipped) {
            steed.StartAttack(null, false);
          }

          // Turn this off so that the player isn't forced to dismount.
          attack = false;
          secondaryAttack = false;
        }

        if (canFly) {
          // If the player is riding a flying monster with a saddle, use jump
          // and crouch to direct that monster up or down.  Incorporate that
          // into movedir.
          Vector3 augmentedMoveDir = movedir;
          if (jump) {
            augmentedMoveDir.y = Vector3.up.y;
          } else if (crouch) {
            augmentedMoveDir.y = Vector3.down.y;
          }

          FlySteed(
              saddle,
              player.GetLookDir(),
              augmentedMoveDir,
              run,
              block);

          // Suppress the original.  Our flying controls take over.
          return false;
        }

        // Run the original.
        return true;
      }

      // This replaces the game logic in Sadle.ApplyControlls() that would
      // remove the "y" component from the direction.
      static void FlySteed(
          Sadle saddle, Vector3 lookDir, Vector3 controlDir,
          bool run, bool block) {
        /* NOTE: Original code for reference.
        Speed speed = Speed.NoChange;
        Vector3 vector = Vector3.zero;
        if (block || controlDir.z > 0.5f || run) {
          Vector3 vector2 = lookDir;
          vector2.y = 0f;
          vector2.Normalize();
          vector = vector2;
        }

        if (run) {
          speed = Sadle.Speed.Run;
        } else if (controlDir.z > 0.5f) {
          speed = Sadle.Speed.Walk;
        } else if (controlDir.z < -0.5f) {
          speed = Sadle.Speed.Stop;
        } else if (block) {
          speed = Sadle.Speed.Turn;
        }
        */

        Sadle.Speed speed = Sadle.Speed.NoChange;
        Vector3 goThisWay = Vector3.zero;
        if (block || controlDir.z > 0.5f) {
          // If the mount is being told to go forward at all, move in the XZ
          // direction of the camera, but use the Y component (up/down) of the
          // controls.  Forward+Up is CameraForward + WorldUp.
          goThisWay = lookDir;
          goThisWay.y = controlDir.y;
          goThisWay.Normalize();
        } else if (controlDir.z == 0f && Math.Abs(controlDir.y) > 0.5f) {
          // If we're only directing the steed up/down, first see if we're
          // already moving in an XZ direction.
          var steed = saddle.m_character;

          goThisWay = steed.m_moveDir;
          goThisWay.y = 0f;

          if (goThisWay.magnitude < 0.5f) {
            // In the XZ plane, we're not moving toward anything.
            // Head up/down while keeping the steed's facing direction.
            // For this, we set the XZ components very small, then set Y.
            goThisWay = steed.m_lookDir;
            goThisWay *= 0.001f;
            goThisWay.y = controlDir.y;
          } else {
            // In the XZ plane, we're already moving toward something.
            // Keep that, and add the Y component.
            goThisWay.y = controlDir.y;
            goThisWay.Normalize();
          }
        }

        if (run) {
          speed = Sadle.Speed.Run;
        } else if (controlDir.z > 0.5f || Math.Abs(controlDir.y) > 0.5) {
          speed = Sadle.Speed.Walk;
        } else if (controlDir.z < -0.5f) {
          speed = Sadle.Speed.Stop;
        } else if (block) {
          speed = Sadle.Speed.Turn;
        }

        if (speed != Sadle.Speed.NoChange) {
          Logger.LogDebug("FlySteed: " +
              $"controlDir: {controlDir}, " +
              $"output: {goThisWay} @ speed {speed}");

          saddle.m_nview.InvokeRPC(
              "Controls", goThisWay, (int)speed, saddle.m_rideSkill);
        }
      }
    }

    // The HUD for riding has a hard-coded icon for Lox.  We should fix that,
    // and make some other tweaks for Pokeheim.
    [HarmonyPatch]
    class TweakMountHud_Patch {
      [HarmonyPostfix]
      [HarmonyPatch(typeof(Sadle), nameof(Sadle.RPC_RequestRespons))]
      static void tweakMountUI(Sadle __instance, bool granted) {
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

    // Not all captured monsters can be saddled by just anyone.
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.UseItem))]
    class OnlyOwnerCanSaddle_Patch {
      static bool Prefix(Tameable __instance, ref bool __result, Humanoid user) {
        var tameable = __instance;
        var monster = tameable.m_character;

        if (monster.AlliedWith(user as Player) == false) {
          __result = false;
          return false;
        }

        return true;
      }
    }

    // You can't get into or remove a saddle that's on someone else's monster.
    [HarmonyPatch(typeof(Sadle), nameof(Sadle.Interact))]
    class OnlyOwnerCanRide_Patch {
      static bool Prefix(Sadle __instance, ref bool __result, Humanoid character, bool repeat, bool alt) {
        var saddle = __instance;
        var monster = saddle.m_character;

        if (monster.AlliedWith(character as Player) == false) {
          __result = false;
          return false;
        }

        return true;
      }
    }

    // Drakes are _really_ wiggly in flight.  The camera whips around and makes
    // the user feel sick to watch it.  It turns out that this is due to a
    // default setting for "immersive camera".  Rather than ask people to
    // disable that, disable it in the mod in context of riding, specifically.
    // The internal is called "m_shipCameraTilt", so we can presume it is a
    // setting meant for the motion of waves while sailing.  It should be fine
    // to disable while riding in a saddle, even if the user wants it on for
    // sailing.
    [HarmonyPatch]
    class DontGiveUsMotionSickness_Patch {
      const float increasedMaxDistance = 12f;
      // NOTE: Valheim+ will change the camera's m_maxDistance on every update.
      // So there's way our patch will win if both are installed.  This also
      // means there's no point in reading the value that V+ writes to try to
      // avoid messing up their override.  We can't mess it up, and our tweak
      // to camera distance will be useless with V+.
      const float defaultMaxDistance = 6f;

      static void OnMount() {
        // Disable the camera's "ship" setting so we don't get sick.
        GameCamera.instance.m_shipCameraTilt = false;
        // Increase maximum camera distance, which is also helpful.
        GameCamera.instance.m_maxDistance =
            Mathf.Max(GameCamera.instance.m_maxDistance, increasedMaxDistance);
      }

      static void OnDismount() {
        // Recompute the camera's "ship" setting from user prefs.
        var setting = PlayerPrefs.GetInt("ShipCameraTilt", 1);
        GameCamera.instance.m_shipCameraTilt = (setting == 1);
        // Set the maximum camera distance back to normal.
        GameCamera.instance.m_maxDistance = defaultMaxDistance;
      }

      [HarmonyPatch(typeof(Sadle), nameof(Sadle.RPC_RequestRespons))]
      [HarmonyPostfix]
      static void disableSettingOnMount(Sadle __instance, bool granted) {
        var saddle = __instance;
        if (granted && saddle.IsLocalUser()) {
          // We just mounted up, so apply riding settings.
          OnMount();
        }
      }

      [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.ApplySettings))]
      [HarmonyPostfix]
      static void disableSettingAfterSettingsChanged() {
        var player = Player.m_localPlayer;
        var saddle = player?.GetDoodadController() as Sadle ?? null;
        if (saddle != null) {
          // We are still mounted, so reapply riding settings no matter what
          // the new settings say.
          OnMount();
        }
      }

      [HarmonyPatch(typeof(Sadle), nameof(Sadle.OnUseStop))]
      [HarmonyPostfix]
      static void recomputeSettingOnDismount(Sadle __instance, Player player) {
        if (player == Player.m_localPlayer) {
          // We just dismounted, so apply normal settings again.
          OnDismount();
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
