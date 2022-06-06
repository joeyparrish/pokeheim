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

using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using System;
using System.Collections.Generic;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class Credits {
    // Let the outro play for this long before replacing it with credits.
    private const float outroTime = 40f;  // seconds

    private class Contributor {
      public string name;
      public string link = "";  // Optional

      new public string ToString() {
        if (link != "") {
          return name + " - " + link;
        } else {
          return name;
        }
      }
    }

    // Let the contributors list play for this long before replacing it with
    // translators.
    private const float contributorsTime = 30f;  // seconds

    private static List<Contributor> contributors = new List<Contributor> {
      new Contributor {
        name = "Joey Parrish",
        link = "https://github.com/joeyparrish",
      },

      // Add new entries above this line.
    };

    private static Dictionary<string, List<Contributor>> translators = new Dictionary<string, List<Contributor>> {
      { "Original English text", new List<Contributor> {
        new Contributor {
          name = "Joey Parrish",
          link = "https://github.com/joeyparrish",
        },
      }},

      // Add new entries above this line.

      // These come last.
      { "\"Borrowed\" translations", new List<Contributor> {
        new Contributor {
          name = "Pokémon Go Android APK",
          link = "https://pokemongolive.com/",
        },
      }},
      { "Translate Pokéheim", new List<Contributor> {
        new Contributor {
          name = "https://github.com/joeyparrish/pokeheim#translate",
        },
      }},
    };

    // Roll credits, with or without the outro text.
    public static void Roll(bool withOutro) {
      if (withOutro) {
        RollText(GetOutro(), outroTime, () => RollCreditsOnly());
      } else {
        RollCreditsOnly();
      }
    }

    private static void RollCreditsOnly() {
      RollText(GetContributors(), contributorsTime, () => {
        RollText(GetTranslators());
      });
    }

    private static string GetOutro() {
      return "$pokeheim_outro";
    }

    private static string GetContributors() {
      var text = "$pokeheim_contributors\n===== ===== =====\n";
      foreach (var contributor in contributors) {
        text += contributor.ToString() + "\n";
      }
      return text;
    }

    private static string GetTranslators() {
      var text = "$pokeheim_translators\n\n";
      foreach (var entry in translators) {
        var language = entry.Key;
        text += language + "\n===== ===== =====\n";

        foreach (var translator in entry.Value) {
          text += translator.ToString() + "\n";
        }
        text += "\n\n";
      }
      return text;
    }

    private static void RollText(string text, float timeout = 0f, Action thenDoThis = null) {
      // If we're already showing something, we need to stop the animation
      // (ResetTrigger), then reset the associated parts of the UI (Rebind) so
      // that the scrolling position of the text resets.
      TextViewer.instance.m_animatorIntro.ResetTrigger("play");
      TextViewer.instance.m_animatorIntro.Rebind();

      TextViewer.instance.ShowText(
          TextViewer.Style.Intro,
          /* topic, seems to be ignored */ "",
          text,
          autoHide: false);

      if (thenDoThis != null) {
        Game.instance.DelayCall(timeout, thenDoThis);
      }
    }

    [PokeheimInit]
    public static void Init() {
      CommandManager.Instance.AddConsoleCommand(new CreditsCommand());
    }

    class CreditsCommand : ConsoleCommand {
      public override string Name => "credits";
      public override string Help => "Rolls the credits for Pokeheim.";
      public override bool IsCheat => true;

      public override void Run(string[] args) {
        Credits.Roll(withOutro: false);
      }
    }
  }
}
