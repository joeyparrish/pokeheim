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
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class DressUp {
    private const string WardrobeName = "Wardrobe";

    public class DressUpPanel {
      private static GameObject Panel = null;
      private static Dictionary<string, Dropdown> Choosers =
          new Dictionary<string, Dropdown>();

      public class ClothingData {
        public class Option {
          public Option(string name, string prefabName) {
            this.Name = name;
            this.PrefabName = prefabName;
          }

          public string Name;
          public string PrefabName;
          public string LocalizedName = "";
        }

        public ItemDrop.ItemData.ItemType Type;
        public List<Option> Options;
      }

      private static readonly Dictionary<string, ClothingData> Clothing =
          new Dictionary<string, ClothingData> {
        {"$wardrobe_category_helmet", new ClothingData {
          Type = ItemDrop.ItemData.ItemType.Helmet,
          Options = new List<ClothingData.Option> {
            new ClothingData.Option("$wardrobe_helmet_none", null),
          },
        }},
        {"$wardrobe_category_cape", new ClothingData {
          Type = ItemDrop.ItemData.ItemType.Shoulder,
          Options = new List<ClothingData.Option> {
            new ClothingData.Option("$wardrobe_cape_none", null),
          },
        }},
        {"$wardrobe_category_shirt", new ClothingData {
          Type = ItemDrop.ItemData.ItemType.Chest,
          Options = new List<ClothingData.Option> {
            new ClothingData.Option("$wardrobe_shirt_none", null),
          },
        }},
        {"$wardrobe_category_pants", new ClothingData {
          Type = ItemDrop.ItemData.ItemType.Legs,
          Options = new List<ClothingData.Option> {
            new ClothingData.Option("$wardrobe_pants_none", null),
          },
        }},
        {"$wardrobe_category_belt", new ClothingData {
          Type = ItemDrop.ItemData.ItemType.Utility,
          Options = new List<ClothingData.Option> {
            new ClothingData.Option("$wardrobe_belt_none", null),
          },
        }},
      };

      private static readonly List<string> MenuOrder = new List<string> {
        "$wardrobe_category_helmet",
        "$wardrobe_category_cape",
        "$wardrobe_category_shirt",
        "$wardrobe_category_pants",
        "$wardrobe_category_belt",
      };

      public static void RegisterAllClothing() {
        // Seek out all clothing and build the menus dynamically.
        foreach (var prefab in ZNetScene.instance.m_prefabs) {
          RegisterIfClothing(prefab);
        }
      }

      private static void RegisterIfClothing(GameObject prefab) {
        var itemDrop = prefab.GetComponent<ItemDrop>();
        if (itemDrop == null) {
          // Not an item.
          return;
        }
			  if (itemDrop.m_itemData.m_shared.m_icons.Length == 0) {
          // Not a thing we can hold in inventory.
          // These can be "items" that are only held by monsters.  For example,
          // the prefabs "GoblinArmband" and "GoblinBrute_ExecutionerCap".
          return;
        }
        if (prefab.name == "CapeTest") {
          // This meets all our criteria, but it's garbage.  It's a buggy
          // version of the CapeLinen model AFAICT.
          return;
        }

        var itemName = itemDrop.m_itemData.m_shared.m_name;
        var itemType = itemDrop.m_itemData.m_shared.m_itemType;
        string menuName = "";

        switch (itemType) {
          case ItemDrop.ItemData.ItemType.Helmet:
            menuName = "$wardrobe_category_helmet";
            break;
          case ItemDrop.ItemData.ItemType.Shoulder:
            menuName = "$wardrobe_category_cape";
            break;
          case ItemDrop.ItemData.ItemType.Chest:
            menuName = "$wardrobe_category_shirt";
            break;
          case ItemDrop.ItemData.ItemType.Legs:
            menuName = "$wardrobe_category_pants";
            break;
          case ItemDrop.ItemData.ItemType.Utility:
            menuName = "$wardrobe_category_belt";
            break;
          default:
            // Not clothing.
            return;
        }

        var localizedName = Localization.instance.Localize(itemName);
        var localizedMenuName = Localization.instance.Localize(menuName);
        Logger.LogDebug($"Found {prefab.name} ({localizedName}), belongs to {localizedMenuName}");

        var option = new ClothingData.Option(itemName, prefab.name);
        Clothing[menuName].Options.Add(option);
      }

      public static void Init() {
        Panel = GUIManager.Instance.CreateWoodpanel(
            parent: GUIManager.CustomGUIFront.transform,
            anchorMin: new Vector2(0.2f, 0.5f),
            anchorMax: new Vector2(0.2f, 0.5f),
            position: new Vector2(0f, 0f),
            width: 350f,
            height: 500f,
            draggable: false);
        Panel.SetActive(false);

        var title = GUIManager.Instance.CreateText(
            text: Localization.instance.Localize("$wardrobe_dialog_title"),
            parent: Panel.transform,
            anchorMin: new Vector2(0.5f, 1f),
            anchorMax: new Vector2(0.5f, 1f),
            position: new Vector2(0f, -60f),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 35,
            color: GUIManager.Instance.ValheimOrange,
            outline: true,
            outlineColor: Color.black,
            width: 285f,
            height: 90f,
            addContentSizeFitter: false);

        var close = GUIManager.Instance.CreateButton(
            text: "x",
            parent: Panel.transform,
            anchorMin: new Vector2(1f, 1f),
            anchorMax: new Vector2(1f, 1f),
            position: new Vector2(-30f, -30f),
            width: 30f,
            height: 30f);
        close.SetActive(true);
        close.GetComponent<Button>().onClick.AddListener(Hide);

        var position = new Vector2(0f, -135f);
        foreach (var name in MenuOrder) {
          CreateChooser(name, position);
          position += new Vector2(0f, -70f);
        }
      }

      static void CreateChooser(string name, Vector2 position) {
        var anchor = new Vector2(0.5f, 1f);  // Top center.

        var label = GUIManager.Instance.CreateText(
            text: Localization.instance.Localize(name),
            parent: Panel.transform,
            anchorMin: anchor,
            anchorMax: anchor,
            position: position,
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 20,
            color: GUIManager.Instance.ValheimOrange,
            outline: true,
            outlineColor: Color.black,
            width: 280f,
            height: 30f,
            addContentSizeFitter: false);

        var chooser = GUIManager.Instance.CreateDropDown(
            parent: Panel.transform,
            anchorMin: anchor,
            anchorMax: anchor,
            position: position + new Vector2(0f, -30f),
            fontSize: 16,
            width: 280f,
            height: 30f);

        var options = new List<string>();
        foreach (var option in Clothing[name].Options) {
          var localized = Localization.instance.Localize(option.Name);
          option.LocalizedName = localized;
          options.Add(localized);
        }

        var dropdown = chooser.GetComponent<Dropdown>();
        dropdown.name = name;
        dropdown.AddOptions(options);

        dropdown.onValueChanged.AddListener(delegate {
          OnValueChanged(dropdown);
        });

        Choosers[name] = dropdown;
      }

      private static void SetDefaultChoices() {
        foreach (var name in MenuOrder) {
          var dropdown = Choosers[name];
          var type = Clothing[name].Type;

          var equippedItem = FindEquipped(type);
          var equippedPrefabName = equippedItem?.m_dropPrefab?.name;

          var equippedOptionIndex = 0;
          foreach(var option in Clothing[name].Options) {
            if (option.PrefabName == equippedPrefabName) {
              dropdown.value = equippedOptionIndex;
              break;
            }
            equippedOptionIndex++;
          }
        }
      }

      private static void OnValueChanged(Dropdown dropdown) {
        // We change the value in SetDefaultChoices(), which always happens
        // right before we show the thing.  So ignore value changes that occur
        // while the panel is hidden.
        if (!IsVisible()) {
          return;
        }

        var name = dropdown.name;
        var localizedName = dropdown.options[dropdown.value].text;
        Logger.LogDebug($"Changing {name} to {localizedName}");

        var type = Clothing[name].Type;
        RemoveItemsOfType(type);

        foreach (var option in Clothing[name].Options) {
          if (option.LocalizedName == localizedName) {
            CreateAndEquip(option.PrefabName);
            break;
          }
        }
      }

      private static void CreateAndEquip(string prefabName) {
        var player = Player.m_localPlayer;
        var inventory = player.m_inventory;

        if (prefabName != null) {
          Logger.LogDebug($"Creating {prefabName}");
          var prefab = PrefabManager.Instance.GetPrefab(prefabName);
          var gameObject = UnityEngine.Object.Instantiate(prefab);
          var drop = gameObject.GetComponent<ItemDrop>();

          Logger.LogDebug($"Adding {prefabName} to inventory");
          // Add it to the known items of the player first, to suppress the
          // "new item" popup.
          player.m_knownMaterial.Add(drop.m_itemData.m_shared.m_name);
          inventory.AddItem(drop.m_itemData);

          Logger.LogDebug($"Equipping {prefabName}");
          player.EquipItem(drop.m_itemData, triggerEquipEffects: false);

          ZNetScene.instance.Destroy(gameObject);
        }
      }

      private static void RemoveItemsOfType(ItemDrop.ItemData.ItemType type) {
        var player = Player.m_localPlayer;
        var inventory = player.m_inventory;

        foreach (var item in FindItemsOfType(type)) {
          Logger.LogDebug($"Removing {item}");
          player.UnequipItem(item, triggerEquipEffects: false);
          inventory.RemoveItem(item);
        }
      }

      private static List<ItemDrop.ItemData> FindItemsOfType(ItemDrop.ItemData.ItemType type) {
        var player = Player.m_localPlayer;
        var inventory = player.m_inventory;
        var matchingItems = new List<ItemDrop.ItemData>();

        foreach (var item in inventory.m_inventory) {
          if (item.m_shared.m_itemType == type) {
            matchingItems.Add(item);
          }
        }

        return matchingItems;
      }

      private static ItemDrop.ItemData FindEquipped(ItemDrop.ItemData.ItemType type) {
        foreach (var item in FindItemsOfType(type)) {
          if (item.m_equiped) {
            return item;
          }
        }
        return null;
      }

      public static void Show() {
        if (Panel == null) {
          Init();
        }

        if (!IsVisible()) {
          SetDefaultChoices();
          Panel.SetActive(true);
          GUIManager.BlockInput(true);

          // Make the player face the camera for this.
          var mainCamera = Camera.main;
          var player = Player.m_localPlayer;
          var lookDirection =
              mainCamera.transform.position - player.m_eye.position;
          lookDirection.y = 0;
          lookDirection.Normalize();
          player.transform.rotation = Quaternion.LookRotation(lookDirection);

          // If it's dark, get out a torch so you can see your new clothes.
          if (!EnvMan.instance.IsDaylight()) {
            var torchType = ItemDrop.ItemData.ItemType.Torch;
            if (FindEquipped(torchType) == null) {
              var allTorches = FindItemsOfType(torchType);
              if (allTorches.Count > 0) {
                player.EquipItem(allTorches[0], triggerEquipEffects: true);
              }
            }
          }

          // Stand up straight, young man!
          player.SetCrouch(false);
        }
      }

      public static bool IsVisible() {
        return Panel != null && Panel.activeSelf;
      }

      public static void Hide() {
        if (IsVisible()) {
          GUIManager.BlockInput(false);
          Panel.SetActive(false);
        }
      }

      // This makes "Menu.Update()" ignore escape keys meant for us.  It will
      // ignore them when the inventory GUI is visible, among other conditions.
      // Rather than patch Menu.Update and replicate its logic, just signal
      // that the inventory GUI is visible any time the dress-up dialog is
      // visible.
      [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.IsVisible))]
      class GUIVisible_Patch {
        static bool Postfix(bool originalResult) {
          return originalResult || DressUpPanel.IsVisible();
        }
      }

      // Tack on to the end of Menu.Update to check the keyboard for an escape
      // key and hide the dialog.
      [HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
      class EscapeToClose_Patch {
        static void Postfix() {
          if (Input.GetKeyDown(KeyCode.Escape) ||
              // These are the joystick buttons used to leave the settings menu.
              ZInput.GetButtonDown("JoyBack") ||
              ZInput.GetButtonDown("JoyButtonB")) {
            Hide();
          }
        }
      }
    }

    [PokeheimInit]
    public static void Init() {
      Utils.OnVanillaPrefabsAvailable += delegate {
        RegisterWardrobe();
      };

      Utils.OnFirstSceneStart += delegate {
        DressUpPanel.RegisterAllClothing();
      };

      Utils.OnVanillaLocationsAvailable += delegate {
        var templeObject = Utils.GetSpawnedLocationOrPrefab("StartTemple");
        var wardrobePrefab = PrefabManager.Instance.GetPrefab(WardrobeName);

        Transform wardrobeTransform =
            templeObject.transform.Find(WardrobeName + "(Clone)");
        GameObject wardrobe = null;
        if (wardrobeTransform != null) {
          Logger.LogDebug($"Found an existing {WardrobeName} at the temple!");
          wardrobe = wardrobeTransform.gameObject;
        } else {
          Logger.LogDebug($"Found no {WardrobeName} at the temple!");
          wardrobe = UnityEngine.Object.Instantiate(
              wardrobePrefab, templeObject.transform);
        }

        // Place it on the ground, a little off-center from the starting area.
        wardrobe.transform.localPosition = new Vector3(-5f, -0.1f, -5f);

        // Spin it to face the center of the circle.
        wardrobe.transform.rotation = Quaternion.Euler(0f, -135f, 0f);
      };

      CommandManager.Instance.AddConsoleCommand(new DressUpCommand());
    }

    private static void RegisterWardrobe() {
      var prefab = PrefabManager.Instance.GetPrefab(WardrobeName);
      if (prefab != null) {
        Logger.LogDebug($"Found {WardrobeName} already registered!");
        return;
      }

      // Clone a stone chest.
      prefab = PrefabManager.Instance.CreateClonedPrefab(
          WardrobeName, "loot_chest_stone");

      // Stretch it into shape.
      prefab.transform.localScale = new Vector3(
          prefab.transform.localScale.x,
          prefab.transform.localScale.y * 5f,
          prefab.transform.localScale.z);

      var container = prefab.GetComponent<Container>();

      // Name it.
      container.m_name = WardrobeName;

      // Let anyone use it.
      container.m_checkGuardStone = false;
      container.m_privacy = Container.PrivacySetting.Public;

      // Make it last forever.
      UnityEngine.Object.Destroy(container.GetComponent<WearNTear>());
      UnityEngine.Object.Destroy(container.GetComponent<Destructible>());

      // AddPrefab works early, and RegisterToZNetScene works late.
      PrefabManager.Instance.AddPrefab(prefab);
      PrefabManager.Instance.RegisterToZNetScene(prefab);
    }

    class DressUpCommand : ConsoleCommand {
      public override string Name => "dressup";
      public override string Help => " - Open the dress-up dialog";

      public override void Run(string[] args) {
        DressUpPanel.Show();
      }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    class WardrobeHover_Patch {
      static void Postfix(Container __instance, ref string __result) {
        var container = __instance;
        if (container.m_name == WardrobeName) {
          // Don't show it as "empty".
          var text = "$item_wardrobe";
          text += "\n[<color=yellow><b>$KEY_Use</b></color>] ";
          text += "$piece_container_open";
          __result = Localization.instance.Localize(text);
        }
      }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    class WardrobeInteract_Patch {
      static bool Prefix(Container __instance, Humanoid character) {
        var container = __instance;
        if (container.m_name != WardrobeName) {
          // Fall back on original behavior for all normal containers.
          return true;
        }

        DressUpPanel.Show();

        // Skip the original method.
        return false;
      }
    }

    // We're just playing dress-up, so DLC items are available.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem), new Type[] {
      typeof(ItemDrop.ItemData), typeof(bool),
    })]
    class NoDLCRequired_Patch {
      static void Prefix(ItemDrop.ItemData item) {
        item.m_shared.m_dlc = "";
      }
    }
  }
}
