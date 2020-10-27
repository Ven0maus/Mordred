using Microsoft.Xna.Framework;

namespace Mordred.Entities.Animals
{
    public abstract class Animal : Actor
    {
        public Animal(Color foreground, int glyph) : base(foreground, Color.Black, glyph) { }
    }
}
