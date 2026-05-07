using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace DresserArmoirePlugin.Services;

public sealed unsafe class AutoRestoreService : IDisposable
{
    private const string OutfitGlamourAddon = "MiragePrismPrismBoxCrystallize";
    private const int OutfitGlamourCallback = 0;
    private const string StoreAsGlamourAddon = "MiragePrismPrismSetConvert";
    private const int StoreAsGlamourCallback = 18;
    private const string StoreAsOutfitAddon = "MiragePrismPrismSetConvertC";
    private const int StoreAsOutfitToggleCallback = 3;
    private const int StoreAsOutfitConfirmCallback = 2;
    private static readonly InventoryType[] PlayerInventoryTypes =
    [
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
    ];

    private readonly Plugin plugin;
    private readonly HashSet<ActionFailureKey> skippedFailures = [];
    private AutomationMode mode;

    public bool IsRunning { get; private set; }
    public string Status { get; private set; } = "Idle.";
    public bool IsRestoring => IsRunning && mode == AutomationMode.RestoreToInventory;
    public bool IsRestoringOutfits => IsRunning && mode == AutomationMode.RestoreOutfitSetsToInventory;
    public bool IsStoring => IsRunning && mode == AutomationMode.StoreToArmoire;
    private OutfitStoreStep outfitStoreStep;
    private CandidateItem? currentOutfitStoreCandidate;

    public AutoRestoreService(Plugin plugin)
    {
        this.plugin = plugin;
        Plugin.Framework.Update += OnFrameworkUpdate;
    }

    public void StartRestoreToInventory()
    {
        Start(AutomationMode.RestoreToInventory, "Restore to inventory started.");
    }

    public void StartStoreToArmoire()
    {
        Start(AutomationMode.StoreToArmoire, "Store to armoire started.");
    }

    public void StartRestoreOutfitSetsToInventory()
    {
        Start(AutomationMode.RestoreOutfitSetsToInventory, "Restore outfit-set items to inventory started.");
    }

    public void StartStoreOutfitGlamour()
    {
        Start(AutomationMode.StoreOutfitGlamour, "Store outfit glamour started.");
    }

    private void Start(AutomationMode automationMode, string chatMessage)
    {
        if (IsRunning)
            Stop("Switching automation mode.");

        mode = automationMode;
        IsRunning = true;
        outfitStoreStep = OutfitStoreStep.SelectOutfitGlamour;
        currentOutfitStoreCandidate = null;
        skippedFailures.Clear();
        Status = chatMessage;
        plugin.DebugLog("Automation started. mode={Mode}, skipDyed={SkipDyed}, skipHq={SkipHq}.", mode, plugin.Configuration.SkipDyedItems, plugin.Configuration.SkipHighQualityItems);
        Plugin.ChatGui.Print($"[Dresser Armoire Helper] {chatMessage}");
    }

    public void Stop(string reason = "Stopped.")
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        Status = reason;
        plugin.DebugLog("Automation stopped: mode={Mode}, reason={Reason}", mode, reason);
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

        if (mode == AutomationMode.RestoreToInventory)
        {
            StepRestoreToInventory(emptySlots);
            return;
        }

        if (mode == AutomationMode.RestoreOutfitSetsToInventory)
        {
            StepRestoreOutfitSetsToInventory(emptySlots);
            return;
        }

        if (mode == AutomationMode.StoreOutfitGlamour)
        {
            StepStoreOutfitGlamour();
            return;
        }

