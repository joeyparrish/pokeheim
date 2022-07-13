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
using System.Collections.Generic;
using UnityEngine;

namespace Pokeheim {
  public static class Sounds {
    public enum SoundType {
      Hit = 0,
      Poof = 1,
    }

    private static Dictionary<SoundType, AudioClip> clips =
        new Dictionary<SoundType, AudioClip>();

    [PokeheimInit]
    public static void Init() {
      Utils.OnVanillaPrefabsAvailable += delegate {
        clips[SoundType.Hit] = Utils.StealFromPrefab<ZSFX, AudioClip>(
            "sfx_greydwarf_stone_hit", sfx => sfx.m_audioClips[0]);
        clips[SoundType.Poof] = Utils.StealFromPrefab<ZSFX, AudioClip>(
            "sfx_raven_teleport", sfx => sfx.m_audioClips[0]);
      };
      Utils.OnRPCsReady += delegate {
        ZRoutedRpc.instance.Register<int, Vector3>(
            "PokeheimSoundsPlayAt", RPC_PlayAt);
      };
    }

    public static void PlayAt(this SoundType type, Vector3 position) {
      ZRoutedRpc.instance.InvokeRoutedRPC(
          ZRoutedRpc.Everybody,
          "PokeheimSoundsPlayAt",
          (int)type, position);
    }

    private static void RPC_PlayAt(long sender, int typeInt, Vector3 position) {
      SoundType type = (SoundType)typeInt;
      AudioClip clip = clips[type];

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
