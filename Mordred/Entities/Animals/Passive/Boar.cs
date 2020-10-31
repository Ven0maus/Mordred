using GoRogue;
using Microsoft.Xna.Framework;

namespace Mordred.Entities.Animals.Passive
{
    public class Boar : PassiveAnimal
    {
        public Boar(Coord position, Gender gender) : base(Color.DarkOrchid, 'b', gender)
        {
            Position = position;
            HungerTickRate = 8;
        }
    }
}
