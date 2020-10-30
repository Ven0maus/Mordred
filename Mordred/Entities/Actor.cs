using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions;
using Mordred.Entities.Actions.Implementations;
using Mordred.Entities.Animals;
using Mordred.GameObjects.ItemInventory;
using Mordred.GameObjects.ItemInventory.Items;
using Mordred.Graphics.Consoles;
using SadConsole.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mordred.Entities
{
    public abstract class Actor : Entity
    {
        public Inventory Inventory { get; private set; }
        public IAction CurrentAction { get; private set; }
        private Queue<IAction> _actorActionsQueue;

        public event EventHandler<Actor> OnActorDeath;

        #region Actor stats
        public virtual int MaxHealth { get; private set; }
        public virtual int Health { get; private set; }

        public readonly int MaxHunger;
        public int Hunger { get; protected set; }
        public bool Alive { get { return Health > 0; } }
        public bool Bleeding { get; protected set; }

        public bool Rotting { get; private set; } = false;
        public bool SkeletonDecaying { get; private set; } = false;

        public int HungerTickRate = Constants.ActorSettings.DefaultHungerTickRate;
        public int HealthRegenerationTickRate = Constants.ActorSettings.DefaultHealthRegenerationTickRate;
        private int _hungerTicks = 0, _healthRegenTicks = 0, _bleedingCounterTicks = 0, _bleedingForTicks = 0;

        public int CarcassFoodPercentage = 100;
        #endregion

        public Actor(Color foreground, Color background, int glyph, int health = 100) : base(foreground, background, glyph)
        {
            Name = GetType().Name;

            MaxHunger = Constants.ActorSettings.DefaultMaxHunger;
            Hunger = MaxHunger;

            MaxHealth = health;
            Health = health;

            _actorActionsQueue = new Queue<IAction>();
            Inventory = new Inventory();

            // Subscribe to the game tick event
            Game.GameTick += GameTick;
            Game.GameTick += HandleActions;
        }

        public void AddAction(IAction action, bool prioritize = false)
        {
            if (!Alive) return;
            if (prioritize)
            {
                var l = _actorActionsQueue.ToList();
                l.Insert(0, action);
                _actorActionsQueue = new Queue<IAction>(l);
            }
            else
            {
                _actorActionsQueue.Enqueue(action);
            }
        }

        public bool CanMoveTowards(int x, int y, out CustomPath path)
        {
            path = null;
            if (!Alive) return false;
            path = MapConsole.World.Pathfinder.ShortestPath(Position, new Coord(x, y))?.ToCustomPath();
            return path != null;
        }

        public bool MoveTowards(CustomPath path)
        {
            if (path == null || !Alive) return false;

            var nextStep = path.TakeStep(0);
            if (MapConsole.World.CellWalkable(nextStep.X, nextStep.Y))
            {
                Position = nextStep;
                return true;
            }
            return false;
        }

        public bool MoveTowards(int x, int y)
        {
            if (!Alive) return false;
            var movementPath = MapConsole.World.Pathfinder.ShortestPath(Position, new Coord(x, y)).ToCustomPath();
            return MoveTowards(movementPath);
        }

        private void HandleActions(object sender, EventArgs args)
        {
            if (CurrentAction == null)
            {
                if (_actorActionsQueue.Count > 0)
                {
                    CurrentAction = _actorActionsQueue.Dequeue();
                }
                else
                {
                    return;
                }
            }

            if (CurrentAction.Execute(this))
            {
                CurrentAction = null;
            }
        }

        public virtual void DealDamage(int damage, Actor attacker)
        {
            if (!Alive) return;
            Health -= damage;

            if (!Bleeding && !attacker.Equals(this))
                Bleeding = Game.Random.Next(0, 100) < Constants.ActorSettings.BleedChanceFromAttack;

            // Actor died
            if (Health <= 0)
            {
                // Unset actions
                _actorActionsQueue.Clear();
                CurrentAction = null;

                // Remove from the map and unsubscribe from events
                Game.GameTick -= GameTick;
                Game.GameTick -= HandleActions;

                // Start decay process
                Game.GameTick += StartActorDecayProcess;

                // Trigger death event
                OnActorDeath?.Invoke(this, attacker);

                // Debugging
                Debug.WriteLine(Name + " has died from: " + (attacker.Equals(this) ? "self" : attacker.Name));
            }
        }

        public void DestroyCarcass()
        {
            Game.GameTick -= StartActorDecayProcess;
            EntitySpawner.Destroy(this);
        }

        private int _rottingCounter = 0;
        private void StartActorDecayProcess(object sender, EventArgs args)
        {
            // Add corpse to the name, to make it more clear
            Name += "(corpse)";

            // Initial freshness of the corpse
            int ticksToRot = SkeletonDecaying ? (Constants.ActorSettings.SecondsBeforeCorpsRots * 2 * Game.TicksPerSecond) : (Constants.ActorSettings.SecondsBeforeCorpsRots * Game.TicksPerSecond);
            if (_rottingCounter < ticksToRot)
            {
                _rottingCounter++;
                return;
            }

            if (!Rotting && !SkeletonDecaying)
            {
                // Turn corpse to rotting for the same amount of ticks as the freshness
                Name = Name.Replace("(corpse)", "(rotting)");
                Animation[0].Foreground = Color.Lerp(Animation[0].Foreground, Color.Red, 0.7f);
                Animation.IsDirty = true;
                IsDirty = true;

                _rottingCounter = 0;
                CarcassFoodPercentage = 0;
                Rotting = true;
                Debug.WriteLine("[" + Name + "] just started rotting.");
                return;
            }

            if (!SkeletonDecaying)
            {
                // Turn corpse to skeleton and start decay process which is 2x as long
                Name = Name.Replace("(rotting)", "(skeleton)");
                Animation[0].Foreground = Color.Lerp(Color.GhostWhite, Color.Black, 0.2f);
                Animation.IsDirty = true;
                IsDirty = true;

                _rottingCounter = 0;
                SkeletonDecaying = true;
                Rotting = false;
                Debug.WriteLine("[" + Name + "] just started bone decaying.");
                return;
            }

            // Unset tick event
            Game.GameTick -= StartActorDecayProcess;

            // Destroy the skeleton corpse
            EntitySpawner.Destroy(this);
        }

        public virtual void Eat(EdibleItem edible, int amount)
        {
            if (!Alive) return;
            Hunger += (int)Math.Round(amount * edible.EdibleWorth);
            Debug.WriteLine("[" + Name + "] just ate ["+ edible.Name +"] for: " + (int)Math.Round(amount * edible.EdibleWorth) + " hunger value.");
        }

        /// <summary>
        /// This method is called every game tick, use this to execute actor logic that should be tick based
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected virtual void GameTick(object sender, EventArgs args)
        {
            if (_hungerTicks >= HungerTickRate)
            {
                _hungerTicks = 0;
                if (Hunger > 0)
                    Hunger--;
                else
                    DealDamage(2, this);

                // Health regeneration rate
                if (_healthRegenTicks >= HealthRegenerationTickRate)
                {
                    _healthRegenTicks = 0;
                    if (Hunger >= (MaxHunger / 100 * Constants.ActorSettings.DefaultPercentageHungerHealthRegen) && Health < MaxHealth)
                        Health++;
                }

                if (Hunger <= (MaxHunger / 100 * 35) && !HasActionOfType<EatAction>() && !HasActionOfType<PredatorAction>())
                {
                    if (CurrentAction is WanderAction wAction)
                        wAction.Cancel();
                    if (this is PredatorAnimal predator)
                    {
                        AddAction(new PredatorAction(predator.TimeBetweenAttacksInTicks), true);
                        Debug.WriteLine("Added PredatorAction for: " + Name);
                    }
                    else
                    {
                        AddAction(new EatAction(), true);
                        Debug.WriteLine("Added EatAction for: " + Name);
                    }
                }
            }

            if (Bleeding && Alive)
            {
                // TODO: Add blood trail that disipates automatically after x seconds
                _bleedingForTicks++;
                if (_bleedingForTicks >= Constants.ActorSettings.StopBleedingAfterSeconds * Game.TicksPerSecond)
                {
                    Bleeding = false;
                    return;
                }

                if (_bleedingCounterTicks < Constants.ActorSettings.DefaultSecondsPerBleeding * Game.TicksPerSecond)
                {
                    _bleedingCounterTicks++;
                    return;
                }
                Debug.WriteLine($"{Name}: just bled for {Constants.ActorSettings.BleedingDamage} damage. Only {Health} health remains.");
                DealDamage(Constants.ActorSettings.BleedingDamage, this);
                _bleedingCounterTicks = 0;
            }

            _hungerTicks++;
            _healthRegenTicks++;
        }

        protected bool HasActionOfType<T>() where T : IAction
        {
            if (_actorActionsQueue.Any(a => a.GetType() == typeof(T))) return true;
            if (CurrentAction != null && CurrentAction.GetType() == typeof(T)) return true;
            return false;
        }
    }
}
