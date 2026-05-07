namespace DresserArmoirePlugin;

public sealed record DresserItem(uint ItemId, bool HighQuality, byte Dye1, byte Dye2, int Slot)
{
    public bool IsDyed => Dye1 != 0 || Dye2 != 0;
}

public sealed record CandidateItem(uint ItemId, string Name, bool HighQuality, byte Dye1, byte Dye2, int Slot)
{
    public bool IsDyed => Dye1 != 0 || Dye2 != 0;
}

public sealed record InventoryCandidateItem(
    uint ItemId,
    uint CabinetId,
    string Name,
    bool HighQuality,
    byte Dye1,
    byte Dye2,
    FFXIVClientStructs.FFXIV.Client.Game.InventoryType InventoryType,
    int Slot)
{
    public bool IsDyed => Dye1 != 0 || Dye2 != 0;
}
