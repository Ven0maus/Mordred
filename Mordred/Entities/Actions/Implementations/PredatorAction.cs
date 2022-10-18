using Mordred.Entities.Animals;
using Mordred.Entities.Tribals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities.Actions.Implementations
{
    /// <summary>
    /// Action for a predator animal to hunt for food
    /// </summary>
    public class PredatorAction : BaseAction, ICombatAction
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
            if (actor is not PredatorAnimal predator) 
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
                        if (animal.HasActionOfType<PredatorAction>()) continue;

                        animal.CurrentAction?.Cancel();
                        animal.AddAction(new PredatorAction(_currentPrey, predator.TimeBetweenAttacksInTicks), true, true);
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

                // 35% chance to stun prey for 3 to 8 game ticks
                if (_currentPrey.Alive && Game.Random.Next(0, 100) < 40)
                {
                    bool preyIsStunned = false;
                    if (_currentPrey.CurrentAction != null)
                    {
                        if (_currentPrey.CurrentAction is not StunAction)
                            _currentPrey.CurrentAction.Cancel();
                        else
                            preyIsStunned = true;
                    }

                    // Add stun action (3 to 8 game ticks)
                    if (!preyIsStunned)
                        _currentPrey.AddAction(new StunAction(Game.Random.Next(3, 8), false), true, false);
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
            if (!predator.CanMoveTowards(_currentPrey.WorldPosition.X, _currentPrey.WorldPosition.Y, out PathFinding.CustomPath path))
            {
                validPath = false;
                return false;
            }

            if (predator.WorldPosition.SquaredDistance(_currentPrey.WorldPosition) < 2)
            {
                validPath = true;
                return true;
            }
            else if (predator.WorldPosition == _currentPrey.WorldPosition || !predator.MoveTowards(path))
            {
                validPath = true;
                return true;
            }
            validPath = true;
            return false;
        }

        public static bool PreyExistsNearby(PredatorAnimal predator)
        {
            // Find a body in the world that is dead but not rotten
            var actors = EntitySpawner.Entities.OfType<Actor>();
            var actor = actors
                .Where(a => !a.Alive && !a.Rotting && !a.SkeletonDecaying)
                .FirstOrDefault();
            if (actor != null) return true;

            // No body found?: then find the animal with lower or equal health than the predator
            var predatorType = predator.GetType();
            actor = actors
                .Where(a => a.Alive && a is Animal && a.GetType() != predatorType)
                .Where(a =>
                {
                    // Predator animals should not go after stronger predators
                    if (a is PredatorAnimal && a.Health > predator.MaxHealth)
                    {
                        // Unless they are in a pack and have more numbers than the hunted
                        if (predator is IPackAnimal hunterPa && hunterPa.PackMates.Count > 0)
                        {
                            if (a is IPackAnimal huntedPa && huntedPa.PackMates.Count >= hunterPa.PackMates.Count)
                                return false;
                            return true;
                        }
                        return false;
                    }
                    return true;
                })
                .FirstOrDefault();
            if (actor != null) return true;

            // No animal found?: find nearest human
            actor = actors
                .Where(a => a is Human)
                .FirstOrDefault();
            return actor != null;
        }

        public Actor FindPreyTarget(PredatorAnimal predator)
        {
            // Find a body in the world that is dead but not rotten
            var actors = EntitySpawner.Entities.OfType<Actor>();
            var actor = actors
                .Where(a => !a.Alive && !a.Rotting && !a.SkeletonDecaying && !_badPrey.Contains(a))
                .OrderBy(a => a.WorldPosition
                    .SquaredDistance(predator.WorldPosition))
                .FirstOrDefault();
            if (actor != null) return actor;

            // No body found?: then find the animal with lower or equal health than the predator
            var predatorType = predator.GetType();
            actor = actors
                .Where(a => a.Alive && a is Animal && a.GetType() != predatorType)
                .Where(a => 
                {
                    // Predator animals should not go after stronger predators
                    if (a is PredatorAnimal && a.Health > predator.MaxHealth)
                    {
                        // Unless they are in a pack and have more numbers than the hunted
                        if (predator is IPackAnimal hunterPa && hunterPa.PackMates.Count > 0)
                        {
                            if (a is IPackAnimal huntedPa && huntedPa.PackMates.Count >= hunterPa.PackMates.Count)
                                return false;
                            return true;
                        }
                        return false;
                    }
                    return true;
                })
                .Where(a => !_badPrey.Contains(a))
                .OrderBy(a => a.WorldPosition
                    .SquaredDistance(predator.WorldPosition))
                .FirstOrDefault();
            if (actor != null) return actor;

            // No animal found?: find nearest human
            actor = actors
                .Where(a => a is Human)
                .OrderBy(a => a.WorldPosition
                    .SquaredDistance(predator.WorldPosition))
                .FirstOrDefault();
            return actor;
        }
    }
}
