using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace DresserArmoirePlugin.Windows;

public sealed class MainWindow : Window
{
    private readonly Plugin plugin;

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
        if (!ImGui.BeginTabBar("main-tabs"))
            return;

        if (ImGui.BeginTabItem("Armoire Transfer"))
        {
            DrawArmoireTransferTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Outfit Sets"))
        {
            DrawOutfitSetsTab();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawArmoireTransferTab()
    {
        var skipDyed = plugin.Configuration.SkipDyedItems;
        if (ImGui.Checkbox("Skip dyed items", ref skipDyed))
        {
            plugin.Configuration.SkipDyedItems = skipDyed;
            plugin.SaveConfiguration();
            plugin.Scanner.ForceRefresh();
        }

        ImGui.SameLine();

        var skipHq = plugin.Configuration.SkipHighQualityItems;
        if (ImGui.Checkbox("Skip HQ items", ref skipHq))
        {
            plugin.Configuration.SkipHighQualityItems = skipHq;
            plugin.SaveConfiguration();
            plugin.Scanner.ForceRefresh();
        }

        if (ImGui.Button("Refresh now"))
            plugin.Scanner.ForceRefresh();

        ImGui.SameLine();
        ImGui.TextUnformatted(plugin.Scanner.Status);

        ImGui.Separator();

        var candidates = plugin.Scanner.Candidates;
        if (candidates.Count == 0)
        {
            ImGui.TextWrapped("No candidates are currently listed.");
        }
        else
        {
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
        }

        if (plugin.AutoRestore.IsRunning)
        {
            if (ImGui.Button("Stop automation"))
                plugin.AutoRestore.Stop();
        }
        else
        {
            if (ImGui.Button("Restore dresser to inventory"))
                plugin.AutoRestore.StartRestoreToInventory();

            ImGui.SameLine();

            if (ImGui.Button("Store inventory to armoire"))
                plugin.AutoRestore.StartStoreToArmoire();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted(plugin.AutoRestore.Status);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Restore only pulls armoire-eligible glamour dresser items into your inventory until full or empty. Store only deposits eligible inventory items into the armoire.");

        DrawDebugToggleBottomRight();
    }

    private static void DrawOutfitSetsTab()
    {
        ImGui.TextWrapped("Outfit set candidates will appear here in a future update.");
    }

    private void DrawDebugToggleBottomRight()
    {
        var debugLogging = plugin.Configuration.DebugLogging;
        const string label = "Debug logs";
        var width = ImGui.CalcTextSize(label).X + ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X;
        var available = ImGui.GetContentRegionAvail();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, available.X - width));
        ImGui.SetCursorPosY(ImGui.GetWindowHeight() - ImGui.GetFrameHeightWithSpacing() - ImGui.GetStyle().WindowPadding.Y);

        if (ImGui.Checkbox(label, ref debugLogging))
        {
            plugin.Configuration.DebugLogging = debugLogging;
            plugin.SaveConfiguration();
            plugin.DebugLog("Debug logging enabled from UI.");
        }
    }
}
