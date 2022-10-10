using SadRogue.Primitives;

namespace Mordred.Entities.Animals.Passive
{
    public class Boar : PassiveAnimal
    {
        public Boar(Point position, Gender gender) : base(Color.LightSeaGreen, 'b', gender, 75)
        {
            Position = position;
            HungerTickRate = 8;
        }
    }
}
