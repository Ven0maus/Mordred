using GoRogue;
using Mordred.Entities.Tribals;
using Mordred.GameObjects;
using Mordred.Graphics.Consoles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities.Actions.Implementations
{
    public class GatheringAction : BaseAction
    {
        public Coord? CurrentGatherable;
        public override event EventHandler<ActionArgs> ActionCompleted;
        private readonly int[] _cellsToGather;
        private int _amount;
        private bool _taskDone = false;
        private bool _deliveringItem = false;
        private int _gatherCounter = 0;
        private List<Coord> _gatherables;

        public GatheringAction(int cellId, int amount = 1)
        {
            _cellsToGather = new[] { cellId };
            _amount = amount;
        }

        public GatheringAction(IEnumerable<Coord> gatherables)
        {
            _gatherables = gatherables.ToList();
            _amount = _gatherables.Count;
            _cellsToGather = _gatherables
                .Select(a => MapConsole.World.GetCell(a.X, a.Y).CellId)
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Get the next closest cell to gather from the actor's location
        /// </summary>
        /// <param name="actorPosition"></param>
        /// <param name="tribeman"></param>
        /// <returns></returns>
        private Coord? GetClosestGatherable(Actor actor)
        {
            if (_amount == 1)
            {
                _taskDone = true;
            }

            if (_gatherables == null)
            {
                _gatherables = MapConsole.World.GetCellCoords(a => a.CellId == _cellsToGather[0]).ToList();
            }

            // Update gatherables
            _gatherables.RemoveAll(a => !_cellsToGather.Contains(MapConsole.World.GetCell(a.X, a.Y).CellId));

            if (_gatherables.Count == 0) return null;

            var gatherable =  _gatherables
                .Select(a => (Coord?)a)
                .OrderBy(a => a.Value.SquaredDistance(actor.Position))
                .FirstOrDefault(a => !IsGatherableAlreadyBeingGatheredByOtherActor(a.Value, actor));

            if (gatherable != null)
            {
                _gatherables.Remove(gatherable.Value);
            }

            return gatherable;
        }

        /// <summary>
        /// Use this to validate if the selected tree is not already being cut by someone else
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="tribeman"></param>
        /// <returns></returns>
        private bool IsGatherableAlreadyBeingGatheredByOtherActor(Coord coord, Actor actor)
        {
            var actors = EntitySpawner.Entities.OfType<Actor>()
                .Where(a => !a.Equals(actor) && a.CurrentAction != null && a.CurrentAction is GatheringAction)
                .ToList();
            foreach (var oActor in actors)
            {
                var wa = (GatheringAction)oActor.CurrentAction;
                if (wa.CurrentGatherable != null && wa.CurrentGatherable.Value.X == coord.X && wa.CurrentGatherable.Value.Y == coord.Y)
                    return true;
            }
            return false;
        }

        private bool GatherItem(Actor actor)
        {
            // Require x amount of ticks to gather the item
            if (_gatherCounter < Constants.ActionSettings.DefaultGatherTickRate)
            {
                _gatherCounter++;
                return false;
            }
            _gatherCounter = 0;

            // Replace gatherable by the underlying terrain
            var terrainCell = MapConsole.World.GetTerrain(CurrentGatherable.Value.X, CurrentGatherable.Value.Y);
            MapConsole.World.SetCell(CurrentGatherable.Value.X, CurrentGatherable.Value.Y, terrainCell);
            MapConsole.World.Render(true, false);

            // Add 5 of the gatherable item to actor inventory
            foreach (var itemId in GetItemsFromGatherable())
            {
                actor.Inventory.Add(itemId, Constants.ActionSettings.DefaultTotalGather);
            }
            _amount--;
            return true;
        }

        private List<int> GetItemsFromGatherable()
        {
            var items = Inventory.ItemCache.Where(a => a.Value.DroppedBy != null && a.Value.DroppedBy.Any(b => _cellsToGather.Contains(b)))
                .Select(a => a.Key)
                .ToList();
            return items;
        }

        public override bool Execute(Actor actor)
        {
            // if action is canceled
            if (base.Execute(actor))
            {
                if (_deliveringItem)
                {
                    var mapConsole = Game.Container.GetConsole<MapConsole>();
                    // Drop item(s) on the current standing tile
                    foreach (var itemId in GetItemsFromGatherable())
                    {
                        var item = actor.Inventory.Take(itemId, Constants.ActionSettings.DefaultTotalGather);
                        item.Position = actor.Position;

                        // Insert under the actor
                        mapConsole.Children.Insert(0, item);
                    }
                }
                return true;
            }

            // We are now delivering the gatherable back to our hut
            if (_deliveringItem)
            {
                var tribeman = actor as Tribeman;
                if (tribeman.Position == tribeman.HutPosition ||
                    !tribeman.CanMoveTowards(tribeman.HutPosition.X, tribeman.HutPosition.Y, out CustomPath movPath) ||
                    !tribeman.MoveTowards(movPath))
                {
                    // Add gatherables to village resource collection
                    foreach (var itemId in GetItemsFromGatherable())
                    {
                        tribeman.Village.Inventory.Add(itemId, actor.Inventory.Take(itemId).Amount);
                    }

                    if (_taskDone)
                    {
                        ActionCompleted?.Invoke(this, new ActionArgs { Actor = actor });
                        return true;
                    }

                    CurrentGatherable = null;
                    _deliveringItem = false;
                }
                return false;
            }

            if (CurrentGatherable == null && _taskDone)
            {
                ActionCompleted?.Invoke(this, new ActionArgs { Actor = actor });
                return true;
            }

            if (CurrentGatherable == null)
            {
                CurrentGatherable = GetClosestGatherable(actor);
                if (CurrentGatherable == null)
                {
                    ActionCompleted?.Invoke(this, new ActionArgs { Actor = actor });
                    return true;
                }
            }

            // Check if the path towards the tree is valid
            if (!actor.CanMoveTowards(CurrentGatherable.Value.X, CurrentGatherable.Value.Y, out CustomPath path))
            {
                CurrentGatherable = null;
                if (_taskDone)
                    ActionCompleted?.Invoke(this, new ActionArgs { Actor = actor });
                return _taskDone;
            }

            if (actor.Position == CurrentGatherable.Value || !actor.MoveTowards(path))
            {
                if (GatherItem(actor))
                {
                    if (actor is Tribeman)
                    {
                        _deliveringItem = true;
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
