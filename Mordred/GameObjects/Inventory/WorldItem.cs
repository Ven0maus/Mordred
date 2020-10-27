using Microsoft.Xna.Framework;
using SadConsole.Entities;

namespace Mordred.GameObjects
{
    public class WorldItem : Entity
    {
        public int Id { get; private set; }
        public bool Edible { get; private set; }
        public double EdibleWorth { get; private set; }

        public readonly int[] DroppedBy;

        public int Amount;
        public WorldItem(int id, Color foreground, Color background, int glyph, bool edible = false, double edibleWorth = 0, int? amount = null, int[] droppedBy = null) : base(foreground, background, glyph)
        {
            Id = id;
            EdibleWorth = edibleWorth;
            Edible = edible;
            Amount = amount ?? 1;
            DroppedBy = droppedBy;
        }

        private WorldItem(WorldItem original) : base(original.Animation[0].Foreground, original.Animation[0].Background, original.Animation[0].Glyph)
        {
            Id = original.Id;
            EdibleWorth = original.EdibleWorth;
            Edible = original.Edible;
            Amount = original.Amount;
            DroppedBy = original.DroppedBy;
        }

        public WorldItem Clone()
        {
            return new WorldItem(this);
        }
    }
}
