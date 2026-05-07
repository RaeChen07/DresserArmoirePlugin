namespace DresserArmoirePlugin.Services;

public sealed class CandidateScanner : IDisposable
{
    private readonly Plugin plugin;

    public IReadOnlyList<CandidateItem> Candidates { get; private set; } = [];
    public string Status { get; private set; } = "Open your glamour dresser.";
    public bool DresserLoaded { get; private set; }

    public CandidateScanner(Plugin plugin)
    {
        this.plugin = plugin;
        Plugin.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= OnFrameworkUpdate;
    }

    public void ForceRefresh() => Refresh();

    public void RemoveRestoredCandidate(int slot, uint itemId)
    {
        var before = Candidates.Count;
        Candidates = Candidates
            .Where(candidate => candidate.Slot != slot || candidate.ItemId != itemId)
            .ToList();

        if (before != Candidates.Count)
        {
            Status = DresserLoaded
                ? $"Dresser open. {Candidates.Count} candidate(s)."
                : Status;
            plugin.DebugLog("Removed restored candidate from list: itemId={ItemId}, slot={Slot}, before={Before}, after={After}.", itemId, slot + 1, before, Candidates.Count);
        }
    }

    private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework framework)
    {
        var loaded = plugin.DresserReader.IsLoaded();
        if (loaded == DresserLoaded)
            return;

        if (loaded)
            Refresh();
        else
            Clear();
    }

    private void Refresh()
    {
        var dresserLoaded = plugin.DresserReader.IsLoaded();
        DresserLoaded = dresserLoaded;

        if (!dresserLoaded)
        {
            Clear();
            return;
        }

        var dresserItems = plugin.DresserReader.Read();
        Candidates = dresserItems
            .Where(item => plugin.CabinetIndex.CanGoInArmoire(item.ItemId))
            .Where(item => !plugin.Configuration.SkipDyedItems || !item.IsDyed)
            .Where(item => !plugin.Configuration.SkipHighQualityItems || !item.HighQuality)
            .Select(item => new CandidateItem(
                item.ItemId,
                plugin.CabinetIndex.GetItemName(item.ItemId),
                item.HighQuality,
                item.Dye1,
                item.Dye2,
                item.Slot))
            .OrderBy(item => item.Slot)
            .ToList();

        Status = $"Dresser open. {Candidates.Count} candidate(s) from {dresserItems.Count} item(s).";
        plugin.DebugLog(
            "Candidate refresh: dresserItems={DresserItemCount}, candidates={CandidateCount}, skipDyed={SkipDyed}, skipHq={SkipHq}.",
            dresserItems.Count,
            Candidates.Count,
            plugin.Configuration.SkipDyedItems,
            plugin.Configuration.SkipHighQualityItems);
    }

    private void Clear()
    {
        if (DresserLoaded || Candidates.Count > 0)
            plugin.DebugLog("Candidate list cleared. Previous candidates={CandidateCount}.", Candidates.Count);

        DresserLoaded = false;
        Candidates = [];
        Status = "Open your glamour dresser.";
    }
}
