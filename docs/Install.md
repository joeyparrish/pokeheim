# Pokéheim Installation

> :information_source: **NOTE**: For multiplayer games, the server and all
> players must be using the mod, and everyone must use the same version of the
> mod.


## Installing

You may use the mod manager of your choice, or install the mod manually.  It is
available from:
 - [GitHub](https://github.com/joeyparrish/pokeheim/releases)
 - [Thunderstore](https://valheim.thunderstore.io/package/Pokeheim/Pokeheim/)
 - [Nexus Mods](https://www.nexusmods.com/valheim/mods/1919)

Instructions are provided here for both R2ModMan (recommended if you've never
modded before) and for manual installation (if you are a power user or
experienced Linux user).

> :information_source: **NOTE**: As of the release of this mod, Valheim is not
> available for macOS.  If that changes, let us know and we will add
> macOS-specific instructions.


### Using R2Modman

#### Installing R2Modman

1. Download R2Modman by clicking "Manual Download":
   https://valheim.thunderstore.io/package/ebkr/r2modman/
2. Unzip R2Modman to the location of your choice.
3. _(Linux only)_ Find the `.AppImage` and make it executable.
4. Run the `.exe` _(Windows)_ or `.AppImage` _(Linux)_.
5. Search R2Modman for Valheim.
6. _(Linux only)_ Copy the wrapper command.
7. _(Linux only)_ Configure steam to use the wrapper command.
   See [Steam.md](https://github.com/joeyparrish/pokeheim/blob/main/docs/Steam.md)
8. If you don't know what profiles are, just select the default profile.


#### Installing Pokéheim in R2Modman

1. Click "Online" to search for mods.
2. Type "Pokeheim" into the serach box.
3. Click "Pokéheim by JoeyParrish".
4. Click "Download".
5. Click "Download with dependencies".


#### Turning Pokéheim on and off in R2Modman

1. Click the toggle next to Pokéheim to disable or re-enable it.  You may also
   want to disable dependencies like Mountup.
2. Click "Start modded" to run with your enabled mods, or "Start vanilla" to
   run without mods.


### Manual installation

1. Download:

 - BepInEx: https://valheim.thunderstore.io/package/download/denikson/BepInExPack_Valheim/5.4.1901/
 - Jötunn: https://github.com/Valheim-Modding/Jotunn/releases/tag/v2.7.2
 - MountUp: https://valheim.thunderstore.io/package/download/Oran1/Mountup/3.2.9/
 - Pokéheim: https://github.com/joeyparrish/pokeheim/releases

2. Unpack the BepInEx zip into some temporary location.
3. Move the contents of the `BepInExPack_Valheim` folder into the Valheim
   install folder.
4. _(Linux only)_ Make the file `start_game_bepinex.sh` executable:
   ```sh
   chmod 755 ~/.local/share/Steam/steamapps/common/Valheim/start_game_bepinex.sh
   ```
5. Move `Jotunn.dll` to the `BepInEx/plugins/` folder inside the Valheim
   install folder.
6. Unpack the MountUp zip into the `BepInEx/plugins/` folder.
7. Unpack the Pokéheim zip and move the contents of the zip's `plugins` folder
   into the `BepInEx/plugins/` folder.
8. _(If **not using** Steam)_ Run `start_game_bepinex.sh` to launch Pokéheim.
9. _(If **using** Steam)_
   See [Steam.md](https://github.com/joeyparrish/pokeheim/blob/main/docs/Steam.md)


### Manually uninstalling Pokéheim and its dependencies

1. _(Linux only)_ Remove the mods:
  ```sh
  rm ~/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins/Pokeheim.dll
  rm -rf ~/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins/Pokeheim/
  rm ~/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins/MountUp.dll
  rm ~/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins/Jotunn.dll
  ```
2. _(optional)_ Revert Steam configuration:
   See [Steam.md#reverting](https://github.com/joeyparrish/pokeheim/blob/main/docs/Steam.md#reverting)
