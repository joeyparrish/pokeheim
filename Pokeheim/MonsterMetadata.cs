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

using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class MonsterMetadata {
    // NOTE: Adding New Monsters
    //
    // When new content is added to Valheim, it should show up in the Pokedex
    // automatically.  If a trophy object exists and its prefab name follows a
    // common formula, we will find it automatically.  Otherwise, there will be
    // a question mark for the Pokedex icon.
    //
    // To add a trophy or replace a default, use null for the trophy prefab in
    // the Metadata constructor.  Then add a 64x64 PNG to Pokeheim/Assets/
    // named like PremadeIcon-MONSTER_PREFAB_NAME.png.
    //
    // To take a rendering of the monster in-game for use in an icon, use the
    // debug command "renderanddump PREFAB_NAME 1024", then look for a file
    // like ~/.config/unity3d/IronGate/Valheim/renders/PREFAB_NAME.png
    //
    // To find the best place to saddle a new monster, spawn one and catch it
    // (see also the "catchemall" debug command).  Then release it and dump a
    // list of body parts with the debug command "dumpbodyparts PREFAB_NAME".
    // A list of parts will appear in the log.  Choose a likely mount point for
    // the saddle (a spine, a head, a neck, shoulders, whatever).  You may have
    // to change it if the saddle moves in a weird way when attached to that
    // point.
    //
    // Make sure the path you choose from the log starts with "Visual/", but
    // leave that out of the path when you use it.  ("Visual/" is an implied
    // base for saddles.)
    //
    // You can set the mount point in the Metadata constructor, then rebuild
    // and launch the game.  Or you can set it at runtime with the debug
    // command "setmountpoint PREFAB_NAME MOUNT_PATH".
    //
    // With a mount point set, saddle the monster and optionally mount it.  Use
    // the special key combinations Alt+X, Alt+Y, Alt+Z to adjust the saddle
    // position:
    //   X, Y, and Z keys adjust in those axes.
    //   +X is to the monster's right, +Z is the direction it faces, and +Y is
    //   toward the sky.
    //   Left-Alt means a position change of 0.05 m.
    //   Right-Alt means a rotation of 5.00 degrees.
    //   Left-Control means to invert the change.
    //   Left-Command/Left-Windows means to make the change small (20% normal).
    //
    // Test out the position, and if the saddle moves in a weird way when
    // attached to that point of the armature, you can try another body part.
    // When you're happy with the mount point and runtime-adjusted position, be
    // sure to record both in the Metadata constructor.
    private static List<Metadata> AllMonsters = new List<Metadata> {
      new Metadata("Abomination", "TrophyAbomination",
                   "Armature.001/root/hip/spine1/spine1.002/spine1.003",
                   new Vector3(0.00f, 0.00f, 0.00f)),
      new Metadata("Bat", null,
                   "CaveBat/Armature/Root/Hip/Spine/Head",
                   new Vector3(-0.10f, 0.35f, 0.00f)),
      new Metadata("Blob", "TrophyBlob",
                   "blob/Armature/Bone/Bone.002/Bone.002_end",
                   new Vector3(-0.05f, 0.10f, 0.05f)),
      new Metadata("BlobElite", null,
                   "blob/Armature/Bone/Bone.002/Bone.002_end",
                   new Vector3(-0.05f, 0.10f, 0.25f)),
      new Metadata("BlobTar", "TrophyGrowth",
                   "blob/Armature/Bone/Bone.002/Bone.002_end",
                   new Vector3(-0.05f, 0.10f, 0.05f)),
      new Metadata("Boar", "TrophyBoar",
                   "CG/Pelvis/Spine/Spine1",
                   new Vector3(-0.07f, 0.30f, 0.05f)),
      new Metadata("Bonemass", "TrophyBonemass",
                   "model/Armature/root/spine1/spine2/spine3/l_shoulder",
                   new Vector3(-2.15f, 0.80f, -0.15f)),
      new Metadata("Deathsquito", "TrophyDeathsquito",
                   "Armature/Root/Root2/Thorax/Bone.005",
                   new Vector3(-0.07f, 0.15f, -0.25f)),
      new Metadata("Deer", "TrophyDeer",
                   "CG/Pelvis/Spine/Spine1",
                   new Vector3(-0.05f, 0.30f, 0.15f)),
      new Metadata("Dragon", "TrophyDragonQueen",
                   "Armature/Root/Hips/Spine/Spine1/Spine2/Neck",
                   new Vector3(-0.05f, 1.60f, -0.10f),
                   new Vector3(300f, 0f, 0f)),
      new Metadata("Draugr", "TrophyDraugr",
                   "_draugr_base/Armature/Hips/Spine0/Spine1/Spine2/Head/HelmetAttach",
                   new Vector3(0.07f, 0.05f, 0.05f),
                   new Vector3(0, 180, 0)),
      new Metadata("Draugr_Elite", "TrophyDraugrElite",
                   "_draugr_base/Armature/Hips/Spine0/Spine1/Spine2/Head/HelmetAttach",
                   new Vector3(0.07f, 0.05f, 0.05f),
                   new Vector3(0, 180, 0)),
      new Metadata("Eikthyr", "TrophyEikthyr",
                   "Armature/Root2/Pelvis/Spine/Spine1/Spine2/Neck",
                   new Vector3(-0.02f, 0.45f, 0.00f)),
      new Metadata("Fenring", null /* Using custom instead of TrophyFenring */,
                   "Armature/Root/Hips/Spine/Spine1/Spine2/Neck/Head",
                   new Vector3(-0.07f, 0.45f, 0.00f)),
      new Metadata("Fenring_Cultist", "TrophyCultist",
                   "Armature/Root/Hips/Spine/Spine1/Spine2/Neck/Head",
                   new Vector3(-0.06f, 0.29f, -0.08f)),
      new Metadata("Ghost", null,
                   "Point light",
                   new Vector3(-0.08f, 0.70f, 0.00f)),
      new Metadata("Goblin", "TrophyGoblin",
                   "Armature/Root/spine1/spine2/spine3/neck/head",
                   new Vector3(-0.05f, 0.20f, -0.05f)),
      new Metadata("GoblinBrute", "TrophyGoblinBrute",
                   "Armature/Root/Hip2/Hip/Spine1/Spine2/Spine3/Neck/Neck2/Head",
                   new Vector3(-0.05f, 0.30f, 0.00f)),
      new Metadata("GoblinKing", "TrophyGoblinKing",
                   "Armature/Root/Root2/Hip/Spine1/Spine2/Spine3/Neck/Bone.007/Jaw",
                   new Vector3(-0.05f, -0.85f, 1.95f)),
      new Metadata("GoblinShaman", null /* Using custom instead of TrophyGoblinShaman */,
                   "Armature/Root/Root2/Hip/Spine1/Spine2/Spine3/Neck/Head",
                   new Vector3(-0.07f, 0.20f, 0.00f)),
      new Metadata("Greydwarf", "TrophyGreydwarf",
                   "Armature.001/root/spine1/spine2/spine3",
                   new Vector3(-0.10f, 0.35f, 0.00f)),
      new Metadata("Greydwarf_Elite", "TrophyGreydwarfBrute",
                   "Armature.001/root/spine1/spine2/spine3",
                   new Vector3(-0.10f, 0.55f, 0.15f)),
      new Metadata("Greydwarf_Shaman", "TrophyGreydwarfShaman",
                   "Armature.001/root/spine1/spine2/spine3",
                   new Vector3(-0.10f, 0.40f, 0.00f)),
      new Metadata("Greyling", null,
                   "Armature.001/root/spine1/spine2/spine3",
                   new Vector3(-0.10f, 0.29f, 0.00f)),
      new Metadata("Hatchling", "TrophyHatchling",
                   "Hatchling_mountain/Armature/Root/Spine1/Spine2/Neck1/Neck2/Neck3/Head",
                   new Vector3(-0.08f, 0.00f, -0.35f),
                   new Vector3(-55, 0, 0)),
      new Metadata("Leech", "TrophyLeech",
                   "Armature/root/Head",
                   new Vector3(-0.07f, 0.25f, -0.15f)),
      new Metadata("Lox", "TrophyLox",
                   "offset/Armature/Root/Hip/spine1",
                   new Vector3(-0.20f, 1.85f, 1.30f)),
      new Metadata("Neck", "TrophyNeck",
                   "Armature/Hips/Spine/Spine1",
                   new Vector3(-0.07f, 0.35f, -0.10f)),
      new Metadata("Serpent", "TrophySerpent",
                   "Armature/Root/Main/Tail2/Tail1/Head",
                   new Vector3(-0.05f, 1.35f, -0.15f)),
      new Metadata("Skeleton", "TrophySkeleton",
                   "_skeleton_base/Armature/Hips/Spine/Spine1/Spine2/Neck/Head/Helmet_attach",
                   new Vector3(-0.07f, 0.15f, 0.00f)),
      new Metadata("Skeleton_Poison", "TrophySkeletonPoison",
                   "_skeleton_base/Armature/Hips/Spine/Spine1/Spine2/Neck/Head/Helmet_attach",
                   new Vector3(-0.06f, 0.15f, 0.00f)),
      new Metadata("StoneGolem", "TrophySGolem",
                   "Armature/Root/Hip/Spine0/Spine1/Spine2/Head",
                   new Vector3(-0.55f, 0.65f, -0.15f)),
      new Metadata("Surtling", "TrophySurtling",
                   "model/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:Neck/mixamorig:Head",
                   new Vector3(-0.05f, 0.25f, 0.00f)),
      new Metadata("TentaRoot", null,
                   "Tentaroots/Armature/Root/Bone1",
                   new Vector3(-0.05f, 0.45f, 0.55f)).MarkSubordinate(),
      new Metadata("Troll", "TrophyFrostTroll",
                   "Armature/Root/Spine0/Spine1/Spine2/Head",
                   new Vector3(-0.05f, 1.50f, 0.25f)),
      new Metadata("Ulv", null /* Using custom instead of TrophyUlv */,
                   "Ulv/Armature/Root/Hips/Spine/Spine1/Spine2",
                   new Vector3(-0.07f, 0.35f, 0.00f)),
      new Metadata("Wolf", "TrophyWolf",
                   "WolfSmooth/CG/Pelvis/Spine/Spine1",
                   new Vector3(-0.07f, 0.30f, -0.05f)),
      new Metadata("Wraith", "TrophyWraith",
                   "wraith/Armature/root/spine1/spine2/spine3/neck",
                   new Vector3(-0.05f, 0.55f, 0.10f)),
      new Metadata("gd_king", "TrophyTheElder",
                   "Armature/root/spine1/spine2/spine3/r_shoulder",
                   new Vector3(1.05f, 0.35f, 0.00f)),
    };

    private static readonly Vector2 centerPivot = new Vector2(0.5f, 0.5f);

    private static Dictionary<string, Metadata> MonsterMap;

    private static readonly string OverlayPath =
        "Pokeheim/Assets/Inhabited-overlay.png";
    private static readonly string PremadeIconPrefix =
        "Pokeheim/Assets/PremadeIcon-";

    private static Sprite Overlay;
    private const float OverlayAlpha = 0.6f;

    private const int TrophyPanelWidth = 7;

    [PokeheimInit]
    public static void Init() {
      Overlay = AssetUtils.LoadSpriteFromFile(OverlayPath, centerPivot);

#if DEBUG
      CommandManager.Instance.AddConsoleCommand(new SpawnAll());
      CommandManager.Instance.AddConsoleCommand(new SetMountPoint());
#endif

      // If this runs OnVanillaPrefabsAvailable, some (Deer, Neck, Skeleton)
      // will be missing Character components.  Waiting until the scene starts
      // fixes this.
      Utils.OnFirstSceneStart += delegate {
        // Used for captures that aren't in the map, just in case.
        // Will add itself to the map, but we don't put it in the list.
        var fallback = new Metadata("fallback");
        fallback.Load();

        // We have to load everything before we can sort the list.
        foreach (var metadata in AllMonsters) {
          metadata.Load();
        }

        // Add some aliases for species that aren't really independent.
        MonsterMap["Draugr_Ranged"] = MonsterMap["Draugr"];
        MonsterMap["GoblinArcher"] = MonsterMap["Goblin"];
        MonsterMap["Leech_cave"] = MonsterMap["Leech"];
        MonsterMap["Skeleton_NoArcher"] = MonsterMap["Skeleton"];
        MonsterMap["Boar_piggy"] = MonsterMap["Boar"];
        MonsterMap["Lox_Calf"] = MonsterMap["Lox"];
        MonsterMap["Wolf_cub"] = MonsterMap["Wolf"];
        // Created in SerpentMods.cs to make them easier to find/catch:
        MonsterMap["ShorelineSerpent"] = MonsterMap["Serpent"];

        // Seek out any new monsters added to the game since the last update of
        // this mod.  These should show up in the Pokedex and count toward
        // completion.
        foreach (var prefab in ZNetScene.instance.m_prefabs) {
          var character = prefab.GetComponent<Character>();
          if (prefab.GetComponent<Character>() == null ||
              prefab.GetComponent<Player>() != null ||
              prefab.name == "TrainingDummy" ||
              MonsterMap.ContainsKey(prefab.name)) {
            continue;
          }

          Logger.LogWarning($"Adding metadata for new monster {prefab.name}");

          var trophyName = "Trophy" + prefab.name;
          if (ZNetScene.instance.GetPrefab(trophyName) == null) {
            Logger.LogWarning($"Unable to guess trophy name for new monster {prefab.name}");
            trophyName = null;
          }

          var metadata = new Metadata(prefab.name, trophyName);
          metadata.Load();
          AllMonsters.Add(metadata);
        }

        // Now sort them.
        AllMonsters.Sort();

        // Now assign them positions in the Pokedex.
        var index = 0;
        foreach (var metadata in GetAllMonsters()) {
          metadata.SetTrophyPosition(index++);
        }
      };
    }

    public static Metadata Get(string prefabName) {
      Metadata metadata = null;
      if (!MonsterMap.TryGetValue(prefabName, out metadata)) {
        // If we don't have an entry, it could be a new monster added to the
        // game or an old one we overlooked.  To keep the caller running and
        // avoid a failure to catch something just because it's missing from
        // our map, we will synthesize a new entry on-the-fly here.
        metadata = new Metadata(prefabName);
        metadata.Load();
      }
      return metadata;
    }

    public static float PokedexFullness() {
      var player = Player.m_localPlayer;
      int found = 0;
      int total = 0;
      foreach (var metadata in GetAllMonsters()) {
        total++;
        if (player.HasInPokedex(metadata.PrefabName)) {
          found++;
        }
      }
      return (float)found / (float)total;
    }

    public static int NumberOfBosses() {
      int numBosses = 0;

      foreach (var metadata in AllMonsters) {
        if (metadata.IsBoss) {
          numBosses++;
        }
      }

      return numBosses;
    }

    public static bool CaughtAllBosses() {
      var player = Player.m_localPlayer;
      foreach (var metadata in AllMonsters) {
        if (metadata.IsBoss && !player.HasInPokedex(metadata.PrefabName)) {
          return false;
        }
      }
      return true;
    }

    // Only those whose entries are "complete" and can be shown in the Pokedex.
    public static IEnumerable<Metadata> GetAllMonsters() {
      foreach (var metadata in AllMonsters) {
        if (metadata.Incomplete()) {
          Logger.LogDebug($"Skipping incomplete monster {metadata}");
          continue;
        }
        yield return metadata;
      }
    }

    public class Metadata : IComparable<Metadata> {
      private string prefabName;
      private string trophyName;
      internal string mountPointPath;
      private Vector3 saddleOffset;
      private Vector3 saddleRotation;
      private Character prefabCharacter = null;
      private Sprite trophyIcon = null;
      private Sprite trophyShadowIcon = null;
      private Sprite capturedIcon = null;
      private float totalDamage = 0f;
      private double baseCatchRate = 0.0;
      private bool subordinate = false;

      public string PrefabName => prefabName;
      public string TrophyName => trophyName;
      public string MountPointPath => mountPointPath;
      public Vector3 SaddleOffset => saddleOffset;
      public Vector3 SaddleRotation => saddleRotation;
      public Sprite TrophyIcon => trophyIcon;
      public Sprite TrophyShadowIcon => trophyShadowIcon;
      public Sprite CapturedIcon => capturedIcon;
      public float TotalDamage => totalDamage;

      public string GenericName => prefabCharacter.m_name;
      public string LocalizedGenericName => Localization.instance.Localize(GenericName);
      public Character.Faction Faction => prefabCharacter.m_faction;
      public string FactionName => prefabCharacter.m_faction.Name();
      public string LocalizedFactionName => Localization.instance.Localize(FactionName);
      public float BaseHealth => prefabCharacter.m_health;
      public double CatchRate => baseCatchRate;
      public bool IsBoss => Faction == Character.Faction.Boss && !subordinate;

      public Metadata(
          string prefabName,
          string trophyName = null,
          string mountPointPath = null,
          Vector3 saddleOffset = default(Vector3),
          Vector3 saddleRotation = default(Vector3)) {
        this.prefabName = prefabName;
        this.trophyName = trophyName;
        this.mountPointPath = mountPointPath;
        this.saddleOffset = saddleOffset;
        this.saddleRotation = saddleRotation;

        if (MonsterMap == null) {
          MonsterMap = new Dictionary<string, Metadata>();
        }
        MonsterMap.Add(this.prefabName, this);
      }

      override public string ToString() {
        return $"Metadata({prefabName}, {trophyName}, {prefabCharacter}, {trophyIcon}, {capturedIcon})";
      }

      // This will be true for "fallback", but should be false for every other
      // Metadata instance.
      public bool Incomplete() {
        return trophyName == null || prefabCharacter == null ||
               trophyIcon == null || capturedIcon == null;
      }

      // Sort by faction ascending, then by HP ascending, then by name.
      // Load() must be called first on all Metadata instances.
      public int CompareTo(Metadata other) {
        // Push "fallback" to the end.  It has no prefabCharacter.  Don't
        // access any getters that use prefabCharacter for this entry.
        if (prefabCharacter == null) {
          return 1;
        } else if (other.prefabCharacter == null) {
          return -1;
        }

        if (Faction < other.Faction) {
          return -1;
        } else if (Faction > other.Faction) {
          return 1;
        } else if (BaseHealth < other.BaseHealth) {
          return -1;
        } else if (BaseHealth > other.BaseHealth) {
          return 1;
        } else {
          return GenericName.CompareTo(other.GenericName);
        }
      }

      public void Load() {
        if (this.prefabName != "fallback") {
          var prefab = PrefabManager.Instance.GetPrefab(prefabName);
          LoadWeapon(prefab);

          this.prefabCharacter = prefab?.GetComponent<Character>();
          ComputeBaseCatchRate();

          if (this.prefabCharacter == null) {
            Logger.LogError($"Failed to load prefab {prefabName}");
          }
        }

        if (this.trophyName == null) {
          // Load a premade icon from disk.
          var path = $"{PremadeIconPrefix}{this.prefabName}.png";
          this.trophyIcon = AssetUtils.LoadSpriteFromFile(path, centerPivot);

          if (trophyIcon == null) {
            path = $"{PremadeIconPrefix}fallback.png";
            this.trophyIcon = AssetUtils.LoadSpriteFromFile(path, centerPivot);
          }

          if (this.prefabCharacter != null && this.trophyIcon != null) {
            CreateCustomTrophy();
          }
        } else {
          var trophyPrefab = PrefabManager.Instance.GetPrefab(this.trophyName);
          var trophyItem = trophyPrefab?.GetComponent<ItemDrop>();
          this.trophyIcon = trophyItem?.m_itemData?.GetIcon();
        }

        if (this.trophyIcon == null) {
          Logger.LogError($"Unable to load icon for {this.prefabName}");
          this.trophyIcon = MonsterMap["fallback"].trophyIcon;
          this.trophyShadowIcon = MonsterMap["fallback"].trophyShadowIcon;
          this.capturedIcon = MonsterMap["fallback"].capturedIcon;
        } else {
          CreateCapturedIcon();
          CreateShadowIcon();
        }
      }

      private void LoadWeapon(GameObject prefab) {
        if (prefab == null) {
          return;
        }

        // NOTE: There is an exception here in the log, but it gets swallowed
        // somewhere, somehow.  The Instantiate call completes successfully.
        var clone = UnityEngine.Object.Instantiate(prefab);
        var humanoid = clone.GetComponent<Humanoid>();
        var enemy = ItemDrop.ItemData.AiTarget.Enemy;

        if (humanoid != null) {
          try {
            humanoid.GiveDefaultItems();
          } catch (Exception ex) {
            Logger.LogError($"Exception giving out default items to {prefab}: {ex}");
          }
          foreach (var item in humanoid.m_inventory.GetAllItems()) {
            var targetType = item.m_shared.m_aiTargetType;
            if (item.IsWeapon() && targetType == enemy) {
              // What we put into the Pokedex is the most damaging attack.
              var itemDamage = item.m_shared.m_damages.GetTotalDamage();
              this.totalDamage = Mathf.Max(this.totalDamage, itemDamage);
              Logger.LogDebug($"Monster: {humanoid}, damage: {itemDamage}, prio: {item.m_shared.m_aiPrioritized}, weapon: {item.m_shared.m_name}");
            }
          }
        }

        ZNetScene.instance.Destroy(clone);
      }

      public void SetTrophyPosition(int value) {
        var x = value % TrophyPanelWidth;
        var y = value / TrophyPanelWidth;

        var trophyPrefab = PrefabManager.Instance.GetPrefab(this.trophyName);
        var trophyItem = trophyPrefab?.GetComponent<ItemDrop>();
        if (trophyItem == null) {
          Logger.LogError($"No trophy for {this.trophyName}?!");
          return;
        }

        trophyItem.m_itemData.m_shared.m_trophyPos = new Vector2Int(x, y);
      }

      internal Metadata MarkSubordinate() {
        this.subordinate = true;
        return this;
      }

      private void ComputeBaseCatchRate() {
        if (this.prefabCharacter == null) {
          return;
        }

        // Using an older .NET SDK which doesn't have MathF for floats.  So
        // this calculation is done in doubles.
        double catchRate = 1.0;

        // Apply a starting catch rate based on the faction.
        catchRate *= prefabCharacter.m_faction.CatchRate();

        // Cut the rate by 1.25 for every 100 HP (when full, for a level 1
        // monster).
        catchRate /= Math.Pow(1.25, Math.Floor((double)BaseHealth / 100.0));

        // Cut the rate by 1.5 for every 50 damage (assuming standard weapon).
        catchRate /= Math.Pow(1.5, Math.Floor((double)TotalDamage / 50.0));

        this.baseCatchRate = catchRate;
      }

      private void CreateCustomTrophy() {
        this.trophyName = $"TrophyCustom_{this.prefabName}";
        Logger.LogDebug($"Creating custom trophy {trophyName}");

        // The base trophy type doesn't matter.  We will never see this in-game.
        // Only the icon and name matter, since they are used for the Pokedex
        // (repurposed Trophies page).
        var baseTrophy = "TrophyBoar";

        var customItem = new CustomItem(
            this.trophyName, baseTrophy, new ItemConfig {
          Name = this.GenericName,
          Description = "Does not appear in-game.",
          Icons = new []{ this.trophyIcon },
        });
        ItemManager.Instance.AddItem(customItem);
      }

      private void CreateCapturedIcon() {
        var parentObject = new GameObject("Icon parent");

        var bgObject = new GameObject("Icon bg");
        bgObject.transform.SetParent(parentObject.transform);
        var bgRenderer = bgObject.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = this.trophyIcon;

        // Scale the icon.  Most icons are the same size, but some (Eikthyr,
        // Bonemass) are much larger.  I don't understand the exact value here,
        // but it's what the majority of the icons use.  Making them all
        // consistent in size fixes the size and placement of the overlay.
        if (bgRenderer.bounds.size.x != 0.6f) {
          bgRenderer.transform.localScale *= 0.6f / bgRenderer.bounds.size.x;
        }

        // I wouldn't have noticed that this needed flipping were it not for
        // the fallback icon, which is a question mark.
        bgRenderer.flipX = true;

        var fgObject = new GameObject("Icon fg");
        fgObject.transform.SetParent(parentObject.transform);
        var fgRenderer = fgObject.AddComponent<SpriteRenderer>();
        fgRenderer.sprite = Overlay;
        fgRenderer.flipX = true;

        // Set the alpha of the foreground overlay.
        fgRenderer.color = new Color(1f, 1f, 1f, OverlayAlpha);
        // Make sure it appears on top of the background.
        fgRenderer.sortingOrder = bgRenderer.sortingOrder + 1;

        this.capturedIcon = RenderManager.Instance.Render(parentObject);
      }

      private void CreateShadowIcon() {
        var parentObject = new GameObject("Icon parent");

        var bgObject = new GameObject("Icon bg");
        bgObject.transform.SetParent(parentObject.transform);
        var bgRenderer = bgObject.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = this.trophyIcon;

        // I wouldn't have noticed that this needed flipping were it not for
        // the fallback icon, which is a question mark.
        bgRenderer.flipX = true;

        // Turn the icon into a silhouette by setting the color to black.
        bgRenderer.color = Color.black;

        this.trophyShadowIcon = RenderManager.Instance.Render(parentObject);
      }

      internal void Spawn(Vector3 position) {
        Quaternion rotation = Quaternion.identity;
        UnityEngine.Object.Instantiate(
            prefabCharacter.gameObject, position, rotation);
      }
    }

    public static string Name(this Character.Faction faction) {
      switch (faction) {
        case Character.Faction.ForestMonsters:
          return "$faction_forest";
        case Character.Faction.Undead:
          return "$faction_undead";
        case Character.Faction.Demon:
          return "$faction_demon";
        case Character.Faction.MountainMonsters:
          return "$faction_mountain";
        case Character.Faction.SeaMonsters:
          return "$faction_ocean";
        case Character.Faction.PlainsMonsters:
          return "$faction_plains";
        case Character.Faction.Boss:
          return "$faction_boss";
        default:
          return "$faction_unknown";
      }
    }

    public static double CatchRate(this Character.Faction faction) {
      switch (faction) {
        case Character.Faction.ForestMonsters:
          return 0.1;
        case Character.Faction.Undead:
        case Character.Faction.Demon:
          return 0.05;
        case Character.Faction.MountainMonsters:
          return 0.02;
        case Character.Faction.SeaMonsters:
        case Character.Faction.PlainsMonsters:
          return 0.05;
        case Character.Faction.Boss:
        default:
          return 0;
      }
    }

    public static double GetCatchRate(
        this Character monster, double ballFactor = 1.0) {
      // Using an older .NET SDK which doesn't have MathF for floats.  So
      // this calculation is done in doubles.

      // Start with the base catch rate from the metadata object.  This is not
      // specific to an instance.
      var metadata = Get(monster.GetPrefabName());
      double catchRate = metadata.CatchRate;

      // Cut the catch rate in half again for each level of this monster.
      // Levels are 1-based, so a level 1 monster (no stars) will keep the
      // base catch rate based on faction.  A level 2 monster (1 star) will
      // be twice as hard to catch.
      catchRate /= Math.Pow(2.0, (double)(monster.m_level - 1));

      // These steps require a real-life monster, but monster could be a
      // component of a prefab that doesn't exist in the world yet.
      if (monster.m_nview != null) {
        // Divide by the health ratio.  A monster at 1% is 100x more likely to
        // be caught.
        var healthRatio = monster.GetHealth() / monster.GetMaxHealth();
        catchRate /= (double)healthRatio;

        // Apply a bonus when the monster eats a berry.
        var berryEater = monster.GetComponent<Berries.BerryEater>();
        catchRate *= berryEater?.GetBerryCatchRate() ?? 1.0;

        // These steps can overflow 1.0, so cap it.
        catchRate = Math.Min(catchRate, 1.0);
      }

      // Finally, the ball provides an exponent on the failure rate.  Since
      // the failure rate is a fraction of 1.0, raising it to a power lowers
      // the failure rate.
      catchRate = 1.0 - Math.Pow(1.0 - catchRate, ballFactor);

      return catchRate;
    }

#if DEBUG
    class SpawnAll : ConsoleCommand {
      public override string Name => "spawnall";
      public override string Help => "[opt_faction] - Spawn all of a certain faction, or one of everything in the game.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var player = Player.m_localPlayer;
        var faction = args.Length != 0 ? args[0] : null;
        if (faction == null) {
          Logger.LogInfo($"Spawning one of everything... EVERYTHING!");
        } else {
          Logger.LogInfo($"Spawning one of everything from the \"{faction}\" faction!");
        }

        foreach (var metadata in GetAllMonsters()) {
          if (faction == null || faction.ToLower() == metadata.LocalizedFactionName.ToLower()) {
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 5f;
            Vector3 position = player.transform.position + randomOffset;

            position.MoveToFloor(offset: 1f);

            Logger.LogInfo($"Spawning {metadata.LocalizedGenericName} at {position}");
            metadata.Spawn(position);
          }
        }
      }
    }

    class SetMountPoint : ConsoleCommand {
      public override string Name => "setmountpoint";
      public override string Help => "[prefabname] [path] - Override the mount point of a monster.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        var player = Player.m_localPlayer;
        var prefabName = args[0];
        var mountPointPath = String.Join(" ", args, 1, args.Length - 1);

        // Get() will create one on-the-fly to avoid an exception.  Here, we
        // don't want that.  A typo should generate an exception.
        Metadata metadata = null;
        try {
          metadata = MonsterMap[prefabName];
        } catch (Exception) {
          Debug.Log($"No such monster prefab registered: {prefabName}");
          return;
        }

        Logger.LogInfo($"Setting {prefabName} mount point to \"{mountPointPath}\"");
        metadata.mountPointPath = mountPointPath;
      }
    }
#endif
  }
}
