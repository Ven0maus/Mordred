using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
using System;

namespace Mordred.Entities.Animals
{
    public class Deer : Animal
    {
        public Deer(Coord position) : base(Color.SaddleBrown, 'D') 
        {
            Position = position;
            HungerTickRate = 2;
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
