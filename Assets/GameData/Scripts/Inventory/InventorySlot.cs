namespace Pomunculus.Items
{
    [System.Serializable]
    public class InventorySlot
    {
        public int ItemId;
        public int Amount;

        public InventorySlot(int itemId, int amount)
        {
            ItemId = itemId;
            Amount = amount;
        }
    }
}