using GoRogue;
using Microsoft.Xna.Framework;

namespace Mordred.Entities.Animals.Passive
{
    public class Cow : PassiveAnimal
    {
        public Cow(Coord position, Gender gender) : base(Color.PapayaWhip, 'C', gender, 80)
        {
            Position = position;
            HungerTickRate = 10;
        }
    }
}
