using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
using System;

namespace Mordred.Entities.Animals
{
    public class Deer : PassiveAnimal
    {
        public Deer(Coord position, Gender gender) : base(Color.SaddleBrown, 'D', gender) 
        {
            Position = position;
            HungerTickRate = 6;
        }

        protected override void GameTick(object sender, EventArgs args)
        {
            base.GameTick(sender, args);

            if (Health <= 0) return;
            if (CurrentAction == null && !HasActionOfType<WanderAction>())
            {
                AddAction(new WanderAction(), false, false);
            }
        }
    }
}
