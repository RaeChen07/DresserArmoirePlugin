# Dresser Armoire Helper

A Dalamud plugin prototype for Final Fantasy XIV that scans your glamour dresser, lists items that are eligible for armoire storage, and can restore those candidates back to your inventory one at a time.

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

1. Open your glamour dresser in game.
2. Run `/darmoire`.
3. Click `Scan glamour dresser`.
4. Review the listed candidates.
5. Click `Auto-restore candidates to inventory` to begin restoring eligible items from the glamour dresser to your player inventory.

The plugin currently lists glamour dresser items that appear in Lumina's `Cabinet` sheet, which means they should be armoire-eligible according to game data.

The auto-restore loop stops when:

- no armoire-eligible glamour dresser candidates remain,
- your player inventory is full,
- the glamour dresser is closed/unavailable, or
- you click `Stop auto-restore`.

## Current Status

Implemented:

- Dalamud plugin shell.
- `/darmoire` command.
- Glamour dresser memory scan.
- Armoire eligibility check using Lumina `Cabinet`.
- Candidate list UI.
- Options to skip dyed items and HQ items.
- One-item-at-a-time restore loop from glamour dresser to player inventory.

Not implemented yet:

- Automatically depositing restored inventory items into the armoire.

The current automation follows the safer first half of the flow: it restores eligible glamour dresser items into your inventory. Depositing those inventory items into the armoire still needs the armoire UI callback flow to be verified in game.

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

- Verify the current game version's armoire addon callbacks.
- Add a confirmation-based one-item-at-a-time armoire deposit executor.
- Re-scan after every restore/deposit and stop immediately on mismatch.
- Keep dyed items blocked unless explicitly enabled by the user.
