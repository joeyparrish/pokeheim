# Debugging Pokeheim

## BepInEx Settings

By default, BepInEx logs only certain events.  For development and debugging of Pokeheim, you should change some of the default settings in the BepInEx config file.

On Linux, the config file defaults to
`~/.local/share/Steam/steamapps/common/Valheim/BepInEx/config/BepInEx.cfg`

Open this and find the `[Logging.Disk]` section.  In that section, set:

```ini
# Write exceptions to the log file as well as events from the mod.
WriteUnityLog = true

# Write debug logs to the log file as well as info, error, etc.
LogLevels = all
```

## BepInEx Log File

On Linux, the log file defaults to
`~/.local/share/Steam/steamapps/common/Valheim/BepInEx/LogOutput.log`

You can watch this with `tail -f` to see logs as the occur.  With the above
settings, you will see exceptions logged here, too.
