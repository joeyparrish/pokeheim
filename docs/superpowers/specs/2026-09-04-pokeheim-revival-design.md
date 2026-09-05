# Pokéheim Revival: Design

Date: 2026-09-04

## Context

Pokéheim last built and ran against the July 2022 build of Valheim. Its last
commit is from July 2022. Since then Valheim has shipped Mistlands, Ashlands,
and Deep North, plus two structural changes (soft referenceable assets and the
Splatform crossplay layer). The mod no longer builds, and the toolchain it was
built with is gone from the development machine and largely end of life.

This document covers reviving the mod. It does not cover adding Pokéheim
content for the new biomes.

### Reference dumps

`../study/` holds decompiled dumps of the game across several years. The
baseline for all comparison work is `ref.cs.2022`, because that is the build the
existing patches were written against. `ref.cs.2023` is a dump Joey took
intending to do an update that never started, and no patch has ever been
validated against it. Using it as a baseline would misattribute 2022 to 2023
game changes to the current update.

`../study/publicized_assemblies.2022/` holds the publicized assemblies from the
last working build, preserved because the 2022 game DLLs they were generated
from no longer exist on disk.

## Goals

1. Pokéheim builds and loads against current Valheim.
2. Features come back one at a time, each verifiable on its own.
3. Patches survive future game updates better than they survived this one, at a
   complexity cost proportional to the benefit.
4. The manual test plan matches how the work is actually staged.

## Non-goals

1. Hand-authoring Pokéheim support for Mistlands, Ashlands, and Deep North
   creatures. The mod's existing discoverability design should carry new
   monsters with no new code. Saddle positions are the known exception and are
   handled by in-game tooling, not by code.
2. New gameplay features.
3. Refactoring unrelated to getting the mod working or keeping it working.

## Findings that shape the design

### The toolchain question is settled

A spike confirmed that a modern SDK style project builds cleanly. The
combination that works is .NET SDK 10.0.400 installed to a user directory,
`JotunnLib` 2.29.2 referenced as a `PackageReference`, and the two properties
the repo already sets, `VALHEIM_INSTALL` and `ExecutePrebuild`. Jotunn brings
BepInEx and HarmonyX in transitively, so `packages/` and `packages.config` are
deleted outright. Mono, msbuild, and nuget.exe are not needed at all. A clean
build takes about five seconds.

Jotunn's `Paths.props` still imports `Environment.props` and `DoPrebuild.props`
from the solution directory, so the repo's existing configuration convention
survives unchanged.

### Jotunn's publicizer is broken on Linux

`JotunnBuildTask` hardcodes the game data directory as `Valheim_Data` with a
capital V, in two places. On case sensitive filesystems this never matches the
real `valheim_Data`, so publicizing fails, and it fails badly: the task returns
false without logging, so MSBuild reports only the generic MSB4181 error and
never mentions letter case. Jotunn's own `Paths.props` already probes both
spellings, so the props and the build task disagree about supported layouts.

A fix has been sent upstream. Until a fixed release ships, the repo carries a
workaround. The workaround must be a single isolated change with a comment
pointing at the upstream PR, so that removing it later is trivial.

Worth recording, because it caused a false result during the spike: the
publicizer does not skip regeneration when its output directory already exists.
It hash checks and self heals. The stale 2022 assemblies that briefly made a
build appear to succeed were consumed only because `ExecutePrebuild` was unset
and the task never ran at all.

### The damage is smaller than expected

Valheim grew from 443 to 856 types between the 2022 baseline and the May 2026
build, counting the four assemblies the dump script now covers. Of the 125 patch
targets that could be resolved automatically:

| Outcome                            | Count | Share |
| ---------------------------------- | ----: | ----: |
| Clean, name and signature unchanged |  103 |   82% |
| Survived, signature drifted         |   18 |   14% |
| Gone entirely                       |    4 |    3% |

All four missing targets are the same refactor applied repeatedly: Valheim moved
Unity lifecycle methods behind centralized update interfaces. `BaseAI.FixedUpdate`
became `UpdateAI`, driven by a static list of `IUpdateAI`. `Character.FixedUpdate`
and `Tail.LateUpdate` became `CustomFixedUpdate` and `CustomLateUpdate` under
`IMonoUpdater`. `Character.SetupContinousEffect` was renamed to
`SetupContinuousEffect`, fixing a spelling error, and is now static with
different parameters. These are mechanical migrations.

