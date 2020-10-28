using Mordred.Config;
using Mordred.GameObjects.ItemInventory.Items;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.GameObjects.ItemInventory
{
    public class Inventory
    {
        public static readonly Dictionary<int, WorldItem> ItemCache = ConfigLoader.LoadWorldItems();

        private readonly Dictionary<int, int> Items;

        public Inventory()
        {
            Items = new Dictionary<int, int>();
        }

        public Dictionary<int, int> Peek()
        {
            return Items.ToDictionary(a => a.Key, a => a.Value);
        }

        public bool HasItem(int itemId)
        {
            return Items.ContainsKey(itemId);
        }

        /// <summary>
        /// Take's the selected item from the inventory.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public WorldItem Take(int itemId)
        {
            if (!HasItem(itemId)) return null;
            var item = ItemCache[itemId].Clone();
            item.Amount = Items[itemId];
            Items.Remove(itemId);
            return item;
        }

        /// <summary>
        /// Takes only the given amount from the item with this itemId from the inventory.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public WorldItem Take(int itemId, int amount)
        {
            if (!HasItem(itemId)) return null;
            var item = ItemCache[itemId].Clone();
            var current = Items[itemId];
            if (amount >= current)
            {
                item.Amount = current;
                Items.Remove(itemId);
            }
            else
            {
                item.Amount = amount;
                Items[itemId] -= amount;
            }
            return item;
        }

        public void Add(int itemId, int amount = 1)
        {
            if (Items.ContainsKey(itemId))
            {
                Items[itemId] += amount;
            }
            else
            {
                Items.Add(itemId, amount);
            }
        }
    }
}
