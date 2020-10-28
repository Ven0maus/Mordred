using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
using System;

namespace Mordred.Entities.Animals
{
    public class Bunny : PassiveAnimal
    {
        public Bunny(Coord position) : base(Color.FloralWhite, 'B')
        {
            Position = position;
            HungerTickRate = 5;
        }

        protected override void GameTick(object sender, EventArgs args)
        {
            base.GameTick(sender, args);

            if (Health <= 0) return;
            if (CurrentAction == null)
            {
                AddAction(new WanderAction());
            }
        }
    }
}
