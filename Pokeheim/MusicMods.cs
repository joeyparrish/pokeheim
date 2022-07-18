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
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class MusicMods {
    private static readonly Dictionary<string, string> OverrideMusic = new Dictionary<string, string> {
      {"menu", "Main-Menu.mp3"},
      {"morning", "Dawn-short-version.mp3"},
    };

    static AudioClip LoadAudioClip(string relativePath) {
      var absolutePath = Utils.GetAssetPath(relativePath);
      var pathUrl = "file:///" + absolutePath.Replace("\\", "/");
      var request = UnityWebRequestMultimedia.GetAudioClip(
          pathUrl, AudioType.MPEG);

      request.SendWebRequest();
      while (!request.isDone) {}

      if (request.error != null) {
        Logger.LogError($"Failed to load clip from {absolutePath}: {request.error}");
        return null;
      }

      var downloadHandler = request.downloadHandler as DownloadHandlerAudioClip;
      return downloadHandler?.audioClip;
    }

    // Patch in our custom music.
    [HarmonyPatch(typeof(MusicMan), nameof(MusicMan.Awake))]
    class CustomMusic_Patch {
      static void Postfix() {
        foreach (var music in MusicMan.instance.m_music) {
          if (OverrideMusic.ContainsKey(music.m_name)) {
            AudioClip audioClip = LoadAudioClip(OverrideMusic[music.m_name]);
            if (audioClip == null) {
              Logger.LogError($"Failed to load music override: {music.m_name}");
            } else {
              Logger.LogDebug($"Overriding music: {music.m_name}, orig. volume {music.m_volume}");
              music.m_clips = new AudioClip[]{ audioClip };
              music.m_volume = 1f;
            }
          } else {
            Logger.LogDebug($"Not overriding music: {music.m_name}, volume {music.m_volume}");
          }
        }
      }
    }

#if DEBUG
    [RegisterCommand]
    class PlayMusic : ConsoleCommand {
      public override string Name => "playmusic";
      public override string Help => "[key] Play a specific piece of music by its key.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        if (args.Length > 0) {
          var key = args[0];

          foreach (var music in MusicMan.instance.m_music) {
            if (music.m_name == key) {
              MusicMan.instance.StopMusic();
              MusicMan.instance.StartMusic(music);
              return;
            }
          }

          Debug.Log($"Music not found: \"{key}\"");
        } else {
          Debug.Log($"Please specify a piece of music to play.");
        }
      }
    }
#endif
  }
}
