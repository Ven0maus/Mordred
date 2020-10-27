using Microsoft.Xna.Framework;

namespace Mordred.Entities.Monsters
{
    public abstract class Monster : Actor
    {
        public Monster(Color foreground, int glyph) : base(foreground, Color.Black, glyph) { }
    }
}
