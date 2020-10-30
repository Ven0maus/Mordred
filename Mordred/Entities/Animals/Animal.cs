using Microsoft.Xna.Framework;

namespace Mordred.Entities.Animals
{
    public enum Gender
    {
        Male,
        Female
    }

    public abstract class Animal : Actor
    {
        public readonly Gender Gender;
        public Animal(Color foreground, int glyph, Gender gender) : base(foreground, Color.Black, glyph) => Gender = gender;
    }
}
