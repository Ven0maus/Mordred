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

        List<Animal> IPackAnimal.PackMates => PackMates.OfType<Animal>().ToList();

        public Wolf(Coord position) : base(Color.LightSlateGray, 'w')
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