The signature drifts matter more, because they fail later and less obviously.
The ones with the most consequence are `Player.OnSpawned`, which gained a
`spawnValkyrie` flag that may supersede logic the mod hand rolls;
`Player.SetControls`, which gained a dodge parameter and feeds riding;
`Projectile.OnHit`, which gained a surface normal and feeds the core catching
mechanic; `SpawnSystem.Spawn`, which gained level override parameters and feeds
shiny spawning; and `Version.GetVersionString`, which feeds multiplayer version
gating.

The survey has real limits and should not be over trusted. It resolves targets
named through `nameof` and `AccessTools` only, so patches that name their target
implicitly are invisible to it. It compares decompiled C# signatures rather than
IL, so the mod's three transpilers are entirely unassessed and remain the highest
risk items regardless of the table above. It cannot see behavioral change behind
an unchanged signature, and soft referenceable assets are the obvious candidate
there, since the mod's `ZNetScene` and `ObjectDB` patches assume prefabs load
eagerly.

### Failure is currently all or nothing

The plugin applies every patch through a single `PatchAll` call. Harmony throws
when a prefix declares a parameter its target no longer has, so any one of the
eighteen drifted signatures takes down the entire mod rather than one feature.
The four missing targets behave differently: because they are named through
`nameof`, they are compile errors against the new assemblies and will surface
before the mod ever loads.

By contrast the mod's existing `[PokeheimInit]` attribute already does the right
thing, invoking each initializer under its own try and catch and logging failures
individually. The design below extends that established pattern to patching
rather than inventing a new one.

### A dependency has rotted

`Riding.cs` depends on MountUp for exactly one thing, a generic saddle prefab,
and then actively undoes the rest of that mod by removing its saddle items and
clearing its saddle references. The package the build installs from is now marked
deprecated on Thunderstore. Its successor is a near total rewrite by an author
whose other mod Pokéheim declares itself incompatible with. The dependency costs
more than it provides and is removed.

## Phasing

Work splits on the Valheim 1.0 release, scheduled for 2026-09-09. Infrastructure
work does not depend on game APIs and proceeds immediately against the May 2026
build already on disk. All patch rework starts against the 1.0 dump, so that
patches are not written twice.

1. **Stage 0, now.** Build infrastructure and a minimal load.
2. **MusicMod interlude, while waiting for 1.0.** Apply the same build
   modernization to the standalone MusicMod repository. It is a much smaller
   codebase and far easier to test, so it validates the pattern cheaply, and it
   unblocks the eventual music spin out.
3. **Stage 1 onward, after the 1.0 dump.** Re-enable features one at a time.

## Stage 0

Stage 0 succeeds when the game launches with the mod installed, the main menu
shows the Pokéheim logo, custom menu music plays, the startup report lists
exactly the features that were meant to load, and the log contains no
exceptions.

Scope:

- Convert to an SDK style project built with `dotnet build`. Delete `packages/`
  and `packages.config`. Keep `Environment.props` and `DoPrebuild.props`.
- Carry the Jotunn Linux workaround, isolated and commented.
- Replace the single `PatchAll` with the feature registry described below.
- Register two features only: the main menu logo, and music.
- Remove the MountUp dependency, its `BepInDependency` attribute, its install
  script, and its CI fetch.
- Rebuild CI on current action versions and the .NET SDK.
- Leave every other source file in the repository, unregistered and excluded
  from compilation.

Music in Stage 0 uses the in-repo `MusicMods.cs` that is already committed and
known to work, not the MusicMod package. The spin out is deferred until MusicMod
itself has been modernized, which keeps Stage 0 to one moving part. The
unfinished spin out currently in the working tree is set aside for later; a copy
of that work is preserved outside the repository.

## Feature registry

The registry exists to serve two needs that turn out to be one need. Staging
requires enabling features one at a time. Robustness requires that a game change
which breaks one feature does not break the rest. Both are satisfied by
attributing patches to named features and applying them independently.

Each patch class is marked with the feature it belongs to. At startup the plugin
groups patch classes by feature and applies each feature's patches as a unit,
under its own error handling, rather than in one undifferentiated pass. A feature
whose patches fail is recorded as failed, with the reason, and the rest continue.

