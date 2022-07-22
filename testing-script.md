# Manual Testing Script

## 1. Intro

1. Confirm Pokeheim logo on main menu
1. Confirm custom menu music
1. Enter world with new character
1. Confirm that Pokeheim tips are shown on loading screen
1. Check that custom Pokeheim intro plays
1. After intro, check that no Valkyrie animation plays
1. Character should have a torch and rag clothing
1. Confirm that Professor Raven has appropriate name, hover text, and alert
   messages
1. Confirm that Professor Raven appears with tutorials on welcome, catching,
   and crafting
1. Confirm that the welcome message contains the correct number of bosses


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
1. Spawn "pickaxeiron"
1. Dig a small pit (2-3m deep, 2m x 4m wide) to force monsters together
1. Spawn a greydwarf in the pit and faint it
1. Release a greyling in the pit, rename it to "Fainted"
1. Spawn another greydwarf in the pit and wait for it to faint the greyling
1. Run debug command "freeze Greydwarf" (case sensitive)
1. Release a greyling in the pit and rename it "Awake"
1. Run debug command "freeze Greyling" (case sensitive)
1. Confirm that ball catches wild fainted greydwarf first ("Gotcha!")
1. Confirm that ball catches tame fainted greyling next ("Fainted, return!")
1. Confirm that ball catches wild awake greyling next ("Gotcha!")
1. Confirm that ball catches tame awake greyling next ("Awake, return!")
1. Open settings
1. Confirm that "Recall Monsters" appears in key settings (default "R")
1. Recall anything that might be out
1. Release The Elder
1. Check how many "root" balls you have
1. Spawn Eikthyr
1. Wait for The Elder to spawn roots
1. Recall The Elder
1. Check that the screen didn't say "root, return!"
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
1. Run debug command "nospawns"
1. Run debug command "catchemall"
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
1. Rename to something long, confirm that 20 characters are allowed
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
1. Release boar and rename to "Francis Bacon"
1. Release Moder and rename to "Freddie Mercury"
1. Run debug command "faintall"
1. Catch Francis and Freddie
1. Confirm that their names are still correct
1. Release Francis and Freddie
1. Run debug command "faintall"
1. Log out and back in
1. Catch Francis and Freddie
1. Confirm that their names are still correct


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
1. Spawn "raspberry"
1. Check that the greyling goes for the berry and eats it (within 10s interval)
1. Repeat with "blueberries" and "cloudberry"
1. Remove greyling
1. Spawn boar
1. Tame boar
1. Spawn "carrot"
1. Check that the boar does not eat the carrot
1. Spawn "raspberry"
1. Check that the boar does not eat the raspberry
1. Remove boar
1. Spawn 50 "raspberry" and pick them up
1. Spawn greyling
1. Throw full stack of 50 "raspberry"
1. Check that greyling sits to eat for a long time and ignores you


## 10. Crafting

1. Use debug command "tutorialreset"
1. Spawn:
  - "stone" x50
  - "wood" x50
  - "feathers" x50
  - "LeatherScraps" x50
  - "TrophyDeer"
  - "raspberry"
  - "blueberries"
  - "mushroomyellow"
1. Craft:
  - pokeball
  - greatball
  - ultraball
  - wood arrows
  - stone axe
  - club
  - hammer
  - hoe
  - pickaxe
  - torch
  - bow
  - saddle
1. Verify that you see tutorials for:
  - berries
  - pokeballs
  - hammer
  - pickaxe
1. Verify that you can build camp fire with hammer
1. Verify that you can raise and level ground with hoe
1. Find a pine tree
1. Knock down pine tree
1. Verify that only regular wood spawns
1. Spawn, catch, release, and kill greydwarf
1. Verify that only wood and stone spawn
1. Spawn, catch, release, and kill boar
1. Verify that only leather spawns


## 11. Bosses

1. Enter game at starting location
1. Confirm that the chains on boss stones are not interactable
1. Remove any boss locations from map
1. Loop over all Vegvisir:
  1. Read Vegvisir
  1. Check that next boss is on the map
  1. Use debug command "findboss NAME_OR_INDEX"
  1. Check that other locations for this boss are all farther from the start
  1. Fly to next boss (any location)
  1. Confirm no runestone or item stands
  1. Confirm alter hovertext says "start encounter"
  1. Start raid at altar
  1. Confirm "go!" message shown
  1. Type "catchemall" in dev console
  1. Try to start raid again
  1. Confirm that raid does not start (already caught)
  1. Loop if Vegvisir present (all but last boss)
