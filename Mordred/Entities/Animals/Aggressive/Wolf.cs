using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
using System;

namespace Mordred.Entities.Animals
{
    public class Wolf : PredatorAnimal
    {
        public Wolf(Coord position) : base(Color.LightSlateGray, 'w')
        {
            Position = position;
            HungerTickRate = 4;
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
