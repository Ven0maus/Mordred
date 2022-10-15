using Mordred.Entities;
using SadConsole.Entities;
using SadRogue.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.GameObjects.ItemInventory.Items
{
    public class WorldItem : Entity, IEntity
    {
        public int Id { get; private set; }

        public Point WorldPosition { get; set; }

        public readonly Dictionary<int, DropRate> DroppedBy;

        public int Amount;
        public WorldItem(int id, string name, Color foreground, Color background, int glyph, int? amount = null, string[] droppedBy = null) : base(foreground, background, glyph, 1)
        {
            Name = name;
            Id = id;
            Amount = amount ?? 1;

            if (droppedBy != null)
            {
                DroppedBy = droppedBy.Select(a =>
                {
                    var parts = a.Split('|');
                    var cellId = int.Parse(parts[0]);

                    // Min and Max
                    parts = parts[1].Split(':');
                    if (parts.Length == 1)
                    {
                        var droprate = int.Parse(parts[0]);
                        return (cellId, new DropRate(droprate, droprate));
                    }
                    else
                    {
                        return (cellId, new DropRate(int.Parse(parts[0]), int.Parse(parts[1]) + 1));
                    }
                }).ToDictionary(a => a.cellId, a => a.Item2);
            }
        }

        public static string[] ConvertDropRatesToString(Dictionary<int, DropRate> dropRates)
        {
            return dropRates.Select(a => a.Key + "|" + a.Value.Min + ":" + a.Value.Max).ToArray();
        }

        public sealed class DropRate
        {
            public int Min { get; }
            public int Max { get; }

            public DropRate(int min, int max)
            {
                Min = min;
                Max = max;
            }
        }

        public DropRate GetDropRateForCellId(int cellId)
        {
            if (DroppedBy == null) return null;
            DroppedBy.TryGetValue(cellId, out DropRate dropRate);
            return dropRate;
        }

        public bool IsDroppedBy(int cellId)
        {
            if (DroppedBy == null) return false;
            return DroppedBy.ContainsKey(cellId);
        }

        public IEnumerable<int> GetCellDropIds()
        {
            if (DroppedBy == null) return Enumerable.Empty<int>();
            return DroppedBy.Select(a => a.Key);
        }

        private WorldItem(WorldItem original) : base(original.Appearance.Foreground, original.Appearance.Background, original.Appearance.Glyph, 1)
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
