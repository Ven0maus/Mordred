using Mordred.Entities.Actions;
using Mordred.Entities.Actions.Implementations;
using Mordred.Entities.Animals;
using Mordred.GameObjects.Effects;
using Mordred.GameObjects.ItemInventory;
using Mordred.GameObjects.ItemInventory.Items;
using Mordred.Graphics.Consoles;
using SadConsole.Entities;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mordred.Entities
{
    public abstract class Actor : Entity, IEntity, IEquatable<Entity>, IEquatable<Actor>
    {
        public Inventory Inventory { get; private set; }
        public IAction CurrentAction { get; private set; }
        private Queue<IAction> _actorActionsQueue;

        public event EventHandler<Actor> OnActorDeath;

        private Point _worldPosition;
        public Point WorldPosition
        {
            get { return _worldPosition; }
            set 
            { 
                _worldPosition = value;
                IsVisible = MapConsole.World.IsWorldCoordinateOnViewPort(_worldPosition.X, _worldPosition.Y);
                if (IsVisible)
                    Position = MapConsole.World.WorldToScreenCoordinate(_worldPosition.X, _worldPosition.Y);
            }
        }

        private readonly PathFinding _pathfinder;

        #region Actor stats
        public virtual int MaxHealth { get; private set; }
        public virtual int Health { get; private set; }

        public readonly int MaxHunger;
        public int Hunger { get; protected set; }
        public bool Alive { get { return Health > 0; } }
        public bool Bleeding { get; protected set; }

        public bool Rotting { get; private set; } = false;
        public bool SkeletonDecaying { get; private set; } = false;

        private int _hungerTicks = 0, _healthRegenTicks = 0, _bleedingCounterTicks = 0, _bleedingForTicks = 0;

        public int CarcassFoodPercentage = 100;

        private int _combatTimer = 0;

        public bool InCombat { get { return CurrentAttacker != null || _combatTimer != 0; } }

        public Actor CurrentAttacker { get; set; }
        #endregion

        public Actor(Color foreground, Color background, int glyph, int health = 100) : base(foreground, background, glyph, 1)
        {
            Name = GetType().Name;

            MaxHunger = Constants.ActorSettings.DefaultMaxHunger;
            Hunger = Game.Random.Next(50, MaxHunger + 1);

            MaxHealth = health;
            Health = health;

            _actorActionsQueue = new Queue<IAction>();
            _pathfinder = new PathFinding(this, Constants.ActorSettings.PathingWidth, Constants.ActorSettings.PathingHeight);
            Inventory = new Inventory();

            // Subscribe to the game tick event
            Game.GameTick += GameTick;
            Game.GameTick += HandleActions;
        }

        protected static bool IsOnScreen(Point position)
        {
            return MapConsole.World.IsWorldCoordinateOnViewPort(position.X, position.Y);
        }

        public void AddAction(IAction action, bool prioritize = false, bool addDuplicateTask = true)
        {
            if (!Alive) return;
            if (!addDuplicateTask && _actorActionsQueue.Any(a => a.GetType() == action.GetType())) return;
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

        public bool CanMoveTowards(int x, int y, out PathFinding.CustomPath path)
        {
            path = null;
            if (!Alive) return false;
            path = _pathfinder.ShortestPath(WorldPosition, (x, y));
            return path != null;
        }

        public bool MoveTowards(PathFinding.CustomPath path)
        {
            if (path == null || !Alive) return false;

            var nextStep = path.TakeStep();
            if (nextStep == null) return false;
            if (MapConsole.World.CellWalkable(nextStep.Value.X, nextStep.Value.Y))
            {
                WorldPosition = nextStep.Value;
                IsVisible = MapConsole.World.IsWorldCoordinateOnViewPort(WorldPosition.X, WorldPosition.Y);
                if (IsVisible)
                {
                    Position = MapConsole.World.WorldToScreenCoordinate(WorldPosition.X, WorldPosition.Y);
                }
                return true;
            }
            return false;
        }

        public bool MoveTowards(int x, int y)
        {
            if (!Alive) return false;
            var movementPath = _pathfinder.ShortestPath(WorldPosition, (x, y));
            return MoveTowards(movementPath);
        }

        private void HandleActions(object sender, EventArgs args)
        {
            // Handle in combat checker
            if (CurrentAttacker != null)
            {
                if (_combatTimer < (Constants.ActorSettings.HowLongInCombatInSeconds * Game.TicksPerSecond))
                {
                    _combatTimer++;
                }
                else
                {
                    _combatTimer = 0;
                    CurrentAttacker = null;
                }
            }

            // TODO: Investigate if we should somehow introduce threading for actions?
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

        protected virtual void OnAttacked(int damage, Actor attacker)
        {
            _combatTimer = 0;
        }

        public virtual void DealDamage(int damage, Actor attacker)
        {
            if (!Alive) return;
            Health -= damage;

            // Handle attacker assignment
            if (!attacker.Equals(this))
            {
                if (Alive)
                    CurrentAttacker = attacker;
                else
                    CurrentAttacker = null;

                if (attacker is PredatorAnimal predator)
                {
                    if (Alive)
                        predator.CurrentlyAttacking = this;
                    else
                        predator.CurrentlyAttacking = null;
                }
            }

            // Handle bleeding effect
            if (!Bleeding && !attacker.Equals(this))
            {
                Bleeding = Game.Random.Next(0, 100) < Constants.ActorSettings.BleedChanceFromAttack;
            }

            // Actor died
            if (Health <= 0)
            {
                // Unset actions
                _actorActionsQueue.Clear();
                CurrentAction = null;

                // Remove from the map and unsubscribe from events
                UnSubscribe();

                // Make sure we re-assign the leader
                ReAssignPackLeader();

                // Start decay process
                Game.GameTick += StartActorDecayProcess;

                // Add corpse to the name, to make it more clear
                Name += "(corpse)";

                // Trigger death event
                OnActorDeath?.Invoke(this, attacker);

                string causeOfDeath = "";
                bool diedBySelf = attacker.Equals(this);
                if (diedBySelf)
                {
                    if (Hunger <= 0 && !Bleeding)
                        causeOfDeath = "Starvation";
                    else if (Hunger > 0 && Bleeding)
                        causeOfDeath = "Bleeding";
                    else if (Hunger <= 0 && Bleeding)
                        causeOfDeath = "Mix of bleeding and starvation";
                    else if (InCombat)
                        causeOfDeath = "Combat";
                    else
                        causeOfDeath = "Unknown cause of death.";
                }

                // Debugging
                Debug.WriteLine(Name + " has died from: " + (diedBySelf ? causeOfDeath : attacker.Name));
            }

            // Call virtual method
            OnAttacked(damage, attacker);
        }

        public void UnSubscribe()
        {
            Game.GameTick -= GameTick;
            Game.GameTick -= HandleActions;
        }

        private void ReAssignPackLeader()
        {
            if (Alive) return;
            if (this is IPackAnimal packAnimal && packAnimal.Leader.Equals(this))
            {
                var newLeader = packAnimal.PackMates.TakeRandom();
                foreach (var packMate in packAnimal.PackMates)
                {
                    packMate.Leader = newLeader;
                }
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
            // Initial freshness of the corpse, skeleton decay takes twice as long as rotting
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
                Appearance.Foreground = Color.Lerp(Appearance.Foreground, Color.Red, 0.7f);
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
                Appearance.Foreground = Color.Lerp(Color.GhostWhite, Color.Black, 0.2f);
                IsDirty = true;

                _rottingCounter = 0;
                SkeletonDecaying = true;
                Rotting = false;
                Debug.WriteLine("[" + Name + "] just started bone decaying.");
                return;
            }

            // Unset tick event
            DestroyCarcass();
        }

        public virtual void Eat(EdibleItem edible, int amount)
        {
            if (!Alive) return;
            Hunger += (int)Math.Round(amount * edible.EdibleWorth);
            if (Constants.GameSettings.DebugMode)
                Debug.WriteLine("[" + Name + "] just ate ["+ edible.Name +"] for: " + (int)Math.Round(amount * edible.EdibleWorth) + " hunger value.");
        }

        /// <summary>
        /// This method is called every game tick, use this to execute actor logic that should be tick based
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected virtual void GameTick(object sender, EventArgs args)
        {
            if (_hungerTicks >= (Constants.ActorSettings.DefaultHungerTickRateInSeconds * Game.TicksPerSecond))
            {
                _hungerTicks = 0;
                if (Hunger > 0)
                    Hunger--;
                else
                    DealDamage(2, this);

                // Health regeneration rate when not in combat
                if (_healthRegenTicks >= (Constants.ActorSettings.DefaultHealthRegenTickRateInSeconds * Game.TicksPerSecond) && CurrentAction is not ICombatAction && Health < MaxHealth)
                {
                    _healthRegenTicks = 0;
                    var minHungerPercentage = (int)((float)MaxHunger / 100 * Constants.ActorSettings.DefaultPercentageHungerHealthRegen);
                    if (Hunger >= minHungerPercentage)
                        Health++;
                }

                if (Hunger <= (int)((float)MaxHunger / 100 * Constants.ActorSettings.LookForFoodAtHungerPercentage) && 
                    !HasActionOfType<EatAction>() && !HasActionOfType<PredatorAction>())
                {
                    if (this is PredatorAnimal predator)
                    {
                        if (PredatorAction.PreyExistsNearby(predator))
                        {
                            if (CurrentAction is WanderAction || CurrentAction is FollowPackLeaderAction)
                                CurrentAction.Cancel();

                            AddAction(new PredatorAction(predator.TimeBetweenAttacksInTicks), true);
                            if (Constants.GameSettings.DebugMode)
                                Debug.WriteLine("Added PredatorAction for: " + Name);
                        }
                    }
                    else
                    {
                        if (CurrentAction is WanderAction || CurrentAction is FollowPackLeaderAction)
                            CurrentAction.Cancel();

                        AddAction(new EatAction(), true);
                    }
                }
            }

            if (Bleeding && Alive)
            {
                _bleedingForTicks++;
                if (_bleedingForTicks >= Constants.ActorSettings.StopBleedingAfterSeconds * Game.TicksPerSecond)
                {
                    Bleeding = false;
                    _bleedingForTicks = 0;
                    _bleedingCounterTicks = 0;
                    return;
                }

                if (_bleedingCounterTicks < Constants.ActorSettings.DefaultSecondsPerBleeding * Game.TicksPerSecond)
                {
                    _bleedingCounterTicks++;
                    return;
                }
                int bleedDamage = (int)Math.Ceiling((double)Health / 100 * 10);

                Debug.WriteLine($"{Name}: just bled for {bleedDamage} damage. Only {Health} health remains.");

                DealDamage(bleedDamage, this);
                AddBleedEffect();

                _bleedingCounterTicks = 0;
            }

            _hungerTicks++;
            _healthRegenTicks++;
        }

        private void AddBleedEffect()
        {
            // Add bleed effect to some random cells
            var cellsToApplyBleedEffectTo = WorldPosition.Get8Neighbors()
                .TakeRandom(Game.Random.Next(1, 4))
                .Append(WorldPosition);
            foreach (var neighbor in cellsToApplyBleedEffectTo)
                MapConsole.World.AddEffect(new Bleed(neighbor, Game.Random.Next(4, 7)));
        }

        public bool HasActionOfType<T>() where T : IAction
        {
            if (_actorActionsQueue.Any(a => a.GetType() == typeof(T))) return true;
            if (CurrentAction != null && CurrentAction.GetType() == typeof(T)) return true;
            return false;
        }

        public bool Equals(Entity other)
        {
            return other != null && Position.Equals(other.Position);
        }

        public bool Equals(Actor other)
        {
            return other != null && Position.Equals(other.Position);
        }
    }
}
