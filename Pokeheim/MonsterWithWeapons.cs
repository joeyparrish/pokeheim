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
using UnityEngine;
using System;
using System.Collections.Generic;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public class MonsterWithWeapons : MonoBehaviour {
    public List<ItemDrop.ItemData> allWeapons = new List<ItemDrop.ItemData>();
    public ItemDrop.ItemData firstWeapon = null;
    public ItemDrop.ItemData secondWeapon = null;

    private Humanoid humanoid = null;

    private void Awake() {
      humanoid = this.GetComponent<Humanoid>();
    }

    private void Start() {
      var enemy = ItemDrop.ItemData.AiTarget.Enemy;

      foreach (var item in humanoid.m_inventory.GetAllItems()) {
        var targetType = item.m_shared.m_aiTargetType;
        if (item.IsWeapon() && targetType == enemy) {
          allWeapons.Add(item);

          // Track the first two "items" that do damage.
          var itemDamage = item.m_shared.m_damages.GetTotalDamage();
          if (itemDamage != 0) {
            if (this.firstWeapon == null) {
              this.firstWeapon = item;
            } else if (this.secondWeapon == null) {
              this.secondWeapon = item;
            }
          }
        }
      }
    }

    public bool EquipWeapon(bool secondary) {
      var weapon = secondary ? secondWeapon : firstWeapon;
      if (weapon == null) {
        Logger.LogDebug($"No such weapon!");
        return false;
      }
      return humanoid.EquipItem(weapon);
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Awake))]
    static class TrackMonsterWeapons_Patch {
      static void Postfix(Humanoid __instance) {
        var humanoid = __instance;
        humanoid.gameObject.AddComponent<MonsterWithWeapons>();
      }
    }
  }
}
