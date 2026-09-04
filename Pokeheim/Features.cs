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
  // Marks a type as belonging to a named feature.  Nested types inherit the
  // feature of the type that encloses them, so marking a feature's top-level
  // static class covers its patches, its Init() methods and its console
  // commands all at once.
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
                  Inherited = false)]
  public class FeatureAttribute : Attribute {
    public string Name { get; private set; }

    public FeatureAttribute(string name) {
      this.Name = name;
    }
  }

  // Applies the mod one feature at a time, so that a game update which breaks
  // one feature disables only that feature instead of the whole mod.
  //
  // Each feature gets its own Harmony instance, which is what makes clean
  // rollback possible: if a feature's third patch throws, its first two are
  // already applied, and UnpatchSelf() on that feature's own instance removes
  // them without touching any other feature.
  public static class Features {
    // Feature names.  Keep these in sync with the Compile list in
    // Pokeheim.csproj and with the sections of testing-script.md.
    public const string Logo = "Logo";
    public const string Music = "Music";
    public const string Version = "Version";
    public const string Intro = "Intro";
    public const string LoadingScreen = "LoadingScreen";

    // The features enabled in the current stage of the revival.  Add to this
    // as each stage brings a feature back.  See
    // docs/superpowers/specs/2026-09-04-pokeheim-revival-design.md.
    private static readonly List<string> EnabledFeatures = new List<string> {
      Logo,
      Music,
    };

    public enum Status {
      // Patched, initialized and registered successfully.
      Loaded,
      // Not enabled in this stage of the revival.
      Disabled,
      // Enabled, but its patches did not apply.  Rolled back.
      Failed,
      // Has patches, Init() methods or commands, but no [Feature] attribute.
      // Never applied, because during the revival we would rather leave
      // something switched off than let it patch by accident.
      Unassigned,
    }

    public class Result {
      public string Name;
      public Status Status;
      // Populated only when Status is Failed.
      public string Error;
    }

    // The outcome of the last ApplyAll(), in a form a UI can render.  The
    // on-screen main menu report reads this.
    public static readonly List<Result> Results = new List<Result>();

    // Types whose feature loaded, used to gate Init() and command
    // registration.
    private static readonly HashSet<Type> LoadedTypes = new HashSet<Type>();

    // Walks outward through enclosing types to find the feature a type belongs
    // to, or null if it has not been assigned to one.
    private static string GetFeatureName(Type type) {
      for (var t = type; t != null; t = t.DeclaringType) {
        var attributes = t.GetCustomAttributes(typeof(FeatureAttribute), false);
        if (attributes.Length > 0) {
          return ((FeatureAttribute)attributes[0]).Name;
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

    // Patches, initializes and registers every enabled feature, then logs a
    // report of what happened.
    public static void ApplyAll() {
      Results.Clear();
      LoadedTypes.Clear();

      var byFeature = new Dictionary<string, List<Type>>();
      var unassigned = new List<Type>();

      foreach (var type in Assembly.GetExecutingAssembly().GetTypes()) {
        if (!IsApplicable(type)) {
          continue;
        }
        var name = GetFeatureName(type);
        if (name == null) {
          unassigned.Add(type);
          continue;
        }
        if (!byFeature.ContainsKey(name)) {
          byFeature[name] = new List<Type>();
        }
        byFeature[name].Add(type);
      }

      foreach (var name in EnabledFeatures) {
        if (!byFeature.ContainsKey(name)) {
          // Enabled but nothing implements it.  Most likely its source file is
          // still commented out of the Compile list.
          Results.Add(new Result {
            Name = name,
            Status = Status.Disabled,
          });
          continue;
        }
        ApplyFeature(name, byFeature[name]);
      }

      foreach (var name in byFeature.Keys) {
        if (!EnabledFeatures.Contains(name)) {
          Results.Add(new Result { Name = name, Status = Status.Disabled });
        }
      }

      foreach (var type in unassigned) {
        Results.Add(new Result {
          Name = type.FullName,
          Status = Status.Unassigned,
        });
      }

      LogReport();
    }

    private static void ApplyFeature(string name, List<Type> types) {
      // One Harmony instance per feature, so that UnpatchSelf() below rolls
      // back this feature and nothing else.
      var harmony = new Harmony($"{PokeheimMod.PluginName}.{name}");

      try {
        foreach (var type in types) {
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
              $"Failed to roll back feature {name} after an error: {unpatchEx}");
        }
        Results.Add(new Result {
          Name = name,
          Status = Status.Failed,
          Error = ex.Message,
        });
        return;
      }

      // Patches are in place, so this feature's initializers and commands may
      // run.  A feature whose patches failed gets neither, because a half
      // alive feature is worse than a cleanly disabled one.
      foreach (var type in types) {
        LoadedTypes.Add(type);
      }

      Results.Add(new Result { Name = name, Status = Status.Loaded });
    }

    private static void LogReport() {
      var loaded = Results.Where(r => r.Status == Status.Loaded).ToList();
      var failed = Results.Where(r => r.Status == Status.Failed).ToList();
      var disabled = Results.Where(r => r.Status == Status.Disabled).ToList();
      var unassigned = Results.Where(r => r.Status == Status.Unassigned).ToList();

      Logger.LogInfo(
          $"Pokeheim features: {loaded.Count} loaded, {failed.Count} failed, " +
          $"{disabled.Count} disabled.");

      foreach (var result in loaded) {
        Logger.LogInfo($"  [loaded]   {result.Name}");
      }
      foreach (var result in disabled) {
        Logger.LogInfo($"  [disabled] {result.Name}");
      }

      // Anything below this point is a problem worth surfacing above Info, so
      // that it is visible without hunting through the log.
      foreach (var result in failed) {
        Logger.LogError($"  [FAILED]   {result.Name}: {result.Error}");
      }
      foreach (var result in unassigned) {
        Logger.LogWarning(
            $"  [no feature] {result.Name} was skipped because it has no " +
            "[Feature] attribute.");
      }
    }
  }
}
