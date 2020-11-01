using GoRogue;
using Microsoft.Xna.Framework;

namespace Mordred.Entities.Animals
{
    public class Bunny : PassiveAnimal
    {
        public Bunny(Coord position, Gender gender) : base(Color.Aquamarine, 'B', gender, 25)
        {
            Position = position;
            HungerTickRate = 7;
        }
    }
}
