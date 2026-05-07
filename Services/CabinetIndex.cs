using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace DresserArmoirePlugin.Services;

public sealed class CabinetIndex
{
    private readonly IDataManager dataManager;
    private readonly HashSet<uint> cabinetItemIds;

    public CabinetIndex(IDataManager dataManager)
    {
        this.dataManager = dataManager;
        cabinetItemIds = dataManager.GetExcelSheet<Cabinet>()
            .Select(row => row.Item.RowId)
            .Where(id => id != 0)
            .ToHashSet();
    }

    public bool CanGoInArmoire(uint itemId) => cabinetItemIds.Contains(itemId);

    public string GetItemName(uint itemId)
    {
        var item = dataManager.GetExcelSheet<Item>().GetRow(itemId);
        return item.Name.ExtractText();
    }
}
