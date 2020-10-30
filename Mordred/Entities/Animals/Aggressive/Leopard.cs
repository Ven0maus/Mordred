using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
using System;

namespace Mordred.Entities.Animals.Aggressive
{
    public class Leopard : PredatorAnimal
    {
        public Leopard(Coord position, Gender gender) : base(Color.YellowGreen, 'L', gender)
        {
            Position = position;
            HungerTickRate = 4;
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
