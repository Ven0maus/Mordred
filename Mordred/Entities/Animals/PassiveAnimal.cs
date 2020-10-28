using Microsoft.Xna.Framework;

namespace Mordred.Entities.Animals
{
    public abstract class PassiveAnimal : Animal
    {
        public PassiveAnimal(Color foreground, int glyph) : base(foreground, glyph) { }
    }
}
