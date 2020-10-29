using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
using System;
using System.Collections.Generic;

namespace Mordred.Entities.Animals
{
    public class Sheep : PassiveAnimal, IPackAnimal<Sheep>
    {
        public List<Sheep> PackMates { get; set; }

        public Sheep(Coord position) : base(Color.PapayaWhip, 'S')
        {
            PackMates = new List<Sheep>();
            Position = position;
            HungerTickRate = 6;
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
