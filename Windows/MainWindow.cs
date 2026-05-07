using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace DresserArmoirePlugin.Windows;

public sealed class MainWindow : Window
{
    private readonly Plugin plugin;
    private List<CandidateItem> candidates = [];
    private string status = "Open your glamour dresser, then press Scan.";

    public MainWindow(Plugin plugin)
        : base("Dresser Armoire Helper###DresserArmoireHelper")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new System.Numerics.Vector2(520, 320),
            MaximumSize = new System.Numerics.Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw()
    {
        var skipDyed = plugin.Configuration.SkipDyedItems;
        if (ImGui.Checkbox("Skip dyed items", ref skipDyed))
        {
            plugin.Configuration.SkipDyedItems = skipDyed;
            plugin.SaveConfiguration();
        }

        ImGui.SameLine();

        var skipHq = plugin.Configuration.SkipHighQualityItems;
        if (ImGui.Checkbox("Skip HQ items", ref skipHq))
        {
            plugin.Configuration.SkipHighQualityItems = skipHq;
            plugin.SaveConfiguration();
        }

        if (ImGui.Button("Scan glamour dresser"))
            Scan();

        ImGui.SameLine();
        ImGui.TextUnformatted(status);

        ImGui.Separator();

        if (candidates.Count == 0)
        {
            ImGui.TextWrapped("No candidates are currently listed.");
            return;
        }

        ImGui.TextUnformatted($"Candidates: {candidates.Count}");
        ImGui.BeginChild("candidate-list", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing() * 2), true);
        foreach (var item in candidates)
        {
            var flags = new List<string>();
            if (item.HighQuality)
                flags.Add("HQ");
            if (item.IsDyed)
                flags.Add($"Dye {item.Dye1}/{item.Dye2}");

            var suffix = flags.Count == 0 ? string.Empty : $" ({string.Join(", ", flags)})";
            ImGui.BulletText($"#{item.Slot + 1}: {item.Name}{suffix}");
        }
        ImGui.EndChild();

        ImGui.BeginDisabled();
        ImGui.Button("Move listed items to armoire");
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("The scanner is ready. The actual move executor should be wired only after verifying the current armoire UI callback flow in-game.");
    }

    private void Scan()
    {
        var dresserItems = plugin.DresserReader.Read();
        candidates = dresserItems
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

        status = dresserItems.Count == 0
            ? "No dresser data. Open the glamour dresser in-game first."
            : $"Scanned {dresserItems.Count} dresser items.";
    }
}
