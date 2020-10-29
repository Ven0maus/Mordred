using Microsoft.Xna.Framework;
using SadConsole.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.GameObjects.ItemInventory.Items
{
    public class WorldItem : Entity
    {
        public int Id { get; private set; }

        public readonly string[] DroppedBy;

        public int Amount;
        public WorldItem(int id, string name, Color foreground, Color background, int glyph, int? amount = null, string[] droppedBy = null) : base(foreground, background, glyph)
        {
            Name = name;
            Id = id;
            Amount = amount ?? 1;
            DroppedBy = droppedBy;
        }

        public KeyValuePair<int, int>? GetDropRateForCellId(int cellId)
        {
            if (DroppedBy == null) return null;
            foreach (var cell in DroppedBy)
            {
                var parts = cell.Split('|');
                if (int.Parse(parts[0]) == cellId)
                {
                    parts = parts[1].Split(':');
                    if (parts.Length == 1)
                    {
                        var droprate = int.Parse(parts[0]);
                        return new KeyValuePair<int, int>(droprate, droprate);
                    }
                    else
                    {
                        // Min and Max
                        return new KeyValuePair<int, int>(int.Parse(parts[0]), int.Parse(parts[1]) + 1);
                    }
                }
            }
            return null;
        }

        public bool IsDroppedBy(int cellId)
        {
            if (DroppedBy == null) return false;
            return DroppedBy.Any(a => int.Parse(a.Split('|')[0]) == cellId);
        }

        public List<int> GetCellDropIds()
        {
            if (DroppedBy == null) return new List<int>();
            return DroppedBy.Select(a => int.Parse(a.Split('|')[0])).ToList();
        }

        private WorldItem(WorldItem original) : base(original.Animation[0].Foreground, original.Animation[0].Background, original.Animation[0].Glyph)
        {
            Id = original.Id;
            Amount = original.Amount;
            DroppedBy = original.DroppedBy;
        }

        public virtual WorldItem Clone()
        {
            return new WorldItem(this);
        }
    }
}
