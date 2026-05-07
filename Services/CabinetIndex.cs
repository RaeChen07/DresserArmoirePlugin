using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace DresserArmoirePlugin.Services;

public sealed class CabinetIndex
{
    private readonly IDataManager dataManager;
    private readonly Dictionary<uint, uint> cabinetIdsByItemId;

    public CabinetIndex(IDataManager dataManager)
    {
        this.dataManager = dataManager;
        cabinetIdsByItemId = dataManager.GetExcelSheet<Cabinet>()
            .Where(row => row.Item.RowId != 0)
            .ToDictionary(row => row.Item.RowId, row => row.RowId);
    }

    public bool CanGoInArmoire(uint itemId) => cabinetIdsByItemId.ContainsKey(itemId);

    public bool TryGetCabinetId(uint itemId, out uint cabinetId) => cabinetIdsByItemId.TryGetValue(itemId, out cabinetId);

    public string GetItemName(uint itemId)
    {
        var item = dataManager.GetExcelSheet<Item>().GetRow(itemId);
        return item.Name.ExtractText();
    }
}
