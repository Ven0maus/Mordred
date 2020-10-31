using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
using System;

namespace Mordred.Entities.Animals
{
    public abstract class PassiveAnimal : Animal
    {
        public PassiveAnimal(Color foreground, int glyph, Gender gender) : base(foreground, glyph, gender) { }

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
