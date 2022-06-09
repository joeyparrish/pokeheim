# Extracting Translations from an APK

1. Pick a translation file to update.  For example, `giovanni.json`.
2. Download the APK (complete, not a split APK).
3. Unzip the APK into a new folder.
4. Create a new file called `transfer.json` with a JSON dictionary.
5. Within the APK folder, open `assets/text/i18n_english.json`.
6. Search `i18n_english.json` for the text you want to extract.
7. The string just before the one you want is the "key" for that translation.
   For example, searching "Persian" leads you to:
   ```
   "pokemon_name_0053", "Persian",
   ```
   Here, `pokemon_name_0053` is the key for "Persian".
8. In your `transfer.json` file, add an entry that maps the APK key to the
   corresponding Pokéheim key.  For example:
   ```json
   {
     "pokemon_name_0053": "npc_persian"
   }
   ```
9. Repeat at step 6 until you have mapped all the strings you want.
10. Run the following script to initiate the transfer:
    ```sh
    ./scripts/extract-apk-translations.py \
        --transfer-map transfer.json \
        --output-file giovanni.json \
        --apk-path path/to/apk/dump
    ```
11. Commit changes to the Pokéheim translation files and send a PR.
