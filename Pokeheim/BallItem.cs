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
using System;
using System.Collections.Generic;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class BallItem {
    private struct BallConfig {
      public string TexturePath;
      public float BallFactor;
      public ItemConfig ItemConfig;
    }

    public static Skills.SkillType Skill = 0;
    public static List<string> RecipeNames = new List<string>();

    private static Mesh BallMesh = null;

    // A map from item ID to CustomItem for inhabited balls.
    private static Dictionary<string, CustomItem> InhabitedBalls =
        new Dictionary<string, CustomItem>();

    private const string InhabitedBallIdPrefix = "pokeheim.inhabited";

    // Generated ball IDs need to be stored in the ball's ZDO, to sync view of
    // these items to other clients.
    private const string GeneratedBallIdKey = "com.pokeheim.GeneratedBallId";

    private static Dictionary<string, BallConfig> BallConfigs = new Dictionary<string, BallConfig> {
      { "Pokeball", new BallConfig {
        TexturePath = "Monster ball texture.png",
        BallFactor = 1.0f,
        ItemConfig = new ItemConfig {
          Name = "$item_pokeball",
          Description = "$item_pokeball_description",
          Icons = new []{ Utils.LoadSprite("Monster ball.png") },
          Amount = 10,
          Requirements = new[] {
            new RequirementConfig {
              Item = "Stone",
              Amount = 1,
            },
            new RequirementConfig {
              Item = "Raspberry",
              Amount = 1,
            },
          },
        },
      } },
      { "Greatball", new BallConfig {
        TexturePath = "Super monster ball texture.png",
        BallFactor = 1.5f,
        ItemConfig = new ItemConfig {
          Name = "$item_greatball",
          Description = "$item_greatball_description",
          Icons = new []{ Utils.LoadSprite("Super monster ball.png") },
          Amount = 10,
          Requirements = new[] {
            new RequirementConfig {
              Item = "Stone",
              Amount = 1,
            },
            new RequirementConfig {
              Item = "Blueberries",
              Amount = 1,
            },
          },
        },
      } },
      { "Ultraball", new BallConfig {
        TexturePath = "Ultra monster ball texture.png",
        BallFactor = 2.0f,
        ItemConfig = new ItemConfig {
          Name = "$item_ultraball",
          Description = "$item_ultraball_description",
          Icons = new []{ Utils.LoadSprite("Ultra monster ball.png") },
          Amount = 10,
          Requirements = new[] {
            new RequirementConfig {
              Item = "Stone",
              Amount = 1,
            },
            new RequirementConfig {
              Item = "MushroomYellow",
              Amount = 1,
            },
          },
        },
      } },
    };

    [PokeheimInit]
    public static void Init() {
      Utils.OnVanillaPrefabsAvailable += delegate {
        // We will steal the mesh from coal to shape the balls.
        BallMesh = Utils.StealFromPrefab<MeshFilter, Mesh>(
            "Coal", filter => filter.mesh);

        AddBalls();
      };
    }

    public static bool IsBall(this ItemDrop.ItemData item) {
      return item.m_shared.m_skillType == Skill;
    }

    public static float BallFactor(this ItemDrop.ItemData item) {
      if (!item.IsBall()) {
        return 0f;
      }

      // Abuse the m_food field (which is ignored for non-food items) to store
      // the ball factor for catching a monster.
      return item.m_shared.m_food;
    }

    public static ItemDrop.ItemData InhabitWith(
        this ItemDrop.ItemData original, Inhabitant inhabitant) {
      var originalItemId = original.m_dropPrefab.name;
      return GetInhabitedBall(originalItemId, inhabitant);
    }

    public static Inhabitant GetInhabitant(this ItemDrop.ItemData item) {
      var itemId = item.m_dropPrefab.name;

      if (ParseInhabitedBallId(
          itemId, out var originalItemId, out var inhabitantString)) {
        return new Inhabitant(inhabitantString);
      }

      return null;
    }

    private static void AddBalls() {
      Skill = SkillManager.Instance.AddSkill(new SkillConfig {
        Identifier = "training",
        Name = "$skill_monster_training",
        Description = "$skill_monster_training_description",
        Icon = Utils.LoadSprite("Skill icon.png"),
        IncreaseStep = 1f,
      });

      foreach (var entry in BallConfigs) {
        var config = entry.Value;
        AddBall(entry.Key, config);
      }
    }

    private static void ReplaceMesh(
        GameObject prefab, Mesh mesh, Vector3 scale, Vector3 rotation,
        Vector3 translation) {
      MeshFilter[] meshes = prefab.GetComponentsInChildren<MeshFilter>(true);
      meshes[0].mesh = mesh;

      var newScale = meshes[0].transform.localScale;
      newScale.x *= scale.x;
      newScale.y *= scale.y;
      newScale.z *= scale.z;

      meshes[0].transform.localScale = newScale;
      meshes[0].transform.Rotate(rotation.x, rotation.y, rotation.z);
      meshes[0].transform.Translate(translation);
    }

    private static void ReplaceTexture(GameObject prefab, Texture2D texture) {
      MeshRenderer[] renderers =
          prefab.GetComponentsInChildren<MeshRenderer>(true);
      Material material = renderers[0].materials[0];
      material.SetTexture("_MainTex", texture);
    }

    private static void ApplyBallStyle(GameObject prefab, string texturePath) {
      // Use these tranforms on the stolen mesh.  The model is apparently
      // gigantic, and has to be scaled to 10%.  We also squish it a bit
      // in what turns out to be the Z axis, to make the balls a little
      // less oblong.  Finally, we move it a little upward in what turns
      // out to be the Y axis so that it looks right in the hand.  Trial
      // and error, baby!
      var scale = new Vector3(0.1f, 0.1f, 0.08f);
      var translation = new Vector3(0f, 0.1f, 0f);
      var rotation = new Vector3(0, 0, 0);
      ReplaceMesh(prefab, BallMesh, scale, rotation, translation);

      // Replace the texture, as well.
      var newTexture = Utils.LoadTexture(texturePath);
      ReplaceTexture(prefab, newTexture);
    }

    private static CustomItem CreateBall(
        string name, string projectileName, BallConfig config) {
      // All ball items are patterned on the Ooze Bomb.
      var customItem = new CustomItem(name, "BombOoze", config.ItemConfig);
      var prefab = customItem.ItemPrefab;

      ApplyBallStyle(prefab, config.TexturePath);

      // Disable the poison gas effect when held, but keep the sparkle when
      // it's an item drop.
      Utils.DisableParticleEffects(prefab, keepFirst: true);

      // Make this item float.  That's really handy.
      customItem.ItemDrop.gameObject.AddComponent<Floating>();

      // Set the associated skill.
      var sharedData = customItem.ItemDrop.m_itemData.m_shared;
      sharedData.m_skillType = Skill;

      // Set the stack size.
      sharedData.m_maxStackSize = 100;

      // It's a painted rock.  As much as I wanted it to have the weight of an
      // in-game stone (2 kg), you really have to be able to carry and throw a
      // stupid number of these to play the game.  So make them light, and easy
      // to throw.
      sharedData.m_weight = 0.1f;
      sharedData.m_attack.m_attackStamina = 1;
      // Lower numbers mean more accurate.
      // The default from the ooze bomb was a range from 20f -> 5f.
      sharedData.m_attack.m_projectileAccuracyMin = 1f;
      sharedData.m_attack.m_projectileAccuracy = 1f;

      // It should put down a deer or boar easily.
      sharedData.m_damages.Modify(0);
      sharedData.m_damageModifiers.Clear();
      sharedData.m_damages.m_blunt = 10;
      sharedData.m_backstabBonus = 1;

      // It's no good for defense, though.
      sharedData.m_blockPower = 0;
      sharedData.m_blockPowerPerLevel = 0;
      sharedData.m_deflectionForce = 0;
      sharedData.m_deflectionForcePerLevel = 0;
      sharedData.m_timedBlockBonus = 0;

      // Abuse the m_food field (which is ignored for non-food items) to store
      // the chance of capture.
      sharedData.m_food = config.BallFactor;

      // Clone and modify the original projectile prefab.  This will serve as
      // the projectile when it is thrown.
      var attack = sharedData.m_attack;
      var projectilePrefab = CreateProjectilePrefab(
          projectileName, attack.m_attackProjectile, config.TexturePath);

      // Now make the attack spawn the projectile type we created, rather than
      // another ooze bomb.
      attack.m_attackProjectile = projectilePrefab;

      // This is the distance at which monsters will hear your attack.
      // This does not actually create any sound effects.
      attack.m_attackHitNoise = 50f;

      return customItem;
    }

    private static GameObject CreateProjectilePrefab(
        string name, GameObject baseProjectilePrefab, string texturePath) {
      var projectilePrefab = PrefabManager.Instance.GetPrefab(name);
      if (projectilePrefab != null) {
        return projectilePrefab;
      }

      projectilePrefab = PrefabManager.Instance.CreateClonedPrefab(
          name, baseProjectilePrefab);
      PrefabManager.Instance.AddPrefab(projectilePrefab);

      // Make it look the same as the custom item.
      ApplyBallStyle(projectilePrefab, texturePath);

      // Disable the poison gas effect.
      Utils.DisableParticleEffects(projectilePrefab);

      var projectile = projectilePrefab.GetComponent<Projectile>();

      // Customize our projectile settings.
      projectile.m_aoe = 0;
      projectile.m_spawnOnHit = null;

      // The visual is what object gets rotated during flight.  This should be
      // the game object of the projectile itself.
      projectile.m_visual = projectile.gameObject;
      projectile.m_rotateVisual = 400;  // degrees per second

      // This will cause the original item to be tracked in
      // projectile.m_spawnItem, but with the chance < 0, it will never be
      // spawned automatically.  When we decide to capture a monster in a ball,
      // we can use the object in m_spawnItem as a template to create an
      // inhabited version containing the monster.
      projectile.m_respawnItemOnHit = true;
      projectile.m_spawnOnHitChance = -1;

      // Disable the ooze bomb's hit effects (bubbling poison sound).
      projectile.m_hitEffects = new EffectList();

      return projectilePrefab;
    }

    private static void AddBall(string name, BallConfig config) {
      var customItem = CreateBall(name, name + "_projectile", config);
      ItemManager.Instance.AddItem(customItem);
      RecipeNames.Add(customItem.Recipe.Recipe.name);
    }

    private static string GetInhabitedBallId(
        string originalItemId, Inhabitant inhabitant) {
      return InhabitedBallIdPrefix + "_" + originalItemId + "_" +
             inhabitant.ToString();
    }

    private static bool ParseInhabitedBallId(
        string ballId, out string originalItemId, out string inhabitantString) {
      if (!ballId.StartsWith(InhabitedBallIdPrefix)) {
        // Not inhabited.
        originalItemId = "";
        inhabitantString = "";
        return false;
      }

      // Inhabited!
      var parts = ballId.Split(new char[]{'_'}, count: 3);
      originalItemId = parts[1];
      inhabitantString = parts[2];
      return true;
    }

    private static ItemDrop.ItemData GetInhabitedBall(
        string originalItemId, Inhabitant inhabitant) {
      var ballId = GetInhabitedBallId(originalItemId, inhabitant);
      CustomItem inhabitedItem = null;
      if (InhabitedBalls.TryGetValue(ballId, out inhabitedItem)) {
        // This ID already existed.
        return inhabitedItem.ItemDrop.m_itemData;
      }

      var config = BallConfigs[originalItemId];
      inhabitedItem = CreateBall(
          ballId, originalItemId + "_projectile", config);

      // This can't be crafted.
      inhabitedItem.Recipe = null;

      var itemData = inhabitedItem.ItemDrop.m_itemData;
      var sharedData = itemData.m_shared;

      // Override the name and description.
      sharedData.m_name = inhabitant.GetFullName();
      sharedData.m_description = inhabitant.GetDescription();
      sharedData.m_icons = inhabitant.GetIcons();

      // Set the prefab so we can instantiate it as a drop right away.
      var prefab = inhabitedItem.ItemPrefab;
      itemData.m_dropPrefab = prefab;

      // AddItem called long after ZNetScene startup doesn't register anything
      // to ObjectDB or ZNetScene.  Calling RegisterItemInObjectDB explicitly
      // will register both to ObjectDB and ZNetScene, even late.
      ItemManager.Instance.AddItem(inhabitedItem);
      ItemManager.Instance.RegisterItemInObjectDB(prefab);
      InhabitedBalls[ballId] = inhabitedItem;

      return itemData;
    }

    private static ItemDrop.ItemData GetInhabitedBall(string ballId) {
      if (ParseInhabitedBallId(
          ballId, out var originalItemId, out var inhabitantString)) {
        try {
          var inhabitant = new Inhabitant(inhabitantString);
          return GetInhabitedBall(originalItemId, inhabitant);
        } catch (Exception ex) {
          Logger.LogError($"Failed to recreate {originalItemId} w/ {inhabitantString}: {ex}");
        }
      } else {
        Logger.LogError($"Failed to parse ball ID \"{ballId}\"");
      }
      return null;
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.GetItemPrefab), new Type[]{ typeof(string) })]
    class GenerateInhabitedBalls_Patch {
      static void Postfix(string name, ObjectDB __instance, ref GameObject __result) {
        // Do nothing during the startup screen.  Only activate this patch
        // during gameplay.  Things go really wonky otherwise, with the debug
        // log being emptied, and with items getting registered wrong and
        // failing to instantiate/register later during gameplay.
        if (Game.instance == null) {
          return;
        }

        if (__result == null && name.StartsWith(InhabitedBallIdPrefix)) {
          // This is an inhabited ball which was not hard-coded and registered
          // to ObjectDB in advance.  Recreate it on-the-fly.  All the data we
          // need is in the name.
          var itemData = GetInhabitedBall(name);
          if (itemData != null) {
            __result = itemData.m_dropPrefab;
          }
        }
      }
    }

    // When transmitting an object over the wire, it turns into a ZDO,
    // which stores a prefab as an integer hash of the prefab name string.
    // But we need strings to recreate these ball items on-the-fly.
    // So we hook into these methods that utilize integer prefab hashes, we
    // extract a string ID from the ZDO, and we use that instead to generate
    // the CustomItem.
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.IsPrefabZDOValid))]
    class GenerateInhabitedBalls_Patch2 {
      static bool Prefix(ref bool __result, ZDO zdo) {
        string ballId = zdo.GetString(GeneratedBallIdKey);
        if (ballId == "") {
          return true;  // Run the original.
        }

        // This is a unique ball item we can regenerate in CreateObject below.
        __result = true;
        return false;  // Suppress the original.
      }
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateObject))]
    class GenerateInhabitedBalls_Patch3 {
      static void Prefix(ZDO zdo) {
        // The original method will eventually call GetPrefab(int), which will
        // look up the hash in m_namedPrefabs.  If we can add things to
        // m_namedPrefabs first, we don't have to mess with the contents of the
        // method.
        string ballId = zdo.GetString(GeneratedBallIdKey);
        if (ballId == "" || !ballId.StartsWith(InhabitedBallIdPrefix)) {
          return;
        }

        // This is a unique ball item.  It may already be registered, but
        // GetInhabitedBall will handle that.
        //Logger.LogDebug($"Loading ZDO ball ID {ballId}");
        GetInhabitedBall(ballId);
      }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
    class GenerateInhabitedBalls_Patch4 {
      static void Postfix(ItemDrop __instance) {
        // Other clients can extract this ID field from a ZDO and generate an
        // equivalent CustomItem.  But we can't set it until the item is awake.
        var name = __instance.m_itemData.m_dropPrefab.name;
        if (name.StartsWith(InhabitedBallIdPrefix)) {
          //Logger.LogDebug($"Storing ZDO ball ID {name}");
          __instance.SetExtraData(GeneratedBallIdKey, name);
        }
      }
    }
  }
}
