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
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  public static class Credits {
    private static Dictionary<string, List<Contributor>> contributors = new Dictionary<string, List<Contributor>> {
      {"Code", new List<Contributor> {
        new Contributor {
          name = "Joey Parrish",
          link = "https://joeyparrish.github.io/",
        },
      }},

      {"Custom Music", new List<Contributor> {
        new Contributor {
          name = "Arranged by Joey Parrish",
        },
      }},

      {"Beta Testing", new List<Contributor> {
        new Contributor {
          name = "Andrew",
        },
        new Contributor {
          name = "Timberjaw",
          link = "https://github.com/Timberjaw",
        },
      }},

      {"Special Thanks", new List<Contributor> {
        new Contributor {
          name = "TheSlowPianist, for their Valheim arrangements",
          link = "https://www.patreon.com/theslowpianist/",
        },
        new Contributor {
          name = "Jules, Redseiko, and A Sharp Pen, for help via discord",
          link = "https://discord.gg/DdUt6g7gyA",
        },
        new Contributor {
          name = "Jötunn: The Valheim Library",
          link = "https://valheim-modding.github.io/Jotunn/",
        },
      }},
    };

    private static Dictionary<string, List<Contributor>> translators = new Dictionary<string, List<Contributor>> {
      { "Original English text", new List<Contributor> {
        new Contributor {
          name = "Joey Parrish",
        },
      }},

      { "Deutsche Übersetzung", new List<Contributor> {
        new Contributor {
          name = "Joey Parrish",
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

    // Let the outro play for this long before replacing it with credits.
    private const float outroTime = 40f;  // seconds

    private static bool rollingCreditsText = false;

    private class Contributor {
      public string name;
      public string link = "";  // Optional

      public string Format(bool brief) {
        if (!brief && link != "") {
          return name + "\n" + link;
        } else {
          return name;
        }
      }
    }

    // Let the contributors list play for this long before replacing it with
    // translators.
    private const float contributorsTime = 40f;  // seconds

    // Let the translators list play for this long before ending the animation.
    private const float translatorsTime = 50f;  // seconds

    public const float totalCreditsTime =
        outroTime + contributorsTime + translatorsTime;

    // Roll credits, with or without the outro text.
    public static void Roll(bool withOutro) {
      if (withOutro) {
        RollText("$pokeheim_outro", outroTime, () => RollCreditsOnly());
      } else {
        RollCreditsOnly();
      }
    }

    private static string CreditsHeader(string headerText) {
      return $"<size=40><color=orange>{headerText}</color></size>";
    }

    private static void RollCreditsOnly() {
      var contributorsText = CreditsHeader("$pokeheim_contributors") + "\n\n" +
          GetContributorsText(contributors, brief: true);
      var translatorsText = CreditsHeader("$pokeheim_translators") + "\n\n" +
          GetContributorsText(translators, brief: true);

      rollingCreditsText = true;

      RollText(contributorsText, contributorsTime, () => {
        if (!rollingCreditsText) {
          // Cancelled by the user.
          return;
        }

        RollText(translatorsText, translatorsTime, () => {
          StopText();
          rollingCreditsText = false;
        });
      });
    }

    private static string GetContributorsText(
        Dictionary<string, List<Contributor>> list, bool brief) {
      string text = "";

      foreach (var entry in list) {
        var subHeading = entry.Key;
        var subList = entry.Value;
        text += $"<color=orange>{subHeading}</color>\n";

        foreach (var contributor in subList) {
          text += contributor.Format(brief) + "\n";
          if (!brief) {
            text += "\n";
          }
        }
        text += "\n";
      }

      return text;
    }

    private static void StopText() {
      // If we're already showing something, we need to stop the animation
      // (ResetTrigger), then reset the associated parts of the UI (Rebind) so
      // that the scrolling position of the text resets.
      TextViewer.instance.m_animatorIntro.ResetTrigger("play");
      TextViewer.instance.m_animatorIntro.Rebind();
    }

    private static void RollText(string text, float timeout = 0f, Action thenDoThis = null) {
      StopText();

      TextViewer.instance.ShowText(
          TextViewer.Style.Intro,
          /* topic, seems to be ignored */ "",
          text,
          autoHide: false);

      if (thenDoThis != null) {
        Game.instance.DelayCall(timeout, thenDoThis);
      }
    }

    private static float GetTextHeight(
        Text template, string newText, int newFontSize = 0) {
      TextGenerationSettings generationSettings =
          template.GetGenerationSettings(template.rectTransform.rect.size);
      if (newFontSize != 0) {
        generationSettings.fontSize = newFontSize;
      }
      TextGenerator textGen = new TextGenerator();
      return textGen.GetPreferredHeight(newText, generationSettings);
    }

    [HarmonyPatch(typeof(TextViewer), nameof(TextViewer.LateUpdate))]
    class CancelCreditsWithEscape_Patch {
      static void Postfix() {
        if (TextViewer.IsShowingIntro() && rollingCreditsText &&
            Input.GetKeyDown(KeyCode.Escape)) {
          rollingCreditsText = false;
          StopText();
        }
      }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    class HookIntoMainMenuCredits_Patch {
      static void Postfix(FejdStartup __instance) {
        var creditsList = __instance.m_creditsList;

        Text templateHeading =
            creditsList.Find("Irongate")?.GetComponent<Text>();
        if (templateHeading == null) {
          Logger.LogError("Failed to find UI heading to hook credits!");
          return;
        }

        Text heading = UnityEngine.Object.Instantiate(
            templateHeading,
            templateHeading.transform.parent).GetComponent<Text>();

        Text text = null;
        foreach (var child in heading.GetComponentsInChildren<Text>()) {
          if (child != heading) {
            text = child;
            break;
          }
        }
        if (text == null) {
          Logger.LogError("Failed to find UI text to hook credits!");
          return;
        }

        var headingText = "Pokéheim";
        var headingHeight = heading.rectTransform.sizeDelta.y;

        // Use a smaller font size
        var textText = "\n" +
                       GetContributorsText(contributors, brief: false) +
                       GetContributorsText(translators, brief: false);
        var textHeight = GetTextHeight(text, textText, text.fontSize);

        var paddingHeight = headingHeight * 2f;
        var totalHeight = headingHeight + textHeight + paddingHeight;
        Logger.LogDebug($"Added credits size: {totalHeight}");

        var headingPosition = heading.rectTransform.position;
        headingPosition.y += totalHeight;
        heading.rectTransform.position = headingPosition;

        heading.text = headingText;
        text.text = textText;

        // Grow the text area to fit the new text.  Make it 1.5x as wide as it
        // used to be.
        text.rectTransform.sizeDelta = new Vector2(
            text.rectTransform.sizeDelta.x * 1.5f,
            textHeight);

        // Grow the credits list to account for the new text, as well.
        creditsList.sizeDelta = new Vector2(
            creditsList.sizeDelta.x,
            creditsList.sizeDelta.y + totalHeight);
        Logger.LogDebug($"Overall credits size: {creditsList.rect}");

        // Shift all the credits children down.
        foreach (Transform child in creditsList) {
          var childTransform = child.GetComponent<RectTransform>();
          if (childTransform != null &&
              // NOTE: The "Thank you!" text is anchored to the bottom of the
              // credits object, so don't adjust its position.
              child.gameObject.name != "Thank you") {
            var position = childTransform.position;
            position.y -= totalHeight;
            Logger.LogDebug(
                $"Adjusted credits element: {childTransform}" +
                $" from {childTransform.position} to {position}");
            childTransform.position = position;
          }
        }
      }
    }

    [RegisterCommand]
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
