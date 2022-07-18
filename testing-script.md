# Manual Testing Script

## 1. Intro

1. Confirm Pokeheim logo on main menu
1. Confirm custom menu music
1. Enter world with new character
1. Confirm that Pokeheim tips are shown on loading screen
1. Check that custom Pokeheim intro plays
1. After intro, check that no Valkyrie animation plays
1. Character should have a torch and rag clothing


## 2. Wardrobe

1. Install CustomWigs mod
1. Enter world with new character
1. Check that wardrobe exists at starting location
1. Check wardrobe height
1. Check that wardrobe faces the center of the circle
1. Go to wardrobe
1. Check hover message says "Wardrobe" and "Open"
1. Press "E" to open
1. Check that player faces camera
1. Check that outfit dialog has correct defaults (current clothing)
1. Check that CustomWigs items appear in helmets list
1. Check that "CAPE TEST" doesn't appear in the capes list
1. Check that DLC doesn't appear in the list (helmets, capes) in a release build
1. Change each piece to something non-empty
1. Check that changes take effect immediately
1. Check that no "new item" popup occurs for these changes
1. Click "x" to close dialog
1. Check that old clothing was removed from inventory
1. Press "E" to open again
1. Check that the dialog defaults to current clothing still
1. Press escape to close the dialog
1. Check that game menu does not show
1. Press F5 to open console
1. Type "dressup"
1. Check that wardrobe dialog appears
1. Press F5 to close console
1. Press escape to close dialog
1. Press F5 to open console
1. Type "devcommands" to enable dev commands if not enabled
1. Type "tod 0" to force night time
1. Type "dressup"
1. Check that player now wields a torch for visibility
1. Press escape to close dialog
1. Press "TAB" to open inventory
1. Check that style icon appears next to inventory grid
1. Check style icon mouseover text
1. Click style icon
1. Check that wardrobe dialog appears
1. Press escape to close dialog


## 3. Capturing

1. Spawn pokeballs
2. Spawn greyling
3. Catch greyling
4. Release greyling
5. Check mouseover text, should show you as the owner, can pet
6. Log out & back in
7. Check the greyling is still owned by you, can still pet
8. Recall greyling
9. Release greyling
10. Check the greyling is still owned by you, can still pet
11. Recall greyling
12. Check stats in ball description
13. Spawn and catch a level 2 greydwarf
14. Release level 2 greydwarf, confirm correct coloring
15. Spawn a "pickaxeiron"
16. Dig a small pit (2-3m deep, 2m x 4m wide) to force monsters together
17. Spawn a greydwarf in the pit and faint it
18. Release a greyling in the pit, rename it to "Fainted" and faint it
19. Spawn another greydwarf in the pit
20. Release a greyling in the pit and rename it "Awake"
21. Confirm that ball catches wild fainted greydwarf first ("Gotcha!")
22. Confirm that ball catches tame fainted greyling next ("Fainted, return!")
23. Confirm that ball catches wild awake greyling next ("Gotcha!")
24. Confirm that ball catches tame awake greyling next ("Awake, return!")
25. Open settings
26. Confirm that "Recall Monsters" appears in key settings (default "R")


## 4. Loyalty

1. Release greyling
2. Run away
3. Check that greyling follows you
4. Log out & back in
5. Run away
6. Check that greyling follows you
7. Spawn wild greyling
8. Check the they fight each other
9. Get rid of wild greyling
10. Release captured greyling
11. Spawn Eikthyr
12. Freeze Eikthyr to ensure it doesn't kill the greyling too quickly
13. Check that greyling fights Eikthyr (no monster natively fights a boss)
14. Unfreeze Eikthyr
15. Check that Eikthyr fights greyling


## 5. Renaming

1. Have at least two greylings
2. Release greyling
3. Check mouseover text, should allow petting and renaming
4. Rename greyling to "Foo"
5. Recall greyling
6. Check that the ball is separate name shows "Foo"
7. Check stats in ball description, should show "Name: Foo"
8. Release Foo
9. Check mouseover text, should show "Foo"
10. Pet Foo, message should be "Foo loves you"
11. Log out & back in
12. Check mouseover text, should show "Foo"
13. Rename to "greyling" (all lowercase)
14. Check mouseover text, should show "Greyling" (capital)
15. Recall greyling
16. Check that the ball stacked with the other greylings
17. Change language to German
18. Quit & restart
19. Release greyling
20. Check mouseover text, should show "Gräuling"
21. Rename greyling to "Foo"
22. Change language to English
23. Quit & restart
24. Check mouseover text, should show "Foo"
25. Rename to ""
26. Check mouseover text, should show "Greyling"


## 6. PVP

