using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
namespace DresserArmoirePlugin.Services;

public sealed class CallbackRecorder : IDisposable
{
    private static readonly string[] RecordedAddons =
    [
        "MiragePrismPrismBox",
        "MiragePrismPrismBoxCrystallize",
        "MiragePrismPrismSetConvert",
        "MiragePrismPrismSetConvertC",
        "MiragePrismPrismItemDetail",
        "MiragePrismPrismItem",
        "MiragePrismMiragePlate",
        "MiragePrismMiragePlateConfirm",
        "MiragePrismDresser",
        "MiragePrismDresserItemDetail",
        "ItemDetail",
        "ContextMenu",
        "ContextIconMenu",
        "SelectYesno",
    ];

    private static readonly HashSet<string> IgnoredEvents =
    [
        "MouseMove",
        "MouseOver",
        "MouseOut",
        "MouseUp",
        "LinkMouseOver",
        "LinkMouseOut",
        "TimerTick",
        "TimelineActiveLabelChanged",
        "ListItemRollOver",
        "ListItemRollOut",
    ];

    private static readonly string[] IgnoredAddonPrefixes =
    [
        "ChatLog",
        "ChatLogPanel_",
        "NamePlate",
        "_LimitBreak",
        "_ScreenInfo",
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
        if (plugin.Configuration.RecordAllAddonCallbacks)
            Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, OnPostReceiveEvent);
        else
            foreach (var addon in RecordedAddons)
                Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, addon, OnPostReceiveEvent);

        Plugin.ChatGui.Print("[Dresser Armoire Helper] Callback recorder started. Manual UI clicks will be logged to /xllog.");
        plugin.DebugLog(
            "Callback recorder started. allAddons={AllAddons}, addons={Addons}.",
            plugin.Configuration.RecordAllAddonCallbacks,
            string.Join(", ", RecordedAddons));
    }

    public void Stop()
    {
        if (!IsRecording)
            return;

        if (plugin.Configuration.RecordAllAddonCallbacks)
            Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, OnPostReceiveEvent);
        else
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

        if (IgnoredEvents.Contains(receiveArgs.AtkEventType.ToString()))
            return;

        if (IgnoredAddonPrefixes.Any(prefix => args.AddonName.StartsWith(prefix, StringComparison.Ordinal)))
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
