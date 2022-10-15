using SadRogue.Primitives;
using System;

namespace Mordred.Entities.Animals.Aggressive
{
    public class Snake : PredatorAnimal
    {
        public Snake(Point position, Gender gender) : base(Color.DarkKhaki, 's', gender, 45)
        {
            WorldPosition = position;
            HungerTickRate = 13;
            AttackDamage = 8;
            TimeBetweenAttacksInTicks = (int)Math.Ceiling(Game.TicksPerSecond / 2f);
        }
    }
}
