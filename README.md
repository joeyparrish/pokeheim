# Pokéheim - A Valheim Mod by Joey Parrish

![Pokeheim Logo](Pokeheim/Assets/Logo.png)

Source: https://github.com/joeyparrish/Pokeheim

> Long ago, the Allfather Odin created the monsters.  No one knows why he did
> this.  Some say he was probably drunk.  Others say he was **surely** drunk.
> In any case, he eventually crammed them all onto a weird, flat planet and
> lost the keys.
>
> For centuries, he actually forgot all about this planet.  The monsters
> enjoyed their world, free from the gods.
>
> When Odin finally remembered the whole monster thing, he decided that
> something had to be done to catch 'em...  all of 'em.  But Odin's always been
> more of an "idea man" so he sent Professor Raven to kidnap you and make
> **you** do it.
>
> He says you "gotta"...
>
> **_WELCOME TO POKEHEIM!_**


## About Pokéheim

Pokéheim is a completely different experience than Valheim.  You don't build
houses, you don't progress through technological stages, and your weapons and
armor don't matter.  You build Pokéballs from rocks and use them to capture
monsters.  Your monsters will then fight other monsters for you, until you've
amassed an army big enough to take on the bosses.

We recommend starting with a fresh character and world.  If you don't, please
make a backup of anything you care about.  It will probably be fine, but better
safe than sorry.


### Dependencies

 - MountUp, v3.2.9
   - We borrow the generic saddle prefab from this mod, and disable the rest.
     We have our own mounting, saddle placement, and riding system in Pokéheim.
 - Jötunn, v2.6.7+
   - Framework on which Pokéheim is built.
 - BepInEx, v5.4+


### Incompatibilities

 - [AllTameable](https://www.nexusmods.com/valheim/mods/478)
   - Pokéheim has its own way of making monsters Tameable (by capturing them),
     so you should not use it with the AllTameable mod.

### Multiplayer

The server and all players must be using the mod, and everyone must use the
same version of the mod.


## Installation

### Using R2Modman

> :pencil: **TODO**: Write R2Modman instructions


### Using Thunderstore Mod Manager

> :pencil: **TODO**: Write Thunderstore Mod Manager instructions


### Manual Installation

1. Download:

 - BepInEx: https://valheim.thunderstore.io/package/download/denikson/BepInExPack_Valheim/5.4.1900/
 - Jötunn: https://github.com/Valheim-Modding/Jotunn/releases/tag/v2.6.7
 - MountUp: https://valheim.thunderstore.io/package/download/Oran1/Mountup/3.2.9/
 - Pokéheim: :pencil: **TODO**: first release!

2. Unpack the BepInEx zip into some temporary location.
3. Move the `BepInExPack_Valheim` folder into the Valheim install folder.
4. Make the file `run_bepinex.sh` executable:
   ```sh
   chmod 755 /path/to/valheim/BepInExPack_Valheim/run_bepinex.sh
   ```
4. Move `Jotunn.dll` to the `BepInEx/plugins/` folder inside the Valheim
   install folder.
5. Unpack the MountUp zip into the `BepInEx/plugins/` folder.
6. Unpack the Pokéheim zip into the `BepInEx/plugins/` folder.
7. If not using Steam, run `run_bepinex.sh` to launch Pokéheim.
8. If using Steam:

   1. Open the game's properties on Steam:
      ![Open game properties on Steam by right-clicking the game name](screenshots/steam_props.png)
   2. Next, click Set launch options button which will open a new window:
      ![Click Set launch options to set launch options](screenshots/steam_launch_opts.png)
   3. Now, change the launch options to:
      ```sh
      ./run_bepinex.sh %command%
      ```


## Translate

Help translate Pokeheim into your language!

1. Fork the repository: https://github.com/joeyparrish/pokeheim/fork
2. Find the folder for your language, or add a new one:
   https://github.com/joeyparrish/pokeheim/blob/main/Pokeheim/Assets/Translations/
3. Check the status of missing translations with this tool:

   ```sh
   ./scripts/missing-translations.py
   ```

   Or see results for a single language with something like:

   ```sh
   ./scripts/missing-translations.py German
   ```
4. Edit/add JSON files in your folder using the English version as a template:
   https://github.com/joeyparrish/pokeheim/blob/main/Pokeheim/Assets/Translations/English/
5. Add yourself to the list of Translators in `Pokeheim/Credits.cs`
6. Commit those changes and send a pull request:
   https://github.com/joeyparrish/pokeheim/compare


### Adding a new language

To add a new language, just create a folder in `Pokeheim/Assets/Translations/`.
The name of the folder should be the language "key", which is generally the
name of the language in English.

These are the language keys Valheim already knows (as of June 2022):

| key | language |
| ===== | ===== |
| abenaki | Alnôbaôdwawôgan |
| bulgarian | български |
| chinese | 中文 |
| croatian | Croatian |
| czech | Čeština |
| danish | Dansk |
| dutch | Nederlands |
| english | English |
| estonian | Estonian |
| finnish | Suomi |
| french | Français |
| georgian | ქართული |
| german | Deutsch |
| greek | Ελληνικά |
| hindi | हिन्दी |
| hungarian | Magyar |
| icelandic | íslenska |
| italian | Italiano |
| japanese | 日本語 |
| korean | 한국어 |
| latvian | Latviski |
| lithuanian | Lietuvių kalba |
| macedonian | македонски |
| norwegian | Norsk |
| polish | Polski |
| portuguese_brazilian | Português (Brasil) |
| portuguese_european | Português europeu |
| romanian | Romanian |
| russian | Русский |
| serbian | Serbian |
| slovak | Slovenčina |
| spanish | Español |
| swedish | Svenska |
| thai | ไทย |
| turkish | Türkçe |
| ukrainian | українська |

For a complete and up-to-date list, search for `language_` in
https://valheim-modding.github.io/Jotunn/data/localization/translations/English.html

To add a language that Valheim doesn't know yet, you need to define a
localization for name of the language itself (in the English folder).  The key
would be `language_` plus the name of the folder in lowercase.  The value would
be the name of the language in that language itself.

For example, if you add a folder called `Pig_Latin`, you would create a
translation in `English/language.json` with something like:

```json
{
  "language_pig_latin": "Igpay Atinlay"
}
```


## Credits

Pokéheim was created by [Joey Parrish](https://joeyparrish.github.io/).

The authors and contributors of Pokéheim have no affiliation with the Pokémon
Company or Niantic.  This is both a parody and tribute.

### Jötunn

Pokéheim is made possible by [Jötunn: The Valheim Library](https://valheim-modding.github.io/Jotunn/).
Many thanks to the authors of Jötunn for their wonderful library and their
support on Discord!

### Pokédex Icon

[Pokédex icon](Pokeheim/Assets/Pokedex icon.png) made by
[Roundicons Freebies](https://www.flaticon.com/authors/roundicons-freebies)
from [FlatIcon](https://www.flaticon.com/)

### "Borrowed" Translations

Translations of things like "Pokédex" and "Pokéball" were extracted from the
[Pokémon Go](https://pokemongolive.com/) [APK](https://www.apkmirror.com/apk/niantic-inc/pokemon-go/).
