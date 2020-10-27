﻿using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions;
using Mordred.GameObjects;
using Mordred.Graphics.Consoles;
using SadConsole.Entities;
using System;
using System.Collections.Generic;

namespace Mordred.Entities
{
    public abstract class Actor : Entity
    {
        public Inventory Inventory { get; private set; }
        public IAction CurrentAction { get; private set; }
        private readonly Queue<IAction> _actorActionsQueue;

        #region Actor stats
        public virtual int Health { get; private set; }
        public virtual int Hunger { get; private set; }

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

        public void AddAction(IAction action)
        {
            _actorActionsQueue.Enqueue(action);
        }

        public bool CanMoveTowards(int x, int y, out CustomPath path)
        {
            path = MapConsole.World.Pathfinder.ShortestPath(Position, new Coord(x, y))?.ToCustomPath();
            return path != null;
        }

        public bool MoveTowards(CustomPath path)
        {
            if (path == null) return false;

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

        public virtual void TakeDamage(int damage)
        {
            Health -= damage;

            // Actor died
            if (Health <= 0)
            {
                // Unset actions
                _actorActionsQueue.Clear();
                CurrentAction = null;

                // Remove from the map and unsubscribe from events
                Game.GameTick -= GameTick;
                Game.GameTick -= HandleActions;
                Game.Container.GetConsole<MapConsole>().Children.Remove(this);
            }
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
                    TakeDamage(2);
            }
            _hungerTicks++;
        }
    }
}
