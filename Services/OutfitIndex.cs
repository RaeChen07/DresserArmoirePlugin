using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace DresserArmoirePlugin.Services;

public sealed class OutfitIndex
{
    private readonly IDataManager dataManager;
    private readonly Dictionary<uint, OutfitSetInfo> outfitsById = [];
    private readonly Dictionary<uint, List<OutfitSetInfo>> outfitsByItemId = [];

    public OutfitIndex(IDataManager dataManager)
    {
        this.dataManager = dataManager;
        BuildIndex();
    }

    public IReadOnlyList<OutfitSetCandidate> BuildCandidates(IEnumerable<DresserItem> dresserItems, Configuration configuration)
    {
        var dresserItemsById = dresserItems
            .Where(item => !configuration.SkipDyedItems || !item.IsDyed)
            .Where(item => !configuration.SkipHighQualityItems || !item.HighQuality)
            .GroupBy(item => item.ItemId)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Slot).ToList());

        var result = new List<OutfitSetCandidate>();
        foreach (var outfit in outfitsById.Values.OrderBy(outfit => outfit.SortKey))
        {
            var items = new List<CandidateItem>();
            foreach (var itemId in outfit.ItemIds)
            {
                if (!dresserItemsById.TryGetValue(itemId, out var matchingItems))
                    continue;

                foreach (var item in matchingItems)
                {
                    items.Add(new CandidateItem(
                        item.ItemId,
                        GetItemName(item.ItemId),
                        item.HighQuality,
                        item.Dye1,
                        item.Dye2,
                        item.Slot));
                }
            }

            if (items.Count == 0)
                continue;

            result.Add(new OutfitSetCandidate(outfit.OutfitId, outfit.Name, items.Count, outfit.ItemIds.Count, items.OrderBy(item => item.Slot).ToList()));
        }

        return result;
    }

    public CandidateItem? FindNextCandidate(IEnumerable<DresserItem> dresserItems, Configuration configuration)
    {
        return BuildCandidates(dresserItems, configuration)
            .SelectMany(outfit => outfit.Items)
            .OrderBy(item => item.Slot)
            .FirstOrDefault();
    }

    public bool IsOutfitSetItem(uint itemId) => outfitsByItemId.ContainsKey(itemId);

    private void BuildIndex()
    {
        var itemSheet = dataManager.GetExcelSheet<Item>();
        foreach (var row in dataManager.GetExcelSheet<RawRow>(name: "MirageStoreSetItem"))
        {
            var outfitId = row.RowId;
            if (outfitId == 0)
                continue;

            var itemIds = new List<uint>();
            for (var i = 0; i < 9; i++)
            {
                var itemId = (uint)row.ReadColumn(2 + i);
                if (itemId != 0)
                    itemIds.Add(itemId);
            }

            if (itemIds.Count == 0)
                continue;

            var name = itemSheet.GetRow(outfitId).Name.ExtractText();
            var sortKey = itemIds.Min(itemId => GetSortKey(itemSheet.GetRow(itemId)));
            var outfit = new OutfitSetInfo(outfitId, name, itemIds, sortKey);
            outfitsById[outfitId] = outfit;

            foreach (var itemId in itemIds)
            {
                if (!outfitsByItemId.TryGetValue(itemId, out var outfits))
                {
                    outfits = [];
                    outfitsByItemId[itemId] = outfits;
                }

                outfits.Add(outfit);
            }
        }
    }

    private string GetItemName(uint itemId)
    {
        var item = dataManager.GetExcelSheet<Item>().GetRow(itemId);
        return item.Name.ExtractText();
    }

    private static ulong GetSortKey(Item item)
    {
        return (((10000ul - item.LevelItem.RowId) * 100000u + item.Unknown4) * 100000u + item.RowId);
    }

    private sealed record OutfitSetInfo(uint OutfitId, string Name, IReadOnlyList<uint> ItemIds, ulong SortKey);
}
