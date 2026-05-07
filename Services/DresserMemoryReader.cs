using Dalamud.Plugin.Services;
using System.Runtime.InteropServices;

namespace DresserArmoirePlugin.Services;

public sealed unsafe class DresserMemoryReader
{
    private const int DresserSize = 800;
    private const string DresserDataPointerSig = "48 8B 0D ?? ?? ?? ?? 48 8D 44 24 ?? 0F 57 C0";

    private readonly IPluginLog log;
    private readonly nint dresserDataPointerAddress;

    public DresserMemoryReader(ISigScanner sigScanner, IPluginLog log)
    {
        this.log = log;

        try
        {
            var instruction = sigScanner.ScanText(DresserDataPointerSig);
            var relative = Marshal.ReadInt32(instruction + 3);
            dresserDataPointerAddress = instruction + 7 + relative;
            log.Information("Found glamour dresser data pointer at 0x{Address:X}", dresserDataPointerAddress);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Could not locate glamour dresser data pointer.");
            dresserDataPointerAddress = 0;
        }
    }

    public IReadOnlyList<DresserItem> Read()
    {
        if (dresserDataPointerAddress == 0)
            return Array.Empty<DresserItem>();

        try
        {
            var dresserData = Marshal.ReadIntPtr(dresserDataPointerAddress);
            if (dresserData == 0)
                return Array.Empty<DresserItem>();

            var data = (byte*)dresserData + 4;
            if (data[(4 + 1 + 1) * DresserSize + 1] == 0)
                return Array.Empty<DresserItem>();

            var result = new List<DresserItem>();
            var itemIds = (uint*)data;
            var dye1Ids = data + (4 * DresserSize);
            var dye2Ids = data + (5 * DresserSize);

            for (var slot = 0; slot < DresserSize; slot++)
            {
                var rawId = itemIds[slot];
                if (rawId == 0)
                    continue;

                var highQuality = rawId > 1_000_000;
                var itemId = highQuality ? rawId - 1_000_000 : rawId;
                result.Add(new DresserItem(itemId, highQuality, dye1Ids[slot], dye2Ids[slot], slot));
            }

            return result;
        }
        catch (Exception ex)
        {
            log.Warning(ex, "Failed to read glamour dresser data.");
            return Array.Empty<DresserItem>();
        }
    }
}
