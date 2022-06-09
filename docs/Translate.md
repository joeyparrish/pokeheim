# Pokéheim Translation

![Pokéheim Logo](https://github.com/joeyparrish/pokeheim/raw/main/Pokeheim/Assets/Logo.png)

Help translate Pokéheim into your language!

These instructions assume you are least familiar with `git`.


## Contributing translations

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


## Adding a new language

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