The plugin then logs a report naming every known feature and its outcome: loaded,
disabled, or failed and why. This report is the mechanism by which staging is
verified, and it is also the first thing to read after any future game update.
It converts a silent total failure into a specific, legible diagnosis.

Deliberately excluded for now: user facing configuration toggles. Features are
enabled in code. If someone later wants to turn features off at runtime, the
registry is the place to add it, but building that now would be speculative.

This mirrors `[PokeheimInit]`, which already discovers annotated members by
reflection and isolates their failures. Reusing an established pattern in this
codebase is preferred over introducing a second, differently shaped one.

## Saddle prefab

MountUp is removed as a dependency in Stage 0. Supplying the replacement saddle
is deferred to the riding stage, since nothing before that needs it.

The saddle is derived at runtime from vanilla. Lox riding has been in the base
game since before the mod was written, so the game already ships a working
saddle: a child object carrying a `Sadle` component, which finds its owner
through `GetComponentInParent<Character>()` and registers its RPCs on the
character's `ZNetView`. Cloning that object onto other monsters is the natural
parallel to what the mod already does when it clones the vanilla `SaddleLox`
item, and it keeps the mod free of any redistributed third party asset.

Every field the mod relies on survived the update: `Tameable.m_saddleItem`,
`Tameable.m_saddle`, `m_dropSaddleOnDeath`, and `m_dropSaddleOffset` are all
still present, and `Sadle` itself was not converted to the new update
interfaces. The change is therefore confined to where the prefab comes from. The
surrounding machinery in `Riding.cs`, meaning the scale insulating parent, the
per monster offset and rotation, the rider attach point, and the universal
saddle item, is unaffected.

Two smaller consequences follow. The code that removes MountUp's `SaddleBoar`
and `SaddleWolf` items is deleted, because without MountUp those items never
exist. And the vanilla saddle is modeled to fit a lox, so it will look oversized
on small monsters; whether to add an optional per monster scale alongside the
existing offset and rotation, or to accept the mismatch as fitting the mod's
tone, is decided at the riding stage.

Whether Ashlands added a second rideable creature, and therefore a second and
possibly better proportioned saddle to derive from, could not be determined from
the code dumps, because creature prefabs are data rather than code. It is
answered in game with the mod's existing prefab dumping commands when the riding
stage is reached.

### Reusing MountUp's saddle asset is not an option

MountUp's generic saddle is original art, loaded from an AssetBundle embedded in
its DLL, and not derived from any vanilla asset. Extracting it was considered
and rejected on licensing grounds.

No license grant for MountUp could be found anywhere: the Thunderstore package
the build fetched states none and is only a reupload, the DLL carries nothing
but an unedited Visual Studio template copyright string, and the successor
project's repository has no license file and no source code. The original lives
on Nexus Mods, whose permissions block could not be retrieved automatically and
remains the one place a grant might exist.

Absent a stated license, default copyright applies and all rights are reserved.
Attribution does not help, because attribution satisfies a condition of a
license that requires it and cannot substitute for a grant that was never made.

The distinction that matters is that depending on a mod and redistributing part
of it are different acts. The current design only ever depended on MountUp, with
users installing it themselves and Pokéheim redistributing nothing. Bundling the
prefab would convert that into redistribution, which is the step that needs a
license Pokéheim does not have.

Note also that even an explicit but qualified permission, of the kind commonly
written on Nexus pages, would likely remain incompatible. Pokéheim is
GPL-3.0-or-later, which requires that everything distributed be redistributable
under GPL terms without additional restrictions, so a grant limited to
noncommercial use or conditioned on credit would still not qualify.

The existing per monster saddle metadata, a mount point path plus an offset and
rotation, is unaffected by this change and remains hand authored, supported by
the in game tooling the mod already provides.

## Tooling

The patch triage script written during the spike becomes permanent tooling in
`scripts/`. It takes a baseline dump, a current dump, and the mod source, and
reports which patch targets vanished, which drifted in signature, and which are
unchanged. It is re-run against the 1.0 dump and against every future game
update, and it is the cheapest available answer to "what did this update break".

Its limitations, listed under findings above, are documented alongside it so that
a clean report is not mistaken for a guarantee.

`scripts/dump-valheim.sh` continues to work as written and needs only a
correction if the assemblies of interest change. Note that the game now ships
`SoftReferenceableAssets.dll` and `gui_framework.dll`, which did not exist in
2022 and which the publicizer now handles; whether the dump script should cover
them as well is an open question for the stage that first needs them.

