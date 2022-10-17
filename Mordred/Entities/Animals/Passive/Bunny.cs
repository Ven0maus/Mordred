using SadRogue.Primitives;

namespace Mordred.Entities.Animals
{
    public class Bunny : PassiveAnimal
    {
        public Bunny(Point position, Gender gender) : base(Color.Aquamarine, 'B', gender, 25)
        {
            WorldPosition = position;
        }
    }
}