1. Use debug command "tutorialreset"
1. Release Eikthyr
1. Confirm that Eikthyr does not attack
1. Punch Eikthyr to death
1. Grab boss trophy
1. Verify that Prof. Raven tutorial shows to shame you for it
1. Spawn Eikthyr
1. Spawn "SwordBlackmetal"
1. Verify that sword does no damage to Eikthyr


## 12. Inventory

1. Use debug command "tutorialreset"
1. Open inventory
1. Check that it's twice as large as usual (8x8)
1. Check that Professor Raven comes to tell you about sorting and style
1. Spawn `TreasureChest_meadows`
1. Open the chest
1. Check that the chest inventory GUI is positioned below the player inventory
1. Check that the sort button appears next to inventory grid
1. Check that the mouseover text says "sort"
1. Click sort and verify that it reorders your things, except the first row
  1. Uninhabited balls, getting stronger
  1. Inhabited balls, going up in Pokedex order
  1. Non-equippable items
  1. Equippable, unequipped
  1. Equippable, equipped


## 13. Player

1. Confirm base HP of 50
1. Spawn 500 rocks (10 stacks), confirm you can carry up to 1000 weight
1. Throw away rocks
1. Spawn troll
1. Get hit by troll
1. Confirm that no damage was taken
1. Spawn "SwordBlackmetal"
1. Hit troll with blackmetal sword
1. Verify that very little damage is done
1. Remove all armor
1. Fly to the mountains
1. Confirm that the player is not freezing
1. Fly to the swamps
1. Run "findlocation SunkenCrypt4"
1. Confirm that the player does not need a key to enter
1. Fly into the sky
1. Stop flying and fall to your death
1. Confirm that no tombstone is created
1. Check that all items are kept in inventory when respawning
1. Run and confirm that you don't get tired
1. Swim and confirm that you _do_ get tired swimming
1. Drown and confirm death


## 14. Pokedex

1. Run debug command "resetpokedex"
1. Spawn neck
1. Catch neck
1. Confirm that Professor Raven appears with first catch tutorial
1. Open inventory
1. Check that Pokedex icon appears
1. Check that hovertext for Pokedex says "Pokédex"
1. Check that Pokedex completion and Trainer skill updated and are matched
1. Spawn 50 pokeballs
1. Spawn Eikthyr
1. Throw tons of pokeballs at Eikthyr
1. Confirm that Trainer skill does not increase
1. Open Pokedex
1. Confirm that only neck is filled in
1. Confirm that neck stats are shown
1. Confirm that other entries are silhouetted
1. Go to shoreline
1. Search up shore for accessible, non-deep-sea Serpent


## 15. Riding

1. Spawn "SaddleUniversal"
1. Spawn Lox
1. Freeze Lox
1. Check that you can't saddle the Lox
1. Catch Lox
1. Release Lox
1. Check that you can saddle and ride Lox
1. Test various monsters
  1. For each of these:
    - boar
    - wolf
    - greyling
    - greydwarf
    - fuling shaman (GoblinShaman)
    - greydwarf brute (Greydwarf_Elite)
    - drake (Hatchling)
    - deathsquito
    - bat
    - wraith
  1. Spawn, catch, release, saddle
  1. Spawn wild greyling
  1. Verify that tamed monster does not react
  1. Ride
  1. Run
  1. Primary attack (if supported)
  1. Secondary attack (if supported)
  1. Back stops
  1. Block stops and changes direction
  1. Check that we can zoom way out
  1. Check that the camera is relatively stable
  1. Check that the riding HUD shows the correct monster icon and name
  1. If flying monster:
    1. Test flying up (jump) and down (crouch)
  1. If Moder:
    1. Test landing (crouch near ground) and taking off (jump while walking)
1. Mount a deathsquito
1. Fly up a bit
1. Run debug command "faintall"
1. Verify that you are not trapped on the monster
1. Verify that the saddle came off
1. Catch the deathsquito
1. Verify no additional saddle dropped
1. Test saddle targeting:
  1. For each of these tall monsters:
    - troll
    - The Elder (GD_King)
    - Moder (Dragon)
  1. Saddle the monster
  1. Verify that the saddle can be reached
  1. Fly up if applicable
  1. Dismount
  1. Verify that you don't die on impact on the ground
  1. Mount again
  1. Fly up again if applicable
  1. Call "return"
  1. Verify that you don't die on impact
