using GoRogue;
using Microsoft.Xna.Framework;

namespace Mordred.Entities.Animals.Aggressive
{
    public class Leopard : PredatorAnimal
    {
        public Leopard(Coord position, Gender gender) : base(Color.YellowGreen, 'L', gender, 125)
        {
            Position = position;
            HungerTickRate = 10;
        }
    }
}
