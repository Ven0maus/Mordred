using GoRogue;
using Microsoft.Xna.Framework;
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

        public Wolf(Coord position, Gender gender) : base(Color.LightSlateGray, 'w', gender, 65)
        {
            PackMates = new List<Wolf>();
            Position = position;
            HungerTickRate = 7;
        }
    }
}
