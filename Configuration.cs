using Dalamud.Configuration;

namespace DresserArmoirePlugin;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool SkipDyedItems { get; set; } = true;
    public bool SkipHighQualityItems { get; set; } = false;
}
