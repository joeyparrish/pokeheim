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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Logger = Jotunn.Logger;

namespace Pokeheim {
  // Marks a type as belonging to a named feature, and optionally names the
  // other features it needs.  Nested types inherit the feature of the type that
  // encloses them, so marking a feature's top-level static class covers its
  // patches, its Init() methods and its console commands all at once.
  //
  // Being compiled in is what enables a feature.  There is no list of enabled
  // features to keep in sync: to leave one out, either leave its source out of
  // the Compile list in Pokeheim.csproj, or mark it [DisableFeature].
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
                  Inherited = false)]
  public class FeatureAttribute : Attribute {
    public string Name { get; private set; }

    // Settable so that it can be named at the call site.  Both of these work:
    //
    //   [Feature(Features.Captured, dependsOn: new[] { Features.Pokedex })]
    //   [Feature(Features.Captured, DependsOn = new[] { Features.Pokedex })]
    //
    // The first names the constructor parameter, the second assigns this
    // property.  C# reserves "=" in an attribute for fields and properties and
    // ":" for constructor parameters, which is why the casing differs between
    // the two.
    public string[] DependsOn { get; set; }

    public FeatureAttribute(string name) {
      this.Name = name;
      this.DependsOn = new string[0];
    }

    public FeatureAttribute(string name, string[] dependsOn) {
      this.Name = name;
      this.DependsOn = dependsOn ?? new string[0];
    }
  }

  // Turns a feature off without taking it out of the build.  Anything that
  // depends on it is skipped in turn.
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
                  Inherited = false)]
  public class DisableFeatureAttribute : Attribute {
    public string Reason { get; private set; }

    public DisableFeatureAttribute(string reason = null) {
      this.Reason = reason;
    }
  }

  // Applies the mod one feature at a time, in dependency order, so that a game
  // update which breaks one feature disables that feature and whatever needs
  // it, rather than the whole mod.
  //
  // Each feature gets its own Harmony instance, which is what makes clean
  // rollback possible: if a feature's third patch throws, its first two are
  // already applied, and UnpatchSelf() on that feature's own instance removes
  // them without touching any other feature.
  public static class Features {
    // Core is an implicit dependency of every feature, so nothing declares it.
    // It carries the lifecycle hooks in Utils that the rest of the mod waits
    // on, and without those the mod is not working in any useful sense, so a
    // Core failure skips everything.
    public const string Core = "Core";

    public const string Logo = "Logo";
    public const string Music = "Music";
    public const string Version = "Version";
    public const string MenuVersion = "MenuVersion";
    public const string Intro = "Intro";
    public const string LoadingScreen = "LoadingScreen";
    public const string Pokedex = "Pokedex";

    public enum Status {
      // Patched, initialized and registered successfully.
      Loaded,
      // Turned off with [DisableFeature].
      Disabled,
      // Its own patches did not apply.  Rolled back.
      Failed,
      // Something it depends on is not available.
      Skipped,
      // Has patches, Init() methods or commands, but no [Feature] attribute.
      // Never applied, because we would rather leave something switched off
      // than let it patch by accident.
      Unassigned,
    }

    public class Result {
      public string Name;
      public Status Status;
      // Why, for everything except Loaded.
      public string Reason;
    }

    // The outcome of the last ApplyAll(), in a form a UI can render.  The
    // on-screen main menu report reads this.
    public static readonly List<Result> Results = new List<Result>();

    // Types whose feature loaded, used to gate Init() and command
    // registration.
    private static readonly HashSet<Type> LoadedTypes = new HashSet<Type>();

    // What the registry knows about one feature, gathered from every type that
    // declares it.
    private class FeatureInfo {
      public string Name;
      public List<Type> Types = new List<Type>();
      public HashSet<string> DependsOn = new HashSet<string>();
      public bool Disabled = false;
      public string DisabledReason = null;
    }

    // Finds an attribute on a type or on any type enclosing it, so that nested
    // types inherit it.
    private static T FindAttribute<T>(Type type) where T : Attribute {
      for (var t = type; t != null; t = t.DeclaringType) {
        var attributes = t.GetCustomAttributes(typeof(T), false);
        if (attributes.Length > 0) {
          return (T)attributes[0];
        }
      }
      return null;
    }

    // True if this type carries anything the registry is responsible for
    // applying.  Types that are merely helpers are not interesting here.
    private static bool IsApplicable(Type type) {
      if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0) {
        return true;
      }
      if (type.GetCustomAttributes(typeof(RegisterCommand), false).Length > 0) {
        return true;
      }
      return type
          .GetMethods(BindingFlags.Static | BindingFlags.Public)
          .Any(m => m.GetCustomAttributes(typeof(PokeheimInit), false).Length > 0);
    }

    private static bool IsPatchClass(Type type) {
      return type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0;
    }

    public static bool IsLoaded(Type type) {
      return LoadedTypes.Contains(type);
    }

    // Patches, initializes and registers every feature that can be applied,
    // then logs a report of what happened.
    public static void ApplyAll() {
      Results.Clear();
      LoadedTypes.Clear();

      var features = new Dictionary<string, FeatureInfo>();
      var unassigned = new List<Type>();

      foreach (var type in Assembly.GetExecutingAssembly().GetTypes()) {
        if (!IsApplicable(type)) {
          continue;
        }

        var attribute = FindAttribute<FeatureAttribute>(type);
        if (attribute == null) {
          unassigned.Add(type);
          continue;
        }

        FeatureInfo info;
        if (!features.TryGetValue(attribute.Name, out info)) {
          info = new FeatureInfo { Name = attribute.Name };
          features[attribute.Name] = info;
        }
        info.Types.Add(type);

        // A feature can be declared by more than one type, so take the union
        // of everything they ask for.  Core is implicit, so ignore it if
        // someone names it anyway.  DependsOn is settable, so guard against it
        // having been assigned null at the call site.
        foreach (var dep in attribute.DependsOn ?? new string[0]) {
          if (dep != Core) {
            info.DependsOn.Add(dep);
          }
        }

        var disable = FindAttribute<DisableFeatureAttribute>(type);
        if (disable != null) {
          info.Disabled = true;
          info.DisabledReason = disable.Reason;
        }
      }

      // A feature cannot depend on itself.  Drop it rather than deadlocking,
      // and say so.
      foreach (var info in features.Values) {
        if (info.DependsOn.Remove(info.Name)) {
          Logger.LogWarning(
              $"Feature {info.Name} lists itself as a dependency; ignoring.");
        }
      }

      var loaded = new HashSet<string>();
      var unavailable = new HashSet<string>();

      ApplyCore(features, loaded, unavailable);
      ApplyTheRest(features, loaded, unavailable);

      foreach (var type in unassigned) {
        Results.Add(new Result {
          Name = type.FullName,
          Status = Status.Unassigned,
          Reason = "no [Feature] attribute",
        });
      }

      LogReport();
    }

    // Core is implicitly required by everything, so it goes first and its
    // failure takes the rest with it.
    private static void ApplyCore(
        Dictionary<string, FeatureInfo> features,
        HashSet<string> loaded,
        HashSet<string> unavailable) {
      FeatureInfo core;
      if (!features.TryGetValue(Core, out core)) {
        // Nothing declares Core.  Every feature that waits on a lifecycle hook
        // would hang silently, so refuse to apply anything.
        unavailable.Add(Core);
        Results.Add(new Result {
          Name = Core,
          Status = Status.Skipped,
          Reason = "not built; no type declares it",
        });
        Logger.LogError(
            $"No feature named {Core} was found.  It carries the lifecycle " +
            "hooks the rest of the mod waits on, so nothing will be applied.");
        return;
      }

      if (core.Disabled) {
        unavailable.Add(Core);
        Results.Add(new Result {
          Name = Core,
          Status = Status.Disabled,
          Reason = core.DisabledReason ?? "disabled",
        });
        return;
      }

      if (ApplyFeature(core)) {
        loaded.Add(Core);
      } else {
        unavailable.Add(Core);
      }
    }

    // Applies everything else in rounds: each pass takes the features whose
    // dependencies are all loaded, until no further progress is possible.
    private static void ApplyTheRest(
        Dictionary<string, FeatureInfo> features,
        HashSet<string> loaded,
        HashSet<string> unavailable) {
      var pending = features.Values.Where(f => f.Name != Core).ToList();

      // If Core is not available, nothing else can run.  Report a feature that
      // was turned off deliberately as disabled even so, since that is the more
      // useful thing to know about it.
      if (!loaded.Contains(Core)) {
        foreach (var info in pending) {
          unavailable.Add(info.Name);
          if (info.Disabled) {
            Results.Add(new Result {
              Name = info.Name,
              Status = Status.Disabled,
              Reason = info.DisabledReason ?? "disabled",
            });
          } else {
            Results.Add(new Result {
              Name = info.Name,
              Status = Status.Skipped,
              Reason = $"{Core} is not available",
            });
          }
        }
        return;
      }

      while (pending.Count > 0) {
        var progressed = false;
        var stillPending = new List<FeatureInfo>();

        foreach (var info in pending) {
          if (info.Disabled) {
            unavailable.Add(info.Name);
            Results.Add(new Result {
              Name = info.Name,
              Status = Status.Disabled,
              Reason = info.DisabledReason ?? "disabled",
            });
            progressed = true;
            continue;
          }

          // A dependency nothing declares is most likely a feature whose
          // source is not in the Compile list, which is how staging works.
          var missing = info.DependsOn
              .Where(d => !features.ContainsKey(d))
              .ToList();
          var blocked = info.DependsOn
              .Where(d => unavailable.Contains(d))
              .ToList();

          if (missing.Count > 0 || blocked.Count > 0) {
            var reasons = new List<string>();
            if (missing.Count > 0) {
              reasons.Add($"not built: {string.Join(", ", missing)}");
            }
            if (blocked.Count > 0) {
              reasons.Add($"unavailable: {string.Join(", ", blocked)}");
            }
            unavailable.Add(info.Name);
            Results.Add(new Result {
              Name = info.Name,
              Status = Status.Skipped,
              Reason = string.Join("; ", reasons),
            });
            progressed = true;
            continue;
          }

          if (info.DependsOn.All(d => loaded.Contains(d))) {
            if (ApplyFeature(info)) {
              loaded.Add(info.Name);
            } else {
              unavailable.Add(info.Name);
            }
            progressed = true;
            continue;
          }

          // Some dependency is still pending, so try again next round.
          stillPending.Add(info);
        }

        pending = stillPending;

        if (!progressed) {
          // Nothing moved, so what is left depends on itself in a circle.
          foreach (var info in pending) {
            unavailable.Add(info.Name);
            Results.Add(new Result {
              Name = info.Name,
              Status = Status.Skipped,
              Reason =
                  "dependency cycle among: " +
                  string.Join(", ", pending.Select(f => f.Name).OrderBy(n => n)),
            });
          }
          break;
        }
      }
    }

    // Applies one feature's patches under its own Harmony instance.  Returns
    // false if they did not apply, having rolled back whatever did.
    private static bool ApplyFeature(FeatureInfo info) {
      // One Harmony instance per feature, so that UnpatchSelf() below rolls
      // back this feature and nothing else.
      var harmony = new Harmony($"{PokeheimMod.PluginName}.{info.Name}");

      try {
        foreach (var type in info.Types) {
          if (IsPatchClass(type)) {
            harmony.CreateClassProcessor(type).Patch();
          }
        }
      } catch (Exception ex) {
        // Roll back any patches this feature already applied, so we are not
        // left running half of it.
        try {
          harmony.UnpatchSelf();
        } catch (Exception unpatchEx) {
          Logger.LogError(
              $"Failed to roll back feature {info.Name} after an error: " +
              $"{unpatchEx}");
        }
        Results.Add(new Result {
          Name = info.Name,
          Status = Status.Failed,
          Reason = ex.Message,
        });
        return false;
      }

      // Patches are in place, so this feature's initializers and commands may
      // run.  A feature whose patches failed gets neither, because a half
      // alive feature is worse than a cleanly disabled one.
      foreach (var type in info.Types) {
        LoadedTypes.Add(type);
      }

      Results.Add(new Result { Name = info.Name, Status = Status.Loaded });
      return true;
    }

    private static void LogReport() {
      var loaded = Results.Where(r => r.Status == Status.Loaded).ToList();
      var disabled = Results.Where(r => r.Status == Status.Disabled).ToList();
      var skipped = Results.Where(r => r.Status == Status.Skipped).ToList();
      var failed = Results.Where(r => r.Status == Status.Failed).ToList();
      var unassigned = Results.Where(r => r.Status == Status.Unassigned).ToList();

      Logger.LogInfo(
          $"Pokeheim features: {loaded.Count} loaded, {failed.Count} failed, " +
          $"{skipped.Count} skipped, {disabled.Count} disabled.");

      foreach (var result in loaded) {
        Logger.LogInfo($"  [loaded]   {result.Name}");
      }
      foreach (var result in disabled) {
        Logger.LogInfo($"  [disabled] {result.Name}: {result.Reason}");
      }

      // Anything below this point is a problem worth surfacing above Info, so
      // that it is visible without hunting through the log.
      foreach (var result in skipped) {
        Logger.LogWarning($"  [skipped]  {result.Name}: {result.Reason}");
      }
      foreach (var result in failed) {
        Logger.LogError($"  [FAILED]   {result.Name}: {result.Reason}");
      }
      foreach (var result in unassigned) {
        Logger.LogWarning(
            $"  [no feature] {result.Name} was skipped because it has no " +
            "[Feature] attribute.");
      }
    }
  }
}
