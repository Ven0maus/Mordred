using SadRogue.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities.Animals
{
    public class Sheep : PassiveAnimal, IPackAnimal<Sheep>
    {
        public List<Sheep> PackMates { get; set; }

        List<IPackAnimal> IPackAnimal.PackMates
        {
            get
            {
                return PackMates?.OfType<IPackAnimal>().ToList();
            }
            set
            {
                PackMates = new List<Sheep>();
                PackMates.AddRange(value.Select(a => (Sheep)a));
            }
        }

        public IPackAnimal Leader { get; set; }

        public Sheep(Point position, Gender gender) : base(Color.Plum, 'S', gender, 65)
        {
            PackMates = new List<Sheep>();
            WorldPosition = position;
            HungerTickRate = 6;
        }
    }
}
