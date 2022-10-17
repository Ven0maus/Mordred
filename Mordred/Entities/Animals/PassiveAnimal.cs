using Mordred.Entities.Actions.Implementations;
using SadRogue.Primitives;
using System;
using System.Diagnostics;
using System.Linq;

namespace Mordred.Entities.Animals
{
    public abstract class PassiveAnimal : Animal
    {
        public PassiveAnimal(Color foreground, int glyph, Gender gender, int health = 100) : base(foreground, glyph, gender, health) { }

        protected override void OnAttacked(int damage, Actor attacker)
        {
            if (!Alive || attacker.Equals(this)) return;
            if (Game.Random.Next(0, 100) < 15 && !HasActionOfType<DefendAction>())
            {
                if (CurrentAction != null)
                    CurrentAction.Cancel();
                AddAction(new DefendAction(), true, false);

                // Let pack know who to attack
                if (this is IPackAnimal packAnimal)
                {
                    foreach (var packMate in packAnimal.PackMates.OfType<PassiveAnimal>())
                    {
                        if (!packMate.HasActionOfType<DefendAction>())
                        {
                            if (packMate.CurrentAction != null)
                                packMate.CurrentAction.Cancel();
                            packMate.AddAction(new DefendAction(this), true, false);
                        }
                    }
                }
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
