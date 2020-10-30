using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;

namespace Mordred.Entities.Animals
{
    public abstract class PredatorAnimal : Animal
    {
        public int AttackDamage = 15;

        /// <summary>
        /// Default 2 seconds
        /// </summary>
        public int TimeBetweenAttacksInTicks;

        public PredatorAnimal(Color foreground, int glyph, Gender gender) : base(foreground, glyph, gender) 
        {
            int ticksPerSecond = (int)Math.Round(1f / Constants.GameSettings.TimePerTickInSeconds);
            TimeBetweenAttacksInTicks = 2 * ticksPerSecond;
        }

        public void Eat(Actor prey)
        {
            if (prey.CarcassFoodPercentage == 0)
            {
                prey.DestroyCarcass();
                return;
            }

            // We use the prey's hunger (feeded) value to determine how much it is worth
            float foodWorth = (prey.Hunger / 100f) * 2.85f;
            int percentageToFood = (int)Math.Round(prey.CarcassFoodPercentage * foodWorth);
            int foodRequirement = MaxHunger - Hunger;
            int totalEaten = Hunger;
            if (percentageToFood >= foodRequirement)
            {
                percentageToFood -= foodRequirement;
                Hunger += foodRequirement;
            }
            else
            {
                Hunger += percentageToFood;
                percentageToFood = 0;
            }
            totalEaten = Hunger - totalEaten;

            Debug.WriteLine($"{Name} just ate a {prey.Name} for {totalEaten} hunger value.");

            int foodToPercentage = (int)Math.Round((float)percentageToFood / foodWorth);
            prey.CarcassFoodPercentage = foodToPercentage;

            Debug.WriteLine($"Only {prey.CarcassFoodPercentage} remains of the {prey.Name}.");
            if (prey.CarcassFoodPercentage <= 0)
            {
                prey.CarcassFoodPercentage = 0;
                prey.DestroyCarcass();
            }
        }
    }
}
