using Microsoft.Xna.Framework;

namespace Mordred.Entities.Animals
{
    public abstract class PredatorAnimal : Animal
    {
        public PredatorAnimal(Color foreground, int glyph, Gender gender) : base(foreground, glyph, gender) { }
    }
}
