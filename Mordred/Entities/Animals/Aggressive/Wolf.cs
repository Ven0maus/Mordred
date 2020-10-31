using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
using System;
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

        public Wolf(Coord position, Gender gender) : base(Color.LightSlateGray, 'w', gender)
        {
            PackMates = new List<Wolf>();
            Position = position;
            HungerTickRate = 3;
        }

        protected override void GameTick(object sender, EventArgs args)
        {
            base.GameTick(sender, args);

            if (Health <= 0) return;
            if (CurrentAction == null && !HasActionOfType<WanderAction>())
            {
                AddAction(new WanderAction());
            }
        }
    }
}
