using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
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

        public PredatorAnimal(Color foreground, int glyph, Gender gender, int health = 100) : base(foreground, glyph, gender, health) 
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

        private int _lastWanderTickCounter = 0;
        private bool _canWander = true;
        protected override void GameTick(object sender, EventArgs args)
        {
            base.GameTick(sender, args);

            if (Health <= 0) return;

            if (this is IPackAnimal packAnimal)
            {
                if (!packAnimal.Leader.Equals(this))
                {
                    if (!HasActionOfType<FollowPackLeaderAction>())
                    {
                        AddAction(new FollowPackLeaderAction(), false, false);
                        return;
                    }
                }
            }

            if (_canWander && _lastWanderTickCounter <= 0 && !HasActionOfType<WanderAction>())
            {
                _canWander = false;

                var wanderAction = new WanderAction();
                wanderAction.ActionCompleted += RestWanderEvent;
                wanderAction.ActionCanceled += RestWanderEvent;

                AddAction(wanderAction, false, false);
            }

            if (_lastWanderTickCounter > 0)
                _lastWanderTickCounter--;
        }

        private void RestWanderEvent(object sender, Actor actor)
        {
            _canWander = true;
            _lastWanderTickCounter = Game.TicksPerSecond * Game.Random.Next(1, 5);
        }
    }
}
