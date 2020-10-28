using Microsoft.Xna.Framework;
using SadConsole.Entities;

namespace Mordred.GameObjects.ItemInventory.Items
{
    public class WorldItem : Entity
    {
        public int Id { get; private set; }

        public readonly int[] DroppedBy;

        public int Amount;
        public WorldItem(int id, Color foreground, Color background, int glyph, int? amount = null, int[] droppedBy = null) : base(foreground, background, glyph)
        {
            Id = id;
            Amount = amount ?? 1;
            DroppedBy = droppedBy;
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
