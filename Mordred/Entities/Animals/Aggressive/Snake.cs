using GoRogue;
using Microsoft.Xna.Framework;
using System;

namespace Mordred.Entities.Animals.Aggressive
{
    public class Snake : PredatorAnimal
    {
        public Snake(Coord position, Gender gender) : base(Color.DarkKhaki, 's', gender, 45)
        {
            Position = position;
            HungerTickRate = 13;
            AttackDamage = 8;
            TimeBetweenAttacksInTicks = (int)Math.Ceiling(Game.TicksPerSecond / 2f);
        }
    }
}
