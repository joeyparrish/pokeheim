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

using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

using Logger = Jotunn.Logger;

[assembly: AssemblyTitle("Pokeheim")]
[assembly: AssemblyProduct("Pokeheim")]
[assembly: AssemblyCopyright("Copyright © 2021-2026 Joey Parrish")]
[assembly: AssemblyVersion(Pokeheim.ModVersion.String + ".0")]

namespace Pokeheim {
#if DEBUG
  // This is generated at build time for releases.
  public static class ModVersion {
    public const string String = "0.0.1";
  }
#endif

  [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
  // We are incompatible with AllTameable.
  [BepInIncompatibility("meldurson.valheim.AllTameable")]
  // Hard dep on Jotunn, which we use everywhere.
  [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
  // Require everyone playing together to use the same version of this mod.
  [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
  public class PokeheimMod : BaseUnityPlugin {
    // BepInEx' plugin metadata
    public const string PluginGUID = "com.pokeheim";
    public const string PluginName = "Pokeheim";
    public const string PluginVersion = ModVersion.String;

    public void Awake() {
      // Patches, initializes and registers one feature at a time, so that a
      // game update which breaks one feature disables only that feature.
      Features.ApplyAll();

      PokeheimInit.InitAll();
      RegisterCommand.RegisterAll();
    }
  }

  // Attach this attribute to static Init() methods of various parts of the mod.
  // These will be invoked once from the plugin's Awake() method.
  [AttributeUsage(AttributeTargets.Method)]
  public class PokeheimInit : Attribute {
    // Find all methods marked with this attribute.
    private static List<MethodInfo> GetMethods() {
      var assembly = Assembly.GetExecutingAssembly();
      var allTypes = assembly.GetTypes();

      var initMethods = new List<MethodInfo>();
      foreach (var type in allTypes) {
        // Skip anything whose feature was disabled or whose patches failed.
        // Initializing half of a feature is worse than not running it at all.
        if (!Features.IsLoaded(type)) {
          continue;
        }

        var publicStaticMethods =
            type.GetMethods(BindingFlags.Static | BindingFlags.Public);

        foreach (var method in publicStaticMethods) {
          var initAttributes =
              method.GetCustomAttributes(typeof(PokeheimInit), false);
          if (initAttributes.Length > 0) {
            initMethods.Add(method);
          }
        }
      }
      return initMethods;
    }

    // Initialize every part of the mod.
    public static void InitAll() {
      foreach (var method in GetMethods()) {
        try {
          method.Invoke(obj: null, parameters: null);
        } catch (Exception ex) {
          var name = $"{method.DeclaringType?.Name}.{method.Name}";
          Logger.LogError($"Exception initializing {name}: {ex}");
        }
      }
    }
  }

  // Attach this attribute to ConsoleCommand subclasses to register them
  // automatically from the plugin's Awake() method.
  [AttributeUsage(AttributeTargets.Class)]
  public class RegisterCommand : Attribute {
    // Find constructors of all types marked with this attribute.
    private static List<ConstructorInfo> GetConstructors() {
      var assembly = Assembly.GetExecutingAssembly();
      var allTypes = assembly.GetTypes();

      var constructors = new List<ConstructorInfo>();
      foreach (var type in allTypes) {
        // You cannot have a debug console command for a feature that was
        // skipped, so gate registration the same way Init() is gated.
        if (!Features.IsLoaded(type)) {
          continue;
        }

        var registerAttributes =
            type.GetCustomAttributes(typeof(RegisterCommand), false);
        if (registerAttributes.Length > 0) {
          // Get the zero-argument constructor for this type.
          var constructorInfo = type.GetConstructor(new Type[]{});
          if (constructorInfo == null) {
            Logger.LogError($"Can't find constructor for command {type.Name}");
          } else {
            constructors.Add(constructorInfo);
          }
        }
      }
      return constructors;
    }

    // Register all commands marked with this attribute.
    public static void RegisterAll() {
      foreach (var constructor in GetConstructors()) {
        try {
          var constructedObject = constructor.Invoke(parameters: null);
          ConsoleCommand commandObject = (ConsoleCommand)constructedObject;
          CommandManager.Instance.AddConsoleCommand(commandObject);
        } catch (Exception ex) {
          var name = constructor.DeclaringType?.Name;
          Logger.LogError($"Exception registering command {name}: {ex}");
        }
      }
    }
  }  // public class RegisterCommand()
}
