# Dresser Armoire Helper

A Dalamud plugin prototype for Final Fantasy XIV that scans your glamour dresser and lists items that are eligible for armoire storage.

This plugin is based on the data-reading idea from `ffxiv-dresser-analyze`, but runs inside Dalamud instead of a separate desktop program.

## Download

The ready-to-test files are committed in [`release/`](./release):

- [`DresserArmoirePlugin.dll`](./release/DresserArmoirePlugin.dll)
- [`DresserArmoirePlugin.json`](./release/DresserArmoirePlugin.json)
- [`latest.zip`](./release/latest.zip)

For local Dalamud testing, download the DLL and JSON from `release/` into the same folder, then load:

```text
DresserArmoirePlugin.dll
```

## Install For Testing

1. Open XIVLauncher and start the game with Dalamud enabled.
2. In game, run `/xlplugins`.
3. Open Dalamud plugin installer settings.
4. Add the local plugin DLL path, for example:

```text
C:\path\to\DresserArmoirePlugin.dll
```

5. Enable the plugin.
6. Run:

```text
/darmoire
```

## How To Use

1. Open your glamour dresser in game.
2. Run `/darmoire`.
3. Click `Scan glamour dresser`.
4. Review the listed candidates.

The plugin currently lists glamour dresser items that appear in Lumina's `Cabinet` sheet, which means they should be armoire-eligible according to game data.

## Current Status

Implemented:

- Dalamud plugin shell.
- `/darmoire` command.
- Glamour dresser memory scan.
- Armoire eligibility check using Lumina `Cabinet`.
- Candidate list UI.
- Options to skip dyed items and HQ items.

Not implemented yet:

- Automatically moving items into the armoire.

The move button is intentionally disabled. Moving items changes game state and can discard dye information, so the executor should be wired only after verifying the current glamour dresser and armoire UI callback flow in game.

## Build

This project targets `Dalamud.NET.Sdk/15.0.0` and needs a .NET 10 SDK.

```powershell
dotnet build .\DresserArmoirePlugin.sln -c Release
```

The build output is:

```text
bin\Release\DresserArmoirePlugin.dll
bin\Release\DresserArmoirePlugin.json
bin\Release\DresserArmoirePlugin\latest.zip
```

## Roadmap

- Verify the current game version's glamour dresser and armoire addon callbacks.
- Add a confirmation-based one-item-at-a-time move executor.
- Re-scan after every move and stop immediately on mismatch.
- Keep dyed items blocked unless explicitly enabled by the user.
