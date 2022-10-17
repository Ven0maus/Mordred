using SadRogue.Primitives;

namespace Mordred.Entities.Animals.Passive
{
    public class Cow : PassiveAnimal
    {
        public Cow(Point position, Gender gender) : base(Color.PapayaWhip, 'C', gender, 80)
        {
            WorldPosition = position;
        }
    }
}
