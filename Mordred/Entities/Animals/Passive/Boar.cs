using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
using System;

namespace Mordred.Entities.Animals.Passive
{
    public class Boar : PassiveAnimal
    {
        public Boar(Coord position, Gender gender) : base(Color.DarkOrchid, 'b', gender)
        {
            Position = position;
            HungerTickRate = 8;
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
