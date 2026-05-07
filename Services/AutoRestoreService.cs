using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace DresserArmoirePlugin.Services;

public sealed unsafe class AutoRestoreService : IDisposable
{
    private const int TicksBetweenActions = 45;
    private static readonly InventoryType[] PlayerInventoryTypes =
    [
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
    ];

    private readonly Plugin plugin;
    private int ticksUntilNextAction;

    public bool IsRunning { get; private set; }
    public string Status { get; private set; } = "Idle.";

    public AutoRestoreService(Plugin plugin)
    {
        this.plugin = plugin;
        Plugin.Framework.Update += OnFrameworkUpdate;
    }

    public void Start()
    {
        if (IsRunning)
            return;

        IsRunning = true;
        ticksUntilNextAction = 0;
        Status = "Running.";
        plugin.DebugLog("Auto-restore started. skipDyed={SkipDyed}, skipHq={SkipHq}.", plugin.Configuration.SkipDyedItems, plugin.Configuration.SkipHighQualityItems);
        Plugin.ChatGui.Print("[Dresser Armoire Helper] Auto-restore started.");
    }

    public void Stop(string reason = "Stopped.")
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        Status = reason;
        plugin.DebugLog("Auto-restore stopped: {Reason}", reason);
        Plugin.ChatGui.Print($"[Dresser Armoire Helper] {reason}");
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework framework)
    {
        if (!IsRunning)
            return;

        if (ticksUntilNextAction-- > 0)
            return;

        ticksUntilNextAction = TicksBetweenActions;
        Step();
    }

    private void Step()
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            Stop("Inventory manager is unavailable.");
            return;
        }

        var emptySlots = inventoryManager->GetEmptySlotsInBag();
        plugin.DebugLog("Auto step: emptyBagSlots={EmptySlots}.", emptySlots);

        var dresserCandidate = FindNextDresserCandidate();
        if (dresserCandidate != null)
        {
            plugin.DebugLog(
                "Next dresser candidate: itemId={ItemId}, name={Name}, slot={Slot}, hq={HighQuality}, dyes={Dye1}/{Dye2}.",
                dresserCandidate.ItemId,
                dresserCandidate.Name,
                dresserCandidate.Slot + 1,
                dresserCandidate.HighQuality,
                dresserCandidate.Dye1,
                dresserCandidate.Dye2);
        }

        if (dresserCandidate != null && emptySlots > 0)
        {
            RestoreDresserCandidate(dresserCandidate);
            return;
        }

        var inventoryCandidate = FindNextInventoryCandidate(inventoryManager);
        if (inventoryCandidate != null)
        {
            plugin.DebugLog(
                "Next inventory candidate: itemId={ItemId}, cabinetId={CabinetId}, name={Name}, container={Container}, slot={Slot}, hq={HighQuality}, dyes={Dye1}/{Dye2}.",
                inventoryCandidate.ItemId,
                inventoryCandidate.CabinetId,
                inventoryCandidate.Name,
                inventoryCandidate.InventoryType,
                inventoryCandidate.Slot + 1,
                inventoryCandidate.HighQuality,
                inventoryCandidate.Dye1,
                inventoryCandidate.Dye2);
            StoreInventoryCandidate(inventoryCandidate);
            return;
        }

        plugin.DebugLog("No inventory candidate found.");

        if (dresserCandidate != null && emptySlots == 0)
        {
            Stop("Player inventory is full and no armoire-eligible inventory item could be stored.");
            return;
        }

        Stop("No armoire-eligible glamour dresser or inventory items remain.");
    }

    private void RestoreDresserCandidate(CandidateItem candidate)
    {
        var mirageManager = MirageManager.Instance();
        if (mirageManager == null || !mirageManager->PrismBoxLoaded)
        {
            Stop("Open the glamour dresser before starting auto-restore.");
            return;
        }

        var restored = mirageManager->RestorePrismBoxItem((uint)candidate.Slot);
        plugin.DebugLog("RestorePrismBoxItem returned {Result} for itemId={ItemId}, slot={Slot}.", restored, candidate.ItemId, candidate.Slot + 1);
        if (!restored)
        {
            Stop($"Restore failed for {candidate.Name} in dresser slot {candidate.Slot + 1}.");
            return;
        }

        Status = $"Restored {candidate.Name} from dresser slot {candidate.Slot + 1}.";
        Plugin.Log.Information("Restored {ItemName} ({ItemId}) from glamour dresser slot {Slot}.", candidate.Name, candidate.ItemId, candidate.Slot + 1);
        plugin.Scanner.ForceRefresh();
    }

    private void StoreInventoryCandidate(InventoryCandidateItem candidate)
    {
        var uiState = UIState.Instance();
        if (uiState == null)
        {
            Stop("UI state is unavailable.");
            return;
        }

        var cabinet = &uiState->Cabinet;
        if (!cabinet->IsCabinetLoaded())
        {
            plugin.DebugLog("Cabinet is not loaded while trying to store itemId={ItemId}, cabinetId={CabinetId}.", candidate.ItemId, candidate.CabinetId);
            Stop("Open the armoire before storing inventory items.");
            return;
        }

        if (cabinet->IsItemInCabinet(candidate.CabinetId))
        {
        Status = $"{candidate.Name} is already in the armoire.";
            plugin.Scanner.ForceRefresh();
            return;
        }

        var stored = cabinet->StoreCabinetItem(candidate.CabinetId);
        plugin.DebugLog(
            "StoreCabinetItem returned {Result} for itemId={ItemId}, cabinetId={CabinetId}, container={Container}, slot={Slot}.",
            stored,
            candidate.ItemId,
            candidate.CabinetId,
            candidate.InventoryType,
            candidate.Slot + 1);
        if (!stored)
        {
            Stop($"Store failed for {candidate.Name} from {candidate.InventoryType} slot {candidate.Slot + 1}.");
            return;
        }

        Status = $"Stored {candidate.Name} from inventory slot {candidate.Slot + 1}.";
        Plugin.Log.Information(
            "Stored {ItemName} ({ItemId}, cabinet {CabinetId}) from {InventoryType} slot {Slot}.",
            candidate.Name,
            candidate.ItemId,
            candidate.CabinetId,
            candidate.InventoryType,
            candidate.Slot + 1);
        plugin.Scanner.ForceRefresh();
    }

    private CandidateItem? FindNextDresserCandidate()
    {
        return plugin.DresserReader.Read()
            .Where(item => plugin.CabinetIndex.CanGoInArmoire(item.ItemId))
            .Where(item => !plugin.Configuration.SkipDyedItems || !item.IsDyed)
            .Where(item => !plugin.Configuration.SkipHighQualityItems || !item.HighQuality)
            .OrderBy(item => item.Slot)
            .Select(item => new CandidateItem(
                item.ItemId,
                plugin.CabinetIndex.GetItemName(item.ItemId),
                item.HighQuality,
                item.Dye1,
                item.Dye2,
                item.Slot))
            .FirstOrDefault();
    }

    private InventoryCandidateItem? FindNextInventoryCandidate(InventoryManager* inventoryManager)
    {
        foreach (var inventoryType in PlayerInventoryTypes)
        {
            var container = inventoryManager->GetInventoryContainer(inventoryType);
            if (container == null || !container->IsLoaded)
                continue;

            for (var slot = 0; slot < container->Size; slot++)
            {
                var item = container->GetInventorySlot(slot);
                if (item == null || item->IsEmpty())
                    continue;

                var itemId = item->GetBaseItemId();
                if (!plugin.CabinetIndex.TryGetCabinetId(itemId, out var cabinetId))
                    continue;

                var highQuality = item->IsHighQuality();
                var dye1 = item->GetStain(0);
                var dye2 = item->GetStain(1);
                if (plugin.Configuration.SkipDyedItems && (dye1 != 0 || dye2 != 0))
                    continue;

                if (plugin.Configuration.SkipHighQualityItems && highQuality)
                    continue;

                return new InventoryCandidateItem(
                    itemId,
                    cabinetId,
                    plugin.CabinetIndex.GetItemName(itemId),
                    highQuality,
                    dye1,
                    dye2,
                    inventoryType,
                    slot);
            }
        }

        return null;
    }

}
