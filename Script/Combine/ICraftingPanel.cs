public interface ICraftingPanel
{
    void TampilkanDariInventory();
    void PilihBahan(string namaBahan);
    void TempatkanBahanKeSlotCombine(int slotCombineIndex);
    void BuatJamu();
    void ResetSlotCombine();
    ResepJamu GetCurrentCraftedJamu();
    bool NeedsScaleAdjustment { get; }

}