using FFXIVClientStructs.FFXIV.Component.GUI;

namespace DresserArmoirePlugin.Services;

public static unsafe class AddonCallbackHelper
{
    public static bool FireCallback(string addonName, params int[] values)
    {
        var addon = (AtkUnitBase*)Plugin.GameGui.GetAddonByName(addonName).Address;
        if (addon == null || !addon->IsVisible || !addon->IsReady)
        {
            Plugin.Log.Debug("Addon callback skipped: addon={AddonName}, available={Available}.", addonName, addon != null);
            return false;
        }

        var atkValues = stackalloc AtkValue[values.Length];
        for (var i = 0; i < values.Length; i++)
            atkValues[i].SetInt(values[i]);

        addon->FireCallback((uint)values.Length, atkValues);
        return true;
    }
}
