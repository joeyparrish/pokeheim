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

namespace Pokeheim {
  public static class Sounds {
    private static AudioClip hit = null;
    private static AudioClip poof = null;

    public static AudioClip Hit => hit;
    public static AudioClip Poof => poof;

    [PokeheimInit]
    public static void Init() {
      Utils.OnVanillaPrefabsAvailable += delegate {
        hit = Utils.StealFromPrefab<ZSFX, AudioClip>(
            "sfx_greydwarf_stone_hit", sfx => sfx.m_audioClips[0]);
        poof = Utils.StealFromPrefab<ZSFX, AudioClip>(
            "sfx_raven_teleport", sfx => sfx.m_audioClips[0]);
      };
    }

    public static void PlayAt(this AudioClip clip, Vector3 position) {
      GameObject gameObject = new GameObject("TempAudio");
      gameObject.transform.position = position;

      AudioSource audioSource = gameObject.AddComponent<AudioSource>();
      audioSource.clip = clip;
      audioSource.reverbZoneMix = 0.1f;
      audioSource.maxDistance = 200f;
      audioSource.spatialBlend = 1f;
      audioSource.rolloffMode = AudioRolloffMode.Linear;

      audioSource.Play();

      // Destroy the game object after playback is complete.
      UnityEngine.Object.Destroy(gameObject, clip.length);
    }
  }
}
