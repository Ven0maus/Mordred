using GoRogue;
using Microsoft.Xna.Framework;

namespace Mordred.Entities.Animals.Passive
{
    public class Boar : PassiveAnimal
    {
        public Boar(Coord position, Gender gender) : base(Color.LightSeaGreen, 'b', gender, 75)
        {
            Position = position;
            HungerTickRate = 8;
        }
    }
}
