using GoRogue;
using Mordred.Graphics.Consoles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities.Actions.Implementations
{
    public class WoodcuttingAction : BaseAction
    {
        public override event EventHandler<ActionArgs> ActionCompleted;

        private readonly IEnumerable<Coord> _selectedTrees = null;
        private Queue<Coord> _selectedTreesOrdered = null;
        public Coord? CurrentTree { get; private set; }
        private bool _taskDone = false;
        private bool _deliveringWood = false;
        private int _treeChopCounter = 0;

        /// <summary>
        /// Constructor for woodcutting action
        /// </summary>
        /// <param name="selectedTrees">if null only the first closest tree will be cutted</param>
        public WoodcuttingAction(IEnumerable<Coord> selectedTrees = null)
        {
            _selectedTrees = selectedTrees;
        }

        /// <summary>
        /// Get the next closest tree to the actor's location
        /// </summary>
        /// <param name="actorPosition"></param>
        /// <param name="tribeman"></param>
        /// <returns></returns>
        private Coord? GetClosestTree(Coord actorPosition, Tribeman tribeman)
        {
            if (_selectedTrees == null)
            {
                _taskDone = true;
                var trees = MapConsole.World.GetCellCoords(a => a.CellId == 2).ToList();
                if (trees.Count == 0) return null;
                return trees
                    .Select(a => (Coord?)a)
                    .OrderBy(a => a.Value.SquaredDistance(actorPosition))
                    .FirstOrDefault(a => !IsTreeAlreadyBeingCutByOtherActor(a.Value, tribeman));
            }
          
            if (_selectedTreesOrdered == null)
                _selectedTreesOrdered = new Queue<Coord>(_selectedTrees.OrderBy(a => a.SquaredDistance(actorPosition)));

            var tree = _selectedTreesOrdered.Count > 0 ? _selectedTreesOrdered.Dequeue() : (Coord?)null;
            while (tree != null && (MapConsole.World.GetCell(tree.Value.X, tree.Value.Y).CellId != 2 || IsTreeAlreadyBeingCutByOtherActor(tree.Value, tribeman)))
            {
                tree = _selectedTreesOrdered.Count > 0 ? _selectedTreesOrdered.Dequeue() : (Coord?)null;
            }
            return tree;
        }

        /// <summary>
        /// Use this to validate if the selected tree is not already being cut by someone else
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="tribeman"></param>
        /// <returns></returns>
        private bool IsTreeAlreadyBeingCutByOtherActor(Coord coord, Tribeman tribeman)
        {
            var villagers = tribeman.Village.Tribemen
                .Where(a => !a.Equals(tribeman) && a.CurrentAction != null && a.CurrentAction is WoodcuttingAction)
                .ToList();
            foreach (var villager in villagers)
            {
                var wa = (WoodcuttingAction)villager.CurrentAction;
                if (wa.CurrentTree != null && wa.CurrentTree.Value.X == coord.X && wa.CurrentTree.Value.Y == coord.Y)
                    return true;
            }
            return false;
        }

        public bool ChopTree(Coord coord, Actor actor)
        {
            // Require 8 ticks to chop the tree
            if (_treeChopCounter < 8)
            {
                _treeChopCounter++;
                return false;
            }

            // Reset counter for the next tree
            _treeChopCounter = 0;

            // Replace tree by the underlying terrain
            var terrainCell = MapConsole.World.GetTerrain(coord.X, coord.Y);
            MapConsole.World.SetCell(coord.X, coord.Y, terrainCell);
            MapConsole.World.Render(true, false);

            // Add 10 wood to actor inventory
            actor.Inventory.Add(0, 10);

            return true;
        }

        public override bool Execute(Actor actor)
        {
            // Check for canceled state
            if (base.Execute(actor))
            {
                if (_deliveringWood)
                {
                    // Drop wood item on the current standing tile
                    var wood = actor.Inventory.Take(0, 10);
                    wood.Position = actor.Position;

                    // Insert under the actor
                    var mapConsole = Game.Container.GetConsole<MapConsole>();
                    mapConsole.Children.Insert(0, wood);
                }
                return true;
            }

            // If this action is not executed by a tribeman we finish the task instantly
            if (!(actor is Tribeman tribeman))
            {
                ActionCompleted?.Invoke(this, new ActionArgs { Actor = actor });
                return true;
            }

            // We are now delivering the wood back to our hut
            if (_deliveringWood) 
            {
                if (tribeman.Position == tribeman.HutPosition || !tribeman.MoveTowards(tribeman.HutPosition.X, tribeman.HutPosition.Y))
                {
                    // Add wood to village resource collection
                    tribeman.Village.Inventory.Add(0, tribeman.Inventory.Take(0).Amount);

                    if (_taskDone)
                    {
                        ActionCompleted?.Invoke(this, new ActionArgs { Actor = actor });
                        return true;
                    }

                    CurrentTree = null;
                    _deliveringWood = false;
                }
                return false;
            }

            // Select the next tree to cut
            if (CurrentTree == null)
            {
                CurrentTree = GetClosestTree(actor.Position, tribeman);
                if (CurrentTree == null)
                {
                    ActionCompleted?.Invoke(this, new ActionArgs { Actor = actor });
                    return true;
                }
            }

            // Check if the path towards the tree is valid
            if (!actor.CanMoveTowards(CurrentTree.Value.X, CurrentTree.Value.Y))
            {
                CurrentTree = null;
                if (_taskDone) 
                    ActionCompleted?.Invoke(this, new ActionArgs { Actor = actor });
                return _taskDone;
            }

            // Move the actor to the tree and if the actor is near the tree, start chopping the tree
            if (actor.Position == CurrentTree.Value || !actor.MoveTowards(CurrentTree.Value.X, CurrentTree.Value.Y))
            {
                if (ChopTree(CurrentTree.Value, actor))
                {
                    _deliveringWood = true;
                    return false;
                }
            }
            return false;
        }
    }
}
