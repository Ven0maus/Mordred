using SadRogue.Primitives;

namespace Mordred.Entities.Animals.Aggressive
{
    public class Leopard : PredatorAnimal
    {
        public Leopard(Point position, Gender gender) : base(Color.YellowGreen, 'L', gender, 125)
        {
            Position = position;
            HungerTickRate = 10;
            AttackDamage = 25;
        }
    }
}
