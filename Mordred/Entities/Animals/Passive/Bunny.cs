using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
using System;

namespace Mordred.Entities.Animals
{
    public class Bunny : PassiveAnimal
    {
        public Bunny(Coord position, Gender gender) : base(Color.FloralWhite, 'B', gender)
        {
            Position = position;
            HungerTickRate = 7;
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
