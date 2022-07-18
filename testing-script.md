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

1. Have at least 2 greylings, The Elder, plus several pokeballs
1. Release a greyling
1. Check mouseover text, should show you as the owner, can pet
1. Log out & back in
1. Check the greyling is still owned by you, can still pet
1. Recall greyling
1. Release greyling
1. Check the greyling is still owned by you, can still pet
1. Equip a torch
1. Check that the greyling is not afraid of fire
1. Recall greyling
1. Check stats in ball description
1. Spawn and catch a level 2 greydwarf
1. Release level 2 greydwarf, confirm correct coloring
1. Spawn "pickaxeiron"
1. Dig a small pit (2-3m deep, 2m x 4m wide) to force monsters together
1. Spawn a greydwarf in the pit and faint it
1. Release a greyling in the pit, rename it to "Fainted"
1. Spawn another greydwarf in the pit and wait for it to faint the greyling
1. Release a greyling in the pit and rename it "Awake"
1. Confirm that ball catches wild fainted greydwarf first ("Gotcha!")
1. Confirm that ball catches tame fainted greyling next ("Fainted, return!")
1. Confirm that ball catches wild awake greyling next ("Gotcha!")
1. Confirm that ball catches tame awake greyling next ("Awake, return!")
1. Open settings
1. Confirm that "Recall Monsters" appears in key settings (default "R")
1. Release The Elder
1. Check how many "root" balls you have
1. Spawn Eikthyr
1. Wait for The Elder to spawn roots
1. Recall The Elder
1. Check that the roots disappear without being captured/recalled
1. Log out and log back in
1. Check that all ball items were correctly generated and placed in inventory


## 4. Loyalty

1. Have at least one greyling, one deer, and four trolls captured
1. Release greyling
1. Run away
1. Check that greyling follows you
1. Log out & back in
1. Run away
1. Check that greyling follows you
1. Spawn wild greyling
1. Check that they fight each other
1. Get rid of wild greyling
1. Release captured greyling
1. Spawn Eikthyr
1. Freeze Eikthyr to ensure it doesn't kill the greyling too quickly
1. Check that greyling fights Eikthyr (no monster natively fights a boss)
1. Unfreeze Eikthyr
1. Check that Eikthyr fights greyling
1. Remove Eikthyr
1. Release deer
1. Check that deer follows you
1. Release troll x4
1. Check that the trolls don't crowd you so closely that they push you around
1. Spawn Eikthyr
1. Run far away quickly
1. Check that trolls stay behind to fight, don't follow until Eikthyr fainted


## 5. Renaming

1. Have at least two greylings
1. Check greyling stats in ball description, should show "Name: (none)"
1. Release greyling
1. Check mouseover text, should allow petting and renaming
1. Rename greyling to "Foo"
1. Recall greyling
1. Check that the ball is separate name shows "Foo"
1. Check stats in ball description, should show "Name: Foo"
1. Release Foo
1. Check mouseover text, should show "Foo"
1. Pet Foo, message should be "Foo loves you"
1. Log out & back in
1. Check mouseover text, should show "Foo"
1. Rename to "greyling" (all lowercase)
1. Check mouseover text, should show "Greyling" (capital)
1. Recall greyling
1. Check that the ball stacked with the other greylings
1. Change language to German
1. Quit & restart
1. Release greyling
1. Check mouseover text, should show "Gräuling"
1. Rename greyling to "Foo"
1. Change language to English
1. Quit & restart
1. Check mouseover text, should show "Foo"
1. Rename to ""
1. Check mouseover text, should show "Greyling"
1. Rename to something long, confirm that 20 characters are allowed


## 6. PVP

1. Have at least 3 captured greylings
1. Release greyling
1. Check that PVP is off
1. Hit greyling with torch, should not hit
1. Punch greyling, should still hit because fists are special
1. Punch greyling to death, should actually die
1. Wait for cooldown, enable PVP
1. Release greyling
1. Hit greyling with torch, should hit this time
1. Release another two greylings
1. Log out
1. Change players and log back in
1. Check that PVP is off
1. Check that new player can pet/rename greyling
1. Run away
1. Check that greyling does not follow, even with PVP off
1. Enable PVP
1. Check that greyling attacks
1. Disable PVP
1. Check that greyling stops attacking
1. Enable PVP
1. Release own greyling
1. Check that greylings attack each other
1. Disable PVP
1. Check that greylings stop attacking each other
1. Enable PVP
1. Try to catch one of the other player's greylings, should not be trivial
1. Check that capture message was "Gotcha!" and not "return!"
1. Disable PVP
1. Recall all, check that only your own greyling was recalled, with the
   message "Greyling, return!" and not "Everyone, return!"
1. Try to catch the other player's remaining greyling, should work right away
1. Check that capture message was "Gotcha!" and not "return!"


## 7. Fainting

1. Spawn greyling
1. Spawn weapon ("club" or "SwordIronFire")
1. Hit greyling with weapon until it faints
1. Check that it doesn't die
1. Check that its mouseover text is gone
1. Wait and check that the ragdoll doesn't explode
1. Check that the ragdoll can be pushed around
1. Catch it (should always work)
1. Spawn greyling
1. Type "killall" in dev console
1. Confirm that greyling dies
1. Release greyling
1. Faint it
1. Recall all, check that your fainted greyling does not come back
1. Spawn deathsquito
1. Attack it until it faints (or use "faintall" in dev console)
1. Check that it falls from the sky and lands upside down
1. Spawn skeleton and faint it
1. Check that it stops making noise
1. Spawn blob, wraith, and bonemass and faint them
1. Check that they stop fuming
1. Spawn serpent and faint it
1. Check that its tail stops moving
1. Spawn "dragon" (Moder) and faint it
1. Check that it falls from the sky (no ragdoll)
1. Log out and back in
1. Check that the dragon is still affected by gravity, even when pushed
1. Spawn "hatchling" (Drake) and faint it
1. Check that it falls from the sky (ragdoll)
1. Spawn The Elder (wild)
1. Wait for The Elder to spawn roots
1. Wait for the roots to faint (timed destruction)
1. Capture roots
1. Capture The Elder
1. Release roots
1. Check that the roots don't faint (timed destruction)
1. Recall roots
1. Release The Elder
1. Spawn Eikthyr (wild)
1. Wait for The Elder to spawn roots
1. Check that the roots don't faint (timed destruction)
1. Spawn BonePileSpawner
1. Faint skeletons as they spawn
1. Check that the spawner keeps spawning instead of stopping at 2 skeletons


## 8. Projectiles

1. Spawn pokeballs, confirm coloring
1. Spawn greatballs, confirm coloring
1. Spawn ultraballs, confirm coloring
1. Throw each, confirm coloring and that projectiles rotate
1. Confirm that there's no ooze effect from the ooze bomb they are based on
1. Confirm the "hit" sound plays on impact
1. Confirm consistent accuracy
1. Spawn greyling
1. Confirm that a direct hit is not needed to capture (2m radius OK)
1. Spawn troll
1. Try to catch troll with pokeball, greatball, and ultraball
1. Read catch rate in debug logs, confirm that ball type has an effect
1. Kill or capture troll because it will be a pain in the ass now
1. Fly so deer won't see/hear you
1. Spawn a deer
1. Fly far enough away to be out of its sensory range
1. Throw a rock nearby the deer
1. Check that the deer is startled and runs away


## 9. Berries

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


## 10. Crafting

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


## 11. Bosses

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
