using Microsoft.Xna.Framework;

namespace Mordred.Entities.Animals
{
    public abstract class PassiveAnimal : Animal
    {
        public PassiveAnimal(Color foreground, int glyph, Gender gender) : base(foreground, glyph, gender) { }
    }
}
