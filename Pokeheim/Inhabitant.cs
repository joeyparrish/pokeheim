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

using Jotunn.Managers;
using UnityEngine;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public class Inhabitant {
    private int level = 0;
    private string petName = "";
    private MonsterMetadata.Metadata metadata = null;

    // We don't store the input prefab name.  Instead, we rely on Metadata for
    // that.  This has a normalizing effect for prefabs that are aliased to one
    // another.  Without this, we end up with overlapping Pokedex entries,
    // multiple counters for the same thing, and an army of Skeletons with zero
    // archers among them.
    public string PrefabName => metadata.PrefabName;
    public int Level => level;
    public string PetName => petName;
    public Character.Faction Faction => metadata.Faction;
    public string GenericName => metadata.GenericName;
    public string LocalizedGenericName => metadata.LocalizedGenericName;
    public string Name => petName.Length > 0 ? petName : GenericName;

    public Inhabitant(string stringifiedInhabitant) {
      var pieces = stringifiedInhabitant.Split(new char[]{'|'}, count: 3);

      var suppliedPrefabName = pieces[0];
      level = int.Parse(pieces[1]);
      petName = pieces[2].Replace('_', ' ');
      metadata = MonsterMetadata.Get(suppliedPrefabName);

      if (metadata == null || metadata.Incomplete()) {
        Logger.LogError($"Incomplete metadata {metadata.PrefabName} from string {stringifiedInhabitant}");
      }
    }

    public Inhabitant(Character monster) {
      var suppliedPrefabName = monster.GetPrefabName();
      level = monster.GetLevel();
      // NOTE: Do not use GetHoverName(), since that is pre-localized.
      // Instead, store exactly what the user named the monster, which may be
      // nothing.
      petName = monster.GetPetName();
      metadata = MonsterMetadata.Get(suppliedPrefabName);

      if (metadata == null || metadata.Incomplete()) {
        Logger.LogError($"Incomplete metadata {metadata} from monster {monster}");
      }
    }

    override public string ToString() {
      return PrefabName + "|" + level.ToString() + "|" + petName.Replace(' ', '_');
    }

    public int CompareTo(Inhabitant other) {
      // Essentially, Pokedex order.
      var comparison = metadata.CompareTo(other.metadata);
      // Same monster?  Compare levels.
      if (comparison == 0) {
        comparison = level.CompareTo(other.level);
      }
      // Same level?  Compare pet names.
      if (comparison == 0) {
        comparison = petName.CompareTo(other.petName);
      }
      return comparison;
    }

    // Used by migration code in ShinyMods.cs and BallItem.cs to upgrade
    // Pokeheim v1 inhabitants to Pokeheim v2 inhabitants.
    public void UpgradeLevel(int level) {
      this.level = level;
    }

    public void Recreate(Vector3 position, Player player) {
      var prefab = PrefabManager.Instance.GetPrefab(PrefabName);
      Quaternion rotation = Quaternion.identity;
      GameObject gameObject = UnityEngine.Object.Instantiate(
          prefab, position, rotation);

      Character monster = gameObject.GetComponent<Character>();

      monster.SetLevel(level);
      monster.ObeyMe(player);
      monster.SetPetName(petName);  // After ObeyMe, so it can be named!
      Sounds.SoundType.Poof.PlayAt(position);
    }

    public string GetFullName() {
      string name = "";
      if (petName == "" || petName == LocalizedGenericName) {
        name = GenericName;
      } else {
        name = $"{petName} ({GenericName})";
      }

      if (level > 1) {
        name += " *";
      }

      return name;
    }

    public string GetDescription() {
      var description = "$stats_name: ";
      if (petName == "" || petName == LocalizedGenericName) {
        description += "$stats_name_none\n";
      } else {
        description += $"{petName}\n";
      }

      description += $"$stats_species: {GenericName}\n";
      if (level > 1) {
        description += $"$stats_shiny!\n";
      }
      description += $"$stats_type: {metadata.FactionName}\n";

      // HP for a real monster will be the base health (hard-coded for prefab)
      // times the level (applied in SetupMaxHealth) times HealthBump (applied
      // in our ObeyMe method).
      var hp = metadata.BaseHealth * level * Captured.HealthBump;

      description += $"$stats_hp: {hp}\n";
      description += $"$stats_damage: {metadata.TotalDamage * level}\n";
      description += $"$stats_catch_rate: {metadata.CatchRate:P2}";

      return description;
    }

    public Sprite[] GetIcons() {
      return new Sprite[] {
        level > 1 ? metadata.CapturedShinyIcon : metadata.CapturedIcon,
      };
    }
  }
}
