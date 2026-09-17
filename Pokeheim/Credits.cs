/**
 * Pokeheim - A Valheim Mod
 * Copyright (C) 2021-2026 Joey Parrish
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
using System;
using System.Collections.Generic;
using TMPro;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  [Feature(Features.Credits)]
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

    // The name we give our own credits block, so that we can recognize it
    // later and avoid adding it twice.
    private const string PokeheimCreditsName = "Pokeheim Credits";

    // Measure text without disturbing the component it is measured with.
    // GetPreferredValues uses that component's current settings but leaves
    // both the settings and the displayed text alone, so it can run before the
    // new text is assigned.
    //
    // "width" is what the text wraps against, so pass the width the text is
    // going to have, not the one it has now.  A height of 0 means
    // "unconstrained", which is what we want since height is the answer.
    private static float GetTextHeight(
        TMP_Text template, string newText, float width) {
      return template.GetPreferredValues(newText, width, 0f).y;
    }

    // The credits list grows downward from a pivot at its top, so a child
    // anchored to the top is positioned relative to a point that does not
    // move, and is the only kind that has to be moved by hand to make room.
    // Children anchored to the bottom (the "Thank you" line) or to the middle
    // (a layout ruler, see below) already follow the list as it grows, and
    // moving those as well would move them twice.
    private static bool IsTopAnchored(RectTransform rect) {
      return Mathf.Approximately(rect.anchorMin.y, 1f) &&
             Mathf.Approximately(rect.anchorMax.y, 1f);
    }

    // Valheim leaves a disabled, empty RectTransform in the credits list whose
    // name states what it measures: the gap between the bottom of the credits
    // text and the top of the "Thank you" line.  Reading its height spaces our
    // section the way Valheim spaces its own, and keeps us in step if they
    // ever retune the layout.  The fallback is that height as of 1.0.12,
    // rounded, in case the ruler is ever removed.
    private const string PaddingRulerName = "Ruler_TopThankYou-to-BottomOfText";
    private const float FallbackPaddingHeight = 548f;

    private static float GetPaddingHeight(Transform creditsList) {
      var ruler = creditsList.Find(PaddingRulerName) as RectTransform;
      if (ruler == null) {
        Logger.LogWarning(
            $"No {PaddingRulerName} in the credits to measure padding with.");
        return FallbackPaddingHeight;
      }
      return ruler.rect.height;
    }

    [HarmonyPatch(typeof(TextViewer), nameof(TextViewer.LateUpdate))]
    class CancelCreditsWithEscape_Patch {
      static void Postfix() {
        if (TextViewer.IsShowingIntro() && rollingCreditsText &&
            Input.GetKeyDown(KeyCode.Escape)) {
          rollingCreditsText = false;
          StopText();

          // This will cancel any existing timer for respawning the player and
          // respawn them right away.
          if (!Game.instance.WaitingForRespawn()) {
            Game.instance.RequestRespawn(0f);
          }
        }
      }
    }

    // Add a Pokeheim section to the credits shown from the main menu.
    //
    // The credits panel is inactive at this point, which is why the search for
    // the text object below has to ask for inactive objects.  Measuring the
    // text still works; TextMeshProUGUI will compute a preferred size for a
    // component whose object is not active.
    //
    // FejdStartup.Start later runs Localization.Localize over this whole
    // transform.  That leaves text containing no "$token" alone, so what we
    // write here survives it.
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    class HookIntoMainMenuCredits_Patch {
      static void Postfix(FejdStartup __instance) {
        var creditsList = __instance.m_creditsList;

        if (creditsList.Find(PokeheimCreditsName) != null) {
          // Somebody already did this.
          return;
        }

        TextMeshProUGUI templateHeading =
            creditsList.Find("Irongate")?.GetComponent<TextMeshProUGUI>();
        if (templateHeading == null) {
          Logger.LogError("Failed to find UI heading to hook credits!");
          return;
        }

        // Where the vanilla credits currently begin.  Our section takes this
        // spot, and the vanilla ones move down into the room we make below.
        var startPosition = templateHeading.rectTransform.anchoredPosition;

        var heading = UnityEngine.Object.Instantiate(
            templateHeading, templateHeading.transform.parent);
        heading.name = PokeheimCreditsName;

        TextMeshProUGUI text = null;
        foreach (var child in
                 heading.GetComponentsInChildren<TextMeshProUGUI>(
                     includeInactive: true)) {
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
        var bodyText = "\n" +
                       GetContributorsText(contributors, brief: false) +
                       GetContributorsText(translators, brief: false);

        // Auto-sizing would make the measurement below meaningless, since the
        // size TMP settles on depends on the box we have not sized yet.
        text.enableAutoSizing = false;

        // Make the text area half again as wide as the vanilla one, and
        // measure against that width rather than the width it has now.
        var textWidth = text.rectTransform.rect.width * 1.5f;
        var textHeight = GetTextHeight(text, bodyText, textWidth);

        var headingHeight = heading.rectTransform.rect.height;
        var paddingHeight = GetPaddingHeight(creditsList);
        var totalHeight = headingHeight + textHeight + paddingHeight;
        Logger.LogDebug(
            $"Added credits size: heading={headingHeight}" +
            $" text={textHeight} padding={paddingHeight} total={totalHeight}");

        heading.text = headingText;
        text.text = bodyText;

        // SetSizeWithCurrentAnchors rather than assigning sizeDelta directly:
        // on a stretched rect, sizeDelta is an offset from the anchors and not
        // a size, so assigning a size to it would be wrong.  This sets the
        // size either way.
        text.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal, textWidth);
        text.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical, textHeight);

        // Grow the list to make room.  Height is also what gives the new text
        // time to scroll past: FejdStartup.Update scrolls until the bottom of
        // this rect rises above half the viewport, then stops.
        creditsList.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            creditsList.rect.height + totalHeight);

        heading.rectTransform.anchoredPosition = startPosition;

        // Move the vanilla credits down into the space we just made.
        foreach (Transform child in creditsList) {
          var childRect = child as RectTransform;
          if (childRect == null ||
              childRect == heading.rectTransform ||
              !IsTopAnchored(childRect)) {
            continue;
          }

          // anchoredPosition, not position.  Every size above is in this
          // rect's own units, and the credits live under a scaled canvas
          // (CanvasScaler plus GuiScaler), so doing this arithmetic in world
          // space would be wrong by the canvas scale factor at any resolution
          // where that factor is not 1.
          var position = childRect.anchoredPosition;
          position.y -= totalHeight;
          childRect.anchoredPosition = position;
        }

        Logger.LogDebug($"Overall credits size: {creditsList.rect}");
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
