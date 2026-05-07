namespace DresserArmoirePlugin.Services;

public sealed class CandidateScanner : IDisposable
{
    private const int RefreshIntervalTicks = 30;

    private readonly Plugin plugin;
    private int ticksUntilRefresh;

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
        if (ticksUntilRefresh-- > 0)
            return;

        ticksUntilRefresh = RefreshIntervalTicks;
        Refresh();
    }

    private void Refresh()
    {
        var dresserLoaded = plugin.DresserReader.IsLoaded();
        DresserLoaded = dresserLoaded;

        if (!dresserLoaded)
        {
            Candidates = [];
            Status = "Open your glamour dresser.";
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
}
