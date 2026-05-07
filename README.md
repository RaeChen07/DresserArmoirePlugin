# Dresser Armoire Helper

Dalamud plugin prototype for finding glamour dresser items that can be stored in the armoire.

## Current status

- `/darmoire` opens the plugin window.
- `Scan glamour dresser` reads the currently loaded glamour dresser data.
- The plugin compares dresser item ids with Lumina's `Cabinet` sheet.
- Dyed items are skipped by default because storing may discard dye state.
- The move button is intentionally disabled until the armoire UI callback flow is verified in-game.

## Build

This project targets `Dalamud.NET.Sdk/15.0.0`, matching the current dev install under XIVLauncher.
It needs a .NET 10 SDK:

```powershell
C:\Users\ChenF\Documents\Codex\2026-05-06\files-mentioned-by-the-user-ffxiv\.dotnet10\dotnet.exe build .\DresserArmoirePlugin.sln -c Release
```

Build output:

```text
bin\Release\DresserArmoirePlugin.dll
bin\Release\DresserArmoirePlugin.json
bin\Release\DresserArmoirePlugin\latest.zip
```

## Next implementation step

The scanner is deliberately separated from the executor. To add actual moving, implement an executor that:

1. Requires the glamour dresser/armoire UI to be open.
2. Moves only one visible, selected candidate at a time.
3. Re-scans after every move and stops on any mismatch.
4. Never moves dyed items unless the user explicitly enables it.
5. Uses Dalamud addon lifecycle callbacks for the current game version instead of hard-coded blind clicking.
