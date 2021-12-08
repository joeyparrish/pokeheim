# How to build Pokéheim

1. Install Valheim:
   https://store.steampowered.com/app/892970/Valheim/

2. Install BepInEx for Valheim:
   https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/

3. Install the `Mount Up` mod to your BepInEx plugins folder:
   https://www.nexusmods.com/valheim/mods/1091

4. Create `Environment.props` as described in Jotunn's docs:
   https://valheim-modding.github.io/Jotunn/guides/guide.html?tabs=tabid-3

5. Set up your dev environment

   a. (Windows only) Set up VS2019 as described in Jotunn's docs:
      https://valheim-modding.github.io/Jotunn/guides/guide.html?tabs=tabid-1

   b. (Linux only) Set up Mono, MSBuild, and Nuget (tested on Ubuntu 20.04 LTS):
      ```sh
      ./scripts/install-linux-tools.sh
      ```

6. Build Pokéheim

   a. (Windows only)
      1. Load `Pokeheim.sln` in VS2019 and build the solution
      2. Copy `Jotunn.dll` to the BepInEx `plugins/` folder
      3. Create `Pokeheim` folder in the `plugins/` folder
      4. Copy `Pokeheim.dll` and `Pokeheim/Assets/` to the `plugins/Pokeheim/`
         folder

   b. (Linux only)
      1. Install/update deps with Nuget:
         ```sh
         nuget restore
         ```

      2. Run the build script:
         ```sh
         ./scripts/build-and-launch.sh
         ```