## Test plan

`testing-script.md` is currently 656 lines describing one linear manual pass,
which can only be run once everything works. That does not fit staged bring up.

It is restructured into per feature sections corresponding to the registry. Each
feature's section is revised as that feature is rebuilt, so the test plan becomes
the acceptance criteria for each stage rather than a final gate. This
restructuring is the part that happens as the work proceeds, from Stage 0
onward.

Three further improvements are agreed in principle and added as the work
progresses rather than up front:

- A short smoke test at the front covering load, startup report, logo, music,
  and a clean log. Something runnable in two minutes after any game update.
- Shifting mechanical verification onto the triage script and the startup
  report, so the manual script can shed "did the patch apply" steps and
  concentrate on what only a person can judge.
- Coverage for the new biomes and their two bosses.

## Stage ordering after Stage 0

This order has been validated against the mod's actual internal dependency
graph rather than assumed.

Four files are infrastructure rather than features, and are always compiled:
`Pokeheim.cs` (the entry point and the init and command attributes, with 21
dependents), `Utils.cs` (20 dependents, and itself depending on nothing else in
the mod), `TranspilerSequence.cs` (6 dependents, no dependencies), and
`Features.cs`.

The remaining files form five tiers. Everything in the first tier depends only
on infrastructure: `Berries`, `BossMods`, `ContainerMods`, `Credits`,
`Debugging`, `DressUp`, `Giovanni`, `InventoryMods`, `MonsterWithWeapons`,
`MusicMods`, `SerpentMods` and `Sounds`. Above that sit `MonsterMetadata`,
`OdinMods` and `Riding`; then `Captured` and `Inhabitant`; then `BallItem` and
`Fainting`; and finally `BallProjectile`, `PlayerMods`, `ShinyMods`,
`Suppressipes` and `ProfessorRaven`.

The order is therefore:

1. Debug commands, sounds and monster weapons. `Debugging` has no internal
   dependencies and provides the `dumpbodyparts`, `setmountpoint` and
   `renderanddump` commands that later stages need, so it goes first on
   evidence rather than on intuition.
2. Monster identity: berries, then monster metadata.
3. The capture state: captured monsters, inhabitants, fainting.
4. Balls: the item, then the projectile.
5. Player and inventory: player mods, recipe suppression, shinies, inventory
   and container changes.
6. World and NPCs: Odin, credits, Professor Raven, bosses, serpents, Giovanni.
7. Riding, including the replacement saddle derived from vanilla.
8. Remaining polish: the wardrobe, the version string, the intro and the
   loading screen.

### Riding must be decoupled from capture first

Taken literally the graph puts riding second, ahead of capture and the balls,
because `Captured` depends on `Riding`. That would mean hand tuning saddle
offsets on monsters there is not yet any way to catch.

The coupling is a single line, `Captured.cs` adding a `Riding.Mountable`
component to every captured monster. Before stage 3, that responsibility moves
into `Riding`, which patches the same hook and attaches the component itself.
`Captured` then no longer references `Riding` and the order above holds.
(`ProfessorRaven` also names `Riding.SaddleName`, but that is a constant with no
behavior behind it.)

This is worth doing for a second and more important reason. Compile time
coupling between features defeats the registry's runtime isolation: as written,
a `Riding` feature whose patches failed and were rolled back would still have
`Captured` adding `Riding.Mountable` to everything it captures, so disabling
riding would not actually disable riding. The registry can only isolate features
that are not wired into each other, so each stage should check for this shape as
its feature comes back.

## Risks

The three transpilers are the largest unknown. Nothing in the survey speaks to
them, and IL level changes across three years of game updates are likely. The
existing TODO already proposes replacing the hand rolled transpiler sequence
helper with Harmony's `CodeMatcher`, which would make them easier to keep
working; whether that happens as part of revival or after it is decided when the
first transpiler is reached.

Soft referenceable assets are the second largest unknown, because the change is
behavioral rather than structural and the survey cannot see it. The patches most
likely affected are those touching prefab and object database setup.

Valheim 1.0 may differ from the May 2026 build in ways that invalidate parts of
the survey. This is accepted deliberately: the survey's purpose is to size the
work, and it is cheap to re-run.
