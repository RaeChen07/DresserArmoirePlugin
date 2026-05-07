using FFXIVClientStructs.FFXIV.Client.Game;

namespace DresserArmoirePlugin.Services;

public sealed unsafe class AutoRestoreService : IDisposable
{
    private const int TicksBetweenActions = 45;

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
        Plugin.ChatGui.Print("[Dresser Armoire Helper] Auto-restore started.");
    }

    public void Stop(string reason = "Stopped.")
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        Status = reason;
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

        if (inventoryManager->GetEmptySlotsInBag() == 0)
        {
            Stop("Player inventory is full.");
            return;
        }

        var candidate = FindNextCandidate();
        if (candidate == null)
        {
            Stop("No armoire-eligible glamour dresser items remain.");
            return;
        }

        var mirageManager = MirageManager.Instance();
        if (mirageManager == null || !mirageManager->PrismBoxLoaded)
        {
            Stop("Open the glamour dresser before starting auto-restore.");
            return;
        }

        var restored = mirageManager->RestorePrismBoxItem((uint)candidate.Slot);
        if (!restored)
        {
            Stop($"Restore failed for {candidate.Name} in dresser slot {candidate.Slot + 1}.");
            return;
        }

        Status = $"Restored {candidate.Name} from slot {candidate.Slot + 1}.";
        Plugin.Log.Information("Restored {ItemName} ({ItemId}) from glamour dresser slot {Slot}.", candidate.Name, candidate.ItemId, candidate.Slot + 1);
    }

    private CandidateItem? FindNextCandidate()
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
}