1. Release greyling at starting temple
2. Check that PVP is off
3. Hit greyling with torch, should not hit
4. Punch greyling, should still hit because fists are special
5. Punch greyling to death, should actually die
6. Wait for cooldown, enable PVP
7. Hit greyling with torch, should hit this time
8. Release another two greylings
9. Log out
10. Change players and log back in
11. Check that PVP is off
12. Check that new player can pet/rename greyling
13. Run away
14. Check that greyling does not follow, even with PVP off
15. Enable PVP
16. Check that greyling attacks
17. Disable PVP
18. Check that greyling stops attacking
19. Enable PVP
20. Release own greyling
21. Check that greylings attack each other
22. Disable PVP
23. Check that greylings stop attacking each other
24. Enable PVP
25. Try to catch one of the other player's greylings, should not be trivial
26. Check that capture message was "Gotcha!" and not "return!"
27. Disable PVP
28. Recall all, check that only your own greyling was recalled, with the
    message "Greyling, return!" and not "Everyone, return!"
29. Try to catch the other player's remaining greyling, should work right away
30. Check that capture message was "Gotcha!" and not "return!"


## 7. Projectiles

1. Spawn pokeballs, confirm coloring
2. Spawn greatballs, confirm coloring
3. Spawn ultraballs, confirm coloring
4. Throw each, confirm coloring and rotation of projectiles
5. Confirm that there's no ooze effect from the ooze bomb they are based on
6. Confirm the "hit" sound plays on impact
7. Spawn greyling
8. Confirm that a direct hit is not needed to capture (2m radius OK)
9. Spawn troll
10. Try to catch troll with pokeball, greatball, and ultraball
11. Read catch rate in debug logs, confirm that ball type has an effect
12. Kill or capture troll because it will be a pain in the ass now


## 8. Berries

1. Spawn greyling
2. Spawn "raspberry"
3. Check that the greyling goes for the berry and eats it
4. Repeat with "blueberries" and "cloudberry"
5. Remove greyling
6. Spawn boar
7. Tame boar
8. Spawn "carrot"
9. Check that the boar does not eat the carrot
10. Spawn "raspberry"
11. Check that the boar eats the raspberry
12. Spawn 50 "raspberry" and pick them up
14. Spawn greyling
15. Throw full stack of 50 "raspberry"
16. Check that greyling sits to eat for a long time and ignores you


## 9. Crafting

1. Use debug command "tutorialreset"
2. Spawn:
  - "stone" x50
  - "wood" x50
  - "flint" x50
  - "feathers" x50
  - "LeatherScraps" x50
  - "TrophyDeer"
  - "raspberry"
  - "blueberries"
  - "mushroomyellow"
3. Craft:
  - pokeball
  - greatball
  - ultraball
  - wood arrows
  - flint arrows
  - stone axe
  - club
  - hammer
  - hoe
  - pickaxe
  - torch
  - bow
  - saddle
4. Verify that you see tutorials for:
  - berries
  - pokeballs
  - hammer
  - pickaxe


## 10. Bosses

1. Enter game at starting location
2. Confirm that the chains on boss stones are not interactable
3. Read Vegvisir
4. Check that next boss is on the map
5. Fly to next boss
6. Confirm no runestone or item stands
7. Start raid at altar
8. Type "catchemall" in dev console
9. Try to start raid again
10. Confirm that raid does not start (already caught)
11. Go to step 3


## 11. Fainting

1. Spawn greyling
2. Spawn weapon ("club" or "SwordIronFire")
3. Hit greyling with weapon until it faints
4. Check that it doesn't die
5. Catch it (should always work)
6. Spawn greyling
7. Type "killall" in dev console
8. Confirm that greyling dies
9. Release greyling
10. Faint it
11. Recall all, check that your fainted greyling does not come back
12. Spawn deathsquito
13. Attack it until it faints (or use "faintall" in dev console)
14. Check that it falls from the sky and lands upside down
15. Spawn blob, wraith, and bonemass
16. Faint them
17. Check that they stop fuming
18. Spawn serpent
19. Faint it
20. Check that its tail stops moving
21. Spawn "dragon" (Moder)
22. Faint it
23. Check that it falls from the sky (no ragdoll)
24. Spawn "hatchling" (Drake)
25. Faint it
26. Check that it falls from the sky (ragdoll)


## 12. Inventory

1. Open inventory
2. Check that it's twice as large as usual (8x8)
3. Spawn `TreasureChest_meadows`
4. Open it
5. Check that the chest inventory GUI is positioned below the player inventory


## 13. Player

1. Spawn troll
2. Get hit by troll
3. Confirm that no damage was taken
4. Remove all armor
5. Fly to the mountains
6. Confirm that the player is not freezing
7. Fly to the swamps
8. Find a sunken crypt
9. Confirm that the player does not need a key to enter
10. Fly into the sky
11. Stop flying and fall to your death
12. Confirm that all items are kept in inventory when respawning


## 14. Pokedex

1. TODO: Pokedex testing


## 15. Riding

1. TODO: Test riding and saddles
1. TODO: Test riding flying things
1. TODO: Test attacking while riding, with example of secondary attack


## 16. Giovanni

1. TODO: Test Giovanni


## 17. Chests

1. TODO: Test chests


## 18. End Game

1. TODO: Test end game


## 19. Languages

1. TODO: Make sure translations are loaded for some non-English language
2. TODO: Make sure untranslated text falls back to English (tips, intro, etc)
