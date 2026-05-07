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
    }

    private void Clear()
    {
        DresserLoaded = false;
        Candidates = [];
        Status = "Open your glamour dresser.";
    }
}