1. Release two greydwarves
1. Saddle one greydwarf
1. Log out
1. Log back in with another character
1. Spawn SaddleUniversal
1. Enable PVP
1. Check that you can't mount the saddled greydwarf
1. Check that you can't saddle the unsaddled greydwarf
1. Disable PVP
1. Check that you _can_ mount the saddled greydwarf
1. Check that you _can_ saddle the unsaddled greydwarf
1. Use debug command "tutorialreset"
1. Saddle something
1. Jump in the water (yourself)
1. Check that no swimming tutorial is given
1. Get back to land
1. Use debug command "tutorialreset"
1. Jump in the water again
1. Check that a swimming tutorial is given


## 16. Giovanni

1. Run "findlocation Vendor_BlackForest" if Giovanni location unknown
1. Go to Giovanni
1. Verify that his name says "Giovanni"
1. Verify that his Lox is named "Persian"
1. Verify that Giovanni text has been overridden
1. Verify that Giovanni cannot be interacted with, no interact hovertext
1. Verify that Persian is covered in "shadow smoke"


## 17. Chests

1. Spawn `TreasureChest_meadows`
1. Open the chest, verify that only useful stuff appears in it
1. Run "findlocation WoodHouse1", "findlocation WoodHouse2", etc.
1. Find a chest in a house
1. Verify that only useful stuff appears in it
1. Run "findlocation Crypt2", "findlocation Crypt3", "findlocation Crypt4"
1. Find a chest in a skeleton crypt
1. Verify that only useful stuff appears in it
1. Run "findlocation SunkenCrypt4"
1. Find a chest in a swamp crypt
1. Verify that only useful stuff appears in it
1. Run "findlocation MountainCave02"
1. Find a chest in a mountain cave
1. Verify that only useful stuff appears in it
1. Run "findlocation GoblinCamp2"
1. Find a chest in a fuling village
1. Verify that only useful stuff appears in it


## 18. Shiny

1. Throw away all greydwarf balls
1. Run "removedrops"
1. Run "spawn greydwarf 1 1"
1. Run "catchemall"
1. Check that the greydwarf _is not_ marked as shiny in inventory (icon,
   description)
1. Run "spawn greydwarf 1 2"
1. Run "catchemall"
1. Check that the greydwarf _is_ marked as shiny in inventory (icon,
   description)
1. Release shiny greydwarf
1. Confirm that coloring is correct
1. Confirm that HUD shows shiny icon instead of Valheim yellow star icons


## 19. Music

1. Ensure music volume is on
1. Use debug command "skiptime" to fast-forward to night
1. Use debug command "sleep" to fast-forward to morning
1. Confirm that custom "dawn" music plays


## 20. End Game

1. Go to starting location
1. Throw away all boss balls
1. Run debug command "removedrops"
1. Run debug command "resetpokedex"
1. Run debug command "tutorialreset"
1. Spawn Eikthyr
1. Run debug command "catchemall"
1. Check that the "caught a boss" tutorial runs
1. Spawn GD_King, Bonemass, Dragon, and GoblinKing
1. Run debug command "catchemall"
1. Check that the "caught all bosses" tutorial runs
1. Run debug command "spawnall"
1. Run debug command "catchemall"
1. Check that the Pokedex is complete
1. Check that Odin is _not_ at the starting location
1. Check that the "caught em all" tutorial runs
1. Check that Odin spawns after you talk to Professor Raven
1. Check that Odin rotates to face you no matter where you go
1. Talk to Odin
1. Dismiss his message
1. Verify that you die
1. Verify that outro and credits roll
1. Watch full credits, check formatting
1. Verify that Odin is gone when you respawn
1. Run debug command "tutorial caught_em_all"
1. Talk to Raven
1. Verify that Odin reappears
1. Log out
1. Click credits in menu
1. Verify that Pokeheim credits run first
1. Watch Pokeheim part of credits, check formatting
1. Log back in
1. Verify that Odin is still there
1. Verify that Odin still follows you
1. Talk to Odin, don't dismiss text
1. Run away
1. Verify that you die
1. During outro, hit escape
1. Outro should not be dismissed
1. Let credits begin, then hit escape again
1. The credits should be dismissed
1. The player should respawn _right away_ and not after the time credits would
   take


## 21. Languages

1. Choose poorly-translated language, quit, and restart game
1. Ensure that untranslated "loading" tips fallback to English
1. Run debug command "tutorial temple1"
1. Ensure that tutorial falls back to English
1. Ensure that universally-translated text like Pokeball names, Pokedex name,
   style dialog heading show in the target language


## 22. Multiplayer

1. TODO: Multiplayer testing script
