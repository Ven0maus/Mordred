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
        public virtual int Health { get; private set; }
        public virtual int Hunger { get; private set; }
        public bool Alive { get { return Health > 0; } }

        public int HungerTickRate = Constants.ActorSettings.DefaultHungerTickRate;
        private int _hungerTicks = 0;
        #endregion

        public Actor(Color foreground, Color background, int glyph, int health = 100) : base(foreground, background, glyph)
        {
            Hunger = Constants.ActorSettings.DefaultMaxHunger;
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
            if (MapConsole.World.GetCell(nextStep.X, nextStep.Y).Walkable)
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

            // Actor died
            if (Health <= 0)
            {
                // Unset actions
                Hunger = 0;
                _actorActionsQueue.Clear();
                CurrentAction = null;

                // Remove from the map and unsubscribe from events
                Game.GameTick -= GameTick;
                Game.GameTick -= HandleActions;

                // Start decay process
                Game.GameTick += StartActorDecayProcess;

                // Trigger death event
                OnActorDeath?.Invoke(this, attacker);
            }
        }

        private int _rottingCounter = 0;
        private bool _corpseRotting = false;
        private bool _skeletonDecaying = false;
        private void StartActorDecayProcess(object sender, EventArgs args)
        {
            // Initial freshness of the corpse
            int ticksToRot = _skeletonDecaying ? (Constants.ActorSettings.TicksBeforeCorpseRots * 2) : Constants.ActorSettings.TicksBeforeCorpseRots;
            if (_rottingCounter < ticksToRot)
            {
                _rottingCounter++;
                return;
            }

            if (!_corpseRotting)
            {
                // Turn corpse red for another half the amount of ticks
                Name += "(rotting)";
                Animation[0].Foreground = Color.Lerp(Animation[0].Foreground, Color.Red, 0.7f);

                _rottingCounter = Constants.ActorSettings.TicksBeforeCorpseRots / 2;
                _corpseRotting = true;
                return;
            }

            if (!_skeletonDecaying)
            {
                // Change this actor to a skeleton
                Name = Name.Replace("(rotting)", "(skeleton)");
                Animation[0].Foreground = Color.Lerp(Color.GhostWhite, Color.Black, 0.2f);

                _rottingCounter = 0;
                _skeletonDecaying = true;
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
            Debug.WriteLine("[" + GetType().Name + "] just ate ["+ edible.Name +"] for: " + (int)Math.Round(amount * edible.EdibleWorth) + " hunger value.");
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

                if (Hunger <= 40 && !HasActionOfType<EatAction>() && !HasActionOfType<PredatorAction>())
                {
                    if (CurrentAction is WanderAction wAction)
                        wAction.Cancel();
                    if (this is PredatorAnimal)
                        AddAction(new PredatorAction(), true);
                    else
                        AddAction(new EatAction(), true);
                }
            }
            _hungerTicks++;
        }

        private bool HasActionOfType<T>() where T : IAction
        {
            if (_actorActionsQueue.Any(a => a.GetType() == typeof(T))) return true;
            if (CurrentAction != null && CurrentAction.GetType() == typeof(T)) return true;
            return false;
        }
    }
}
