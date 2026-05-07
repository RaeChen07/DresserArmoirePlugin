# Dresser Armoire Helper

A Dalamud plugin prototype for Final Fantasy XIV that scans your glamour dresser, lists items that are eligible for armoire storage, restores those candidates to your inventory, and stores eligible inventory items into the armoire.

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

## How To Use

1. Run `/darmoire`.
2. Open your glamour dresser in game.
3. Review the candidate list. It scans once when the glamour dresser opens, and scans again after each restore/store action.
4. Click `Auto restore/store candidates`.

The plugin currently lists glamour dresser items that appear in Lumina's `Cabinet` sheet, which means they should be armoire-eligible according to game data.

The automation loop:

1. Restores one eligible item from the glamour dresser to your inventory.
2. Re-scans.
3. Repeats until no eligible dresser item remains or the inventory is full.
4. Stores eligible inventory items into the armoire one at a time when the armoire is loaded.

The auto-restore loop stops when:

- no armoire-eligible glamour dresser candidates remain,
- your player inventory is full and no eligible inventory item can be stored,
- the glamour dresser is closed/unavailable, or
- the armoire is needed but closed/unavailable,
- you click `Stop auto-restore`.

## Current Status

Implemented:

- Dalamud plugin shell.
- `/darmoire` command.
- Glamour dresser memory scan.
- Candidate refresh when the glamour dresser opens and after each restore/store action.
- Armoire eligibility check using Lumina `Cabinet`.
- Candidate list UI.
- Options to skip dyed items and HQ items.
- One-item-at-a-time restore loop from glamour dresser to player inventory.
- One-item-at-a-time store loop from player inventory to armoire.

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

- Re-scan after every restore/deposit and stop immediately on mismatch.
- Keep dyed items blocked unless explicitly enabled by the user.

## Debugging

Enable `Debug logs` in the plugin window, then open `/xllog` to inspect candidate refreshes, restore/store calls, wait-state transitions, and stop reasons.
