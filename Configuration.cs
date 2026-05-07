using Dalamud.Configuration;

namespace DresserArmoirePlugin;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool SkipDyedItems { get; set; } = true;
    public bool SkipHighQualityItems { get; set; } = false;
    public bool DebugLogging { get; set; } = false;
    public bool EnableExperimentalOutfitStore { get; set; } = false;
    public int OutfitGlamourCallback = 14;
    public int StoreAsGlamourCallback = 15;
    public int StoreAsOutfitGlamourToggleCallback = 16;
}
