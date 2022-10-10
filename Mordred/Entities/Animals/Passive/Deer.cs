using SadRogue.Primitives;

namespace Mordred.Entities.Animals
{
    public class Deer : PassiveAnimal
    {
        public Deer(Point position, Gender gender) : base(Color.SaddleBrown, 'D', gender, 80) 
        {
            Position = position;
            HungerTickRate = 6;
        }
    }
}