        StepStoreToArmoire(inventoryManager);
    }

    private void StepRestoreToInventory(uint emptySlots)
    {
        var dresserCandidate = FindNextDresserCandidate();
        if (dresserCandidate == null)
        {
            Stop("No armoire-eligible glamour dresser items remain.");
            return;
        }

        plugin.DebugLog(
            "Next dresser candidate: itemId={ItemId}, name={Name}, slot={Slot}, hq={HighQuality}, dyes={Dye1}/{Dye2}.",
            dresserCandidate.ItemId,
            dresserCandidate.Name,
            dresserCandidate.Slot + 1,
            dresserCandidate.HighQuality,
            dresserCandidate.Dye1,
            dresserCandidate.Dye2);

        if (emptySlots == 0)
        {
            Stop("Player inventory is full.");
            return;
        }

        RestoreDresserCandidate(dresserCandidate);
    }

    private void StepRestoreOutfitSetsToInventory(uint emptySlots)
    {
        var outfitCandidate = FindNextOutfitSetCandidate();
        if (outfitCandidate == null)
        {
            Stop("No outfit-set glamour dresser items remain.");
            return;
        }

        plugin.DebugLog(
            "Next outfit-set candidate: itemId={ItemId}, name={Name}, slot={Slot}, hq={HighQuality}, dyes={Dye1}/{Dye2}.",
            outfitCandidate.ItemId,
            outfitCandidate.Name,
            outfitCandidate.Slot + 1,
            outfitCandidate.HighQuality,
            outfitCandidate.Dye1,
            outfitCandidate.Dye2);

        if (emptySlots == 0)
        {
            Stop("Player inventory is full.");
            return;
        }

        RestoreDresserCandidate(outfitCandidate);
    }

    private void StepStoreToArmoire(InventoryManager* inventoryManager)
    {
        var inventoryCandidate = FindNextInventoryCandidate(inventoryManager);
        if (inventoryCandidate == null)
        {
            plugin.DebugLog("No inventory candidate found.");
            Stop("No armoire-eligible inventory items remain.");
            return;
        }

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
    }

    private void StepStoreOutfitGlamour()
    {
        if (!plugin.Configuration.EnableExperimentalOutfitStore)
        {
            Stop("Enable experimental outfit store first.");
            return;
        }

        currentOutfitStoreCandidate ??= FindNextCompleteOutfitSetCandidate();
        if (currentOutfitStoreCandidate == null)
        {
            Stop("No complete outfit-set candidate remains.");
            return;
        }

        var candidate = currentOutfitStoreCandidate;
        plugin.DebugLog(
            "Outfit store step: step={Step}, itemId={ItemId}, name={Name}, slot={Slot}.",
            outfitStoreStep,
            candidate.ItemId,
            candidate.Name,
            candidate.Slot + 1);

        switch (outfitStoreStep)
        {
            case OutfitStoreStep.SelectOutfitGlamour:
                if (AddonCallbackHelper.IsAddonReady("SelectYesno"))
                {
                    outfitStoreStep = OutfitStoreStep.ConfirmOutfitGlamour;
                    Status = $"Outfit glamour confirmation is open for {candidate.Name}.";
                    return;
                }

                if (AddonCallbackHelper.IsAddonReady(StoreAsGlamourAddon))
                {
                    outfitStoreStep = OutfitStoreStep.StoreAsGlamour;
                    Status = $"Outfit glamour conversion window is open for {candidate.Name}.";
                    return;
                }

                if (!AddonCallbackHelper.FireCallback(OutfitGlamourAddon, OutfitGlamourCallback, candidate.Slot))
                {
                    Stop("Could not trigger outfit glamour action. Open the glamour dresser.");
                    return;
                }

                outfitStoreStep = OutfitStoreStep.ConfirmOutfitGlamour;
                Status = $"Triggered outfit glamour for {candidate.Name}.";
                return;

            case OutfitStoreStep.ConfirmOutfitGlamour:
                if (!AddonCallbackHelper.FireCallback("SelectYesno", 0))
                    return;

                outfitStoreStep = OutfitStoreStep.StoreAsGlamour;
                Status = $"Confirmed outfit glamour for {candidate.Name}.";
                return;

            case OutfitStoreStep.StoreAsGlamour:
                if (!AddonCallbackHelper.FireCallback(StoreAsGlamourAddon, StoreAsGlamourCallback))
                    return;

                outfitStoreStep = OutfitStoreStep.ToggleStoreAsOutfit;
                Status = $"Triggered store as glamour for {candidate.Name}.";
                return;

            case OutfitStoreStep.ToggleStoreAsOutfit:
                if (!AddonCallbackHelper.FireCallback(StoreAsOutfitAddon, StoreAsOutfitToggleCallback))
                    return;

                outfitStoreStep = OutfitStoreStep.ConfirmStoreAsOutfit;
                Status = $"Enabled store as outfit glamour for {candidate.Name}.";
                return;

            case OutfitStoreStep.ConfirmStoreAsOutfit:
                if (!AddonCallbackHelper.FireCallback(StoreAsOutfitAddon, StoreAsOutfitConfirmCallback))
                    return;

                plugin.Scanner.RemoveRestoredCandidate(candidate.Slot, candidate.ItemId);
                plugin.Scanner.ForceRefresh();
                currentOutfitStoreCandidate = null;
                outfitStoreStep = OutfitStoreStep.SelectOutfitGlamour;
                Status = $"Stored outfit glamour for {candidate.Name}.";
                return;
        }
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
            SkipFailedCandidate(
                new ActionFailureKey(ActionKind.Restore, candidate.ItemId, candidate.Slot),
                $"Restore failed for {candidate.Name} in dresser slot {candidate.Slot + 1}");
            return;
        }

        Status = $"Restored {candidate.Name} from dresser slot {candidate.Slot + 1}.";
        Plugin.Log.Information("Restored {ItemName} ({ItemId}) from glamour dresser slot {Slot}.", candidate.Name, candidate.ItemId, candidate.Slot + 1);
        plugin.Scanner.RemoveRestoredCandidate(candidate.Slot, candidate.ItemId);
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
            SkipFailedCandidate(
                new ActionFailureKey(ActionKind.Store, candidate.ItemId, candidate.Slot),
                $"Store failed for {candidate.Name} from {candidate.InventoryType} slot {candidate.Slot + 1}");
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

    private void SkipFailedCandidate(ActionFailureKey key, string message)
    {
        skippedFailures.Add(key);
        plugin.Scanner.ForceRefresh();
        Status = $"{message}. Skipping this item.";
        plugin.DebugLog(
            "Action failure skipped: kind={Kind}, itemId={ItemId}, slot={Slot}, skippedCount={SkippedCount}, message={Message}.",
            key.Kind,
            key.ItemId,
            key.Slot + 1,
            skippedFailures.Count,
            message);
    }

    private CandidateItem? FindNextDresserCandidate()
    {
        return plugin.DresserReader.Read()
            .Where(item => plugin.CabinetIndex.CanGoInArmoire(item.ItemId))
            .Where(item => !plugin.Configuration.SkipDyedItems || !item.IsDyed)
            .Where(item => !plugin.Configuration.SkipHighQualityItems || !item.HighQuality)
            .Where(item => !skippedFailures.Contains(new ActionFailureKey(ActionKind.Restore, item.ItemId, item.Slot)))
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

    private CandidateItem? FindNextOutfitSetCandidate()
    {
        return plugin.OutfitIndex.FindNextCandidate(plugin.DresserReader.Read(), plugin.Configuration) is { } candidate
            && !skippedFailures.Contains(new ActionFailureKey(ActionKind.Restore, candidate.ItemId, candidate.Slot))
                ? candidate
                : plugin.DresserReader.Read()
                    .Where(item => plugin.OutfitIndex.IsOutfitSetItem(item.ItemId))
                    .Where(item => !plugin.Configuration.SkipDyedItems || !item.IsDyed)
                    .Where(item => !plugin.Configuration.SkipHighQualityItems || !item.HighQuality)
                    .Where(item => !skippedFailures.Contains(new ActionFailureKey(ActionKind.Restore, item.ItemId, item.Slot)))
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

                if (skippedFailures.Contains(new ActionFailureKey(ActionKind.Store, itemId, slot)))
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

    private CandidateItem? FindNextCompleteOutfitSetCandidate()
    {
        plugin.Scanner.ForceRefresh();
        return plugin.Scanner.OutfitSetCandidates
            .Where(outfit => outfit.OwnedCount >= outfit.TotalCount)
            .SelectMany(outfit => outfit.Items)
            .Where(item => !skippedFailures.Contains(new ActionFailureKey(ActionKind.OutfitStore, item.ItemId, item.Slot)))
            .OrderBy(item => item.Slot)
            .FirstOrDefault();
    }

    private enum ActionKind
    {
        Restore,
        Store,
        OutfitStore,
    }

    private sealed record ActionFailureKey(ActionKind Kind, uint ItemId, int Slot);

    private enum AutomationMode
    {
        RestoreToInventory,
        RestoreOutfitSetsToInventory,
        StoreToArmoire,
        StoreOutfitGlamour,
    }

    private enum OutfitStoreStep
    {
        SelectOutfitGlamour,
        ConfirmOutfitGlamour,
        StoreAsGlamour,
        ToggleStoreAsOutfit,
        ConfirmStoreAsOutfit,
    }
}
