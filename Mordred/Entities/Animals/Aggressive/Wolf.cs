using SadRogue.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities.Animals
{
    public class Wolf : PredatorAnimal, IPackAnimal<Wolf>
    {
        public List<Wolf> PackMates { get; set; }

        List<IPackAnimal> IPackAnimal.PackMates
        {
            get
            {
                return PackMates?.OfType<IPackAnimal>().ToList();
            }
            set
            {
                PackMates = new List<Wolf>();
                PackMates.AddRange(value.Select(a => (Wolf)a));
            }
        }

        public IPackAnimal Leader { get; set; }

        public Wolf(Point position, Gender gender) : base(Color.LightSlateGray, 'w', gender, 65)
        {
            PackMates = new List<Wolf>();
            WorldPosition = position;

            // Wolf stats
            AttackDamage = 8;
            TimeBetweenAttacksInTicks = Game.TicksPerSecond; // 1 second between attacks for wolfs
        }
    }
}
