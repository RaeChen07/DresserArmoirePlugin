using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DresserArmoirePlugin.Services;
using DresserArmoirePlugin.Windows;

namespace DresserArmoirePlugin;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/darmoire";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    public Configuration Configuration { get; }
    public CabinetIndex CabinetIndex { get; }
    public DresserMemoryReader DresserReader { get; }
    public CandidateScanner Scanner { get; }
    public AutoRestoreService AutoRestore { get; }
    public readonly WindowSystem WindowSystem = new("DresserArmoirePlugin");

    private readonly MainWindow mainWindow;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        CabinetIndex = new CabinetIndex(DataManager);
        DresserReader = new DresserMemoryReader(SigScanner, Log);
        Scanner = new CandidateScanner(this);
        AutoRestore = new AutoRestoreService(this);
        mainWindow = new MainWindow(this);

        WindowSystem.AddWindow(mainWindow);
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the glamour dresser armoire helper."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;
        CommandManager.RemoveHandler(CommandName);
        AutoRestore.Dispose();
        Scanner.Dispose();
        WindowSystem.RemoveAllWindows();
    }

    public void SaveConfiguration() => PluginInterface.SavePluginConfig(Configuration);
    public void ToggleMainUi() => mainWindow.Toggle();

    private void OnCommand(string command, string args) => ToggleMainUi();
}
