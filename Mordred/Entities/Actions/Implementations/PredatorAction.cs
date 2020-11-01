using GoRogue;
using Mordred.Entities.Animals;
using Mordred.Entities.Tribals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mordred.Entities.Actions.Implementations
{
    /// <summary>
    /// Action for a predator animal to hunt for food
    /// </summary>
    public class PredatorAction : BaseAction
    {
        public override event EventHandler<Actor> ActionCompleted;

        private readonly bool _isPartOfPack = false;
        private Actor _currentPrey;
        private readonly List<Actor> _badPrey; // Cannot reach path

        private readonly int _ticksBetweenPredatorAttacks;
        private int _ticksBetweenLastAttack;

        /// <summary>
        /// Use this for pack hunting, they will not eat the carcass, but let the one who triggered the action eat the carcass
        /// </summary>
        /// <param name="prey"></param>
        /// <param name="timeBetweenPredatorAttacks"></param>
        /// <param name="inSeconds"></param>
        protected PredatorAction(Actor prey, int timeBetweenPredatorAttacks, bool inSeconds = true) : this(timeBetweenPredatorAttacks, inSeconds)
        {
            _currentPrey = prey;
            _isPartOfPack = true;
        }

        /// <summary>
        /// Trigger this by the main animal, pack animal's will follow
        /// </summary>
        /// <param name="timeBetweenPredatorAttacks"></param>
        /// <param name="inSeconds"></param>
        public PredatorAction(int timeBetweenPredatorAttacks, bool inSeconds = true)
        {
            if (inSeconds)
            {
                float ticksPerSecond = 1f / Constants.GameSettings.TimePerTickInSeconds;
                _ticksBetweenPredatorAttacks = (int)Math.Round(ticksPerSecond * timeBetweenPredatorAttacks);
            }
            else
            {
                _ticksBetweenPredatorAttacks = timeBetweenPredatorAttacks;
            }

            _ticksBetweenLastAttack = _ticksBetweenPredatorAttacks;
            _badPrey = new List<Actor>();
        }

        public override bool Execute(Actor actor)
        {
            if (base.Execute(actor)) return true;

            // This action cannot be assigned to actor's that aren't of type PredatorAnimal
            if (!(actor is PredatorAnimal predator)) 
            { 
                Cancel();
                return false;
            }

            // Find a suitable prey
            if (_currentPrey == null)
            {
                if (_isPartOfPack)
                {
                    ActionCompleted?.Invoke(this, actor);
                    return true;
                }

                _currentPrey = FindPreyTarget(predator);
                if (_currentPrey == null)
                {
                    ActionCompleted?.Invoke(this, actor);
                    return true;
                }

                // Make pack animals hunt the same target together
                if (predator is IPackAnimal packAnimal)
                {
                    foreach (var animal in packAnimal.PackMates.OfType<Animal>())
                    {
                        animal.AddAction(new PredatorAction(_currentPrey, predator.TimeBetweenAttacksInTicks), true, true);
                        Debug.WriteLine("Added pack PredatorAction for: " + animal.Name);
                    }
                }
            }

            // Locate and move towards the current prey carcass
            if (!_currentPrey.Alive && !_currentPrey.Rotting && !_currentPrey.SkeletonDecaying)
            {
                // Move towards prey carcass
                if (!MoveTowardsPrey(predator, out bool validPath))
                {
                    if (!validPath)
                    {
                        _badPrey.Add(_currentPrey);
                        _currentPrey = null;
                    }
                    return false;
                }

                // Only the animal that triggered the hunt can eat the carcass
                if (!_isPartOfPack)
                {
                    // We have reached the entity, eat it's carcass
                    predator.Eat(_currentPrey);
                }

                ActionCompleted?.Invoke(this, predator);
                return true;
            }
            else if (_currentPrey.Alive)
            {
                // Move towards prey carcass
                if (!MoveTowardsPrey(predator, out bool validPath))
                {
                    if (!validPath)
                    {
                        _badPrey.Add(_currentPrey);
                        _currentPrey = null;
                    }
                    return false;
                }

                if (_ticksBetweenLastAttack < _ticksBetweenPredatorAttacks)
                {
                    _ticksBetweenLastAttack++;
                    return false;
                }
                _ticksBetweenLastAttack = 0;

                // We have reached the entity, attack the entity
                _currentPrey.DealDamage(predator.AttackDamage, predator);
                Debug.WriteLine($"{predator.Name} just attacked {_currentPrey.Name} for {predator.AttackDamage}");

                // Stun the prey by 1 second
                if (_currentPrey.Alive)
                {
                    bool preyIsStunned = false;
                    if (_currentPrey.CurrentAction != null)
                    {
                        if (!(_currentPrey.CurrentAction is StunAction))
                            _currentPrey.CurrentAction.Cancel();
                        else
                            preyIsStunned = true;
                    }

                    // Add stun action
                    if (!preyIsStunned)
                        _currentPrey.AddAction(new StunAction(1), true, false);
                }
                return false;
            }
            else if (_currentPrey.Rotting || _currentPrey.SkeletonDecaying)
            {
                // Current prey has/is decayed
                _currentPrey = null;
                return false;
            }

            ActionCompleted?.Invoke(this, actor);
            return true;
        }

        private bool MoveTowardsPrey(PredatorAnimal predator, out bool validPath)
        {
            if (!predator.CanMoveTowards(_currentPrey.Position.X, _currentPrey.Position.Y, out CustomPath path))
            {
                validPath = false;
                return false;
            }

            if (predator.Position == _currentPrey.Position || !predator.MoveTowards(path))
            {
                validPath = true;
                return true;
            }
            else if (((Coord)predator.Position).SquaredDistance(_currentPrey.Position) < 2)
            {
                validPath = true;
                return true;
            }
            validPath = true;
            return false;
        }

        public Actor FindPreyTarget(PredatorAnimal predator)
        {
            // Find a body in the world that is dead but not rotten
            var actors = EntitySpawner.Entities.OfType<Actor>();
            var actor = actors
                .Where(a => !a.Alive && !a.Rotting && !a.SkeletonDecaying && !_badPrey.Contains(a))
                .OrderBy(a => ((Coord)a.Position)
                    .SquaredDistance(predator.Position))
                .FirstOrDefault();
            if (actor != null) return actor;

            // No body found?: then find the animal with lower or equal health than the predator
            var predatorType = predator.GetType();
            actor = actors
                .Where(a => a.Alive && a is Animal && a.GetType() != predatorType && a.Health <= predator.Health && !_badPrey.Contains(a))
                .OrderBy(a => ((Coord)a.Position)
                    .SquaredDistance(predator.Position))
                .FirstOrDefault();
            if (actor != null) return actor;

            // No animal found?: find nearest tribeman
            actor = actors
                .Where(a => a is Tribeman)
                .OrderBy(a => ((Coord)a.Position)
                    .SquaredDistance(predator.Position))
                .FirstOrDefault();
            return actor;
        }
    }
}
