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

namespace Pokeheim {
  public static class PlayerMods {
    // Don't take damage from the monsters.  Clothing is just for decoration.
    [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
    class PlayerTakesNoDamage_Patch {
      static float Postfix(float originalResult) {
        return 1e9f;
      }
    }

    // Increase base HP in case of a fall, and max weight so the player doesn't
    // have to think about carrying too much.
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    class PlayerHasMoreHPAndCanCarryMore_Patch {
      static void Postfix(Player __instance) {
        var player = __instance;
        player.m_baseHP = 50f;
        player.m_maxCarryWeight = 1000f;
      }
    }

    // The player shouldn't have to think about stamina in Pokeheim.
    [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
    class PlayerAlwaysHasStamina_Patch {
      static void Prefix(Player __instance, ref float v) {
        // Except while swimming.  You can't swim across the ocean.
        // That's what saddles are for.
        if (!__instance.IsSwimming()) {
          v = 0f;  // No stamina used.
        }
      }
    }

    // Clothing is just for decoration in Pokeheim, and you can't craft
    // potions, so the player is never freezing.
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.IsFreezing))]
    class PlayerIsNeverFreezing_Patch {
      static bool Postfix(bool originalResult) {
        return false;
      }
    }

    // There's no good way to get keys in Pokeheim, so let's just unlock all
    // the doors.
    [HarmonyPatch(typeof(Door), nameof(Door.Awake))]
    class PlayerDoesNotNeedAKey_Patch {
      static void Prefix(ref ItemDrop ___m_keyItem) {
        ___m_keyItem = null;
      }
    }

    // Don't create a tombstone, and let the player keep everything when they
    // die.
    [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
    class PlayerKeepsEverythingOnDeath_Patch {
      static bool Prefix() {
        return false;
      }
    }

    // Players can always punch their own monsters, regardless of PVP setting.
    // The disadvantage of achieving it in this simple way is that fists will
    // never work against wild monsters.
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    class PlayerCanAlwaysAbusePokemon_Patch {
      static void Postfix(Player __instance) {
        var player = __instance;
        player.m_unarmedWeapon.m_itemData.m_shared.m_tamedOnly = true;
      }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    class ModifyPlayerAttacks_Patch {
      static void Prefix(Character __instance, HitData hit) {
        var monster = __instance;
        Character attacker = hit.GetAttacker();

        if (attacker != null && attacker.IsPlayer()) {
          if (monster.IsBoss()) {
            // The boss is immune to attacks from the Player.  You _must_ use
            // captured monsters on a boss.
            hit.ApplyModifier(0f);
          } else if (hit.m_skill == Skills.SkillType.Unarmed) {
            // Beating up your monsters is brutal.  Apply these stats:
            hit.m_damage.m_damage = 20f;
            hit.m_damage.m_blunt = 20f;
          } else {
            // No matter what badass weapons you bring into Pokeheim (which you
            // shouldn't do!), they will all do about 5 damage, equivalent to a
            // club.  This forces people to play Pokeheim the way it was meant.
            // We will back up fire damage, though, and leave that alone, so
            // that torches stay pretty useful on early monsters.
            var fireDamage = hit.m_damage.m_fire;
            hit.m_damage.m_fire = 0f;

            var totalDamage = hit.GetTotalDamage();
            hit.ApplyModifier(5f / totalDamage);

            hit.m_damage.m_fire = fireDamage;
          }
        }
      }
    }
  }
}
