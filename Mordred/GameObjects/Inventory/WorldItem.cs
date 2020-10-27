using Microsoft.Xna.Framework;
using SadConsole.Entities;

namespace Mordred.GameObjects
{
    public class WorldItem : Entity
    {
        public int Amount;
        public WorldItem(Color foreground, Color background, int glyph, int? amount = null) : base(foreground, background, glyph)
        {
            Amount = amount ?? 1;
        }

        public WorldItem(WorldItem original) : base(original.Animation[0].Foreground, original.Animation[0].Background, original.Animation[0].Glyph)
        {
            Amount = original.Amount;
        }
    }
}
