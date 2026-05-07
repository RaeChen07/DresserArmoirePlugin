using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
namespace DresserArmoirePlugin.Services;

public sealed class CallbackRecorder : IDisposable
{
    private static readonly string[] RecordedAddons =
    [
        "MiragePrismPrismBox",
        "SelectYesno",
    ];

    private readonly Plugin plugin;

    public CallbackRecorder(Plugin plugin)
    {
        this.plugin = plugin;
    }

    public bool IsRecording { get; private set; }

    public void Start()
    {
        if (IsRecording)
            return;

        IsRecording = true;
        foreach (var addon in RecordedAddons)
            Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, addon, OnPostReceiveEvent);

        Plugin.ChatGui.Print("[Dresser Armoire Helper] Callback recorder started. Manual UI clicks will be logged to /xllog.");
        plugin.DebugLog("Callback recorder started for addons: {Addons}.", string.Join(", ", RecordedAddons));
    }

    public void Stop()
    {
        if (!IsRecording)
            return;

        foreach (var addon in RecordedAddons)
            Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, addon, OnPostReceiveEvent);

        IsRecording = false;
        Plugin.ChatGui.Print("[Dresser Armoire Helper] Callback recorder stopped.");
        plugin.DebugLog("Callback recorder stopped.");
    }

    public void Dispose()
    {
        Stop();
    }

    private void OnPostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonReceiveEventArgs receiveArgs)
            return;

        Plugin.Log.Information(
            "[CallbackRecorder] addon={AddonName}, event={EventType}, eventParam={EventParam}, atkEvent=0x{AtkEvent:X}, atkEventData=0x{AtkEventData:X}",
            args.AddonName,
            receiveArgs.AtkEventType,
            receiveArgs.EventParam,
            receiveArgs.AtkEvent,
            receiveArgs.AtkEventData);
    }
}
