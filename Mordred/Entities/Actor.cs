using GoRogue;
using GoRogue.Pathing;
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
        public virtual int Health { get; private set; }

        private readonly Queue<IAction> _actorActionsQueue;
        public IAction CurrentAction { get; private set; }

        public Inventory Inventory { get; private set; }

        public Actor(Color foreground, Color background, int glyph, int health = 100) : base(foreground, background, glyph)
        {
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

        public bool CanMoveTowards(int x, int y, out Path path)
        {
            path = MapConsole.World.Pathfinder.ShortestPath(Position, new Coord(x, y));
            return path != null;
        }

        public bool MoveTowards(Path path)
        {
            if (path == null) return false;

            var nextStep = path.GetStep(0);
            if (MapConsole.World.GetCell(nextStep.X, nextStep.Y).Walkable)
            {
                Position = nextStep;
                return true;
            }
            return false;
        }

        public bool MoveTowards(int x, int y)
        {
            var movementPath = MapConsole.World.Pathfinder.ShortestPath(Position, new Coord(x, y));
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

        /// <summary>
        /// This method is called every game tick, use this to execute actor logic that should be tick based
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected virtual void GameTick(object sender, EventArgs args) { }
    }
}
