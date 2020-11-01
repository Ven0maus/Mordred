using GoRogue;
using Mordred.Entities.Tribals;
using Mordred.GameObjects.ItemInventory;
using Mordred.Graphics.Consoles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities.Actions.Implementations
{
    public class GatheringAction : BaseAction
    {
        public Coord? CurrentGatherable;
        public override event EventHandler<Actor> ActionCompleted;
        private readonly int[] _cellsToGather;
        private int _amount;
        private bool _taskDone = false;
        private bool _deliveringItem = false;
        private readonly int _gatherTickRate;
        private int _gatherCounter = 0;
        private List<Coord> _gatherables;

        public GatheringAction(int cellId, int amount = 1, int? gatherTickRate = null)
        {
            _cellsToGather = new[] { cellId };
            _amount = amount;
            _gatherTickRate = gatherTickRate != null ? gatherTickRate.Value : Constants.ActionSettings.DefaultGatherTickRate;
        }

        public GatheringAction(IEnumerable<Coord> gatherables, int? gatherTickRate = null)
        {
            _gatherables = gatherables.ToList();
            _amount = _gatherables.Count;
            _gatherTickRate = gatherTickRate != null ? gatherTickRate.Value : Constants.ActionSettings.DefaultGatherTickRate;
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

        private List<int> _currentItemsGathered;
        private int? _currentCellGathered;
        private bool GatherItem(Actor actor)
        {
            // Require x amount of ticks to gather the item
            if (_gatherCounter < _gatherTickRate)
            {
                _gatherCounter++;
                return false;
            }
            _gatherCounter = 0;

            // Get correct items
            _currentItemsGathered = MapConsole.World.GetItemIdDropsByCellId(CurrentGatherable.Value);
            _currentCellGathered = MapConsole.World.GetCell(CurrentGatherable.Value.X, CurrentGatherable.Value.Y).CellId;

            // Replace gatherable by the underlying terrain
            var terrainCell = MapConsole.World.GetTerrain(CurrentGatherable.Value.X, CurrentGatherable.Value.Y);
            MapConsole.World.SetCell(CurrentGatherable.Value.X, CurrentGatherable.Value.Y, terrainCell);
            MapConsole.World.Render(true, false);

            // Add x of the gatherable item to actor inventory
            foreach (var itemId in _currentItemsGathered)
            {
                var dropRate = Inventory.ItemCache[itemId].GetDropRateForCellId(_currentCellGathered.Value);
                if (dropRate == null) continue;
                actor.Inventory.Add(itemId, Game.Random.Next(dropRate.Value.Key, dropRate.Value.Value));
            }
            _amount--;
            return true;
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
                    foreach (var itemId in _currentItemsGathered)
                    {
                        var dropRate = Inventory.ItemCache[itemId].GetDropRateForCellId(_currentCellGathered.Value);
                        if (dropRate == null) continue;
                        var item = actor.Inventory.Take(itemId, Game.Random.Next(dropRate.Value.Key, dropRate.Value.Value));
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
                var tribeman = actor as Tribal;
                if (tribeman.Position == tribeman.HutPosition ||
                    !tribeman.CanMoveTowards(tribeman.HutPosition.X, tribeman.HutPosition.Y, out CustomPath movPath) ||
                    !tribeman.MoveTowards(movPath))
                {
                    // Add gatherables to village resource collection
                    foreach (var itemId in _currentItemsGathered)
                    {
                        tribeman.Village.Inventory.Add(itemId, actor.Inventory.Take(itemId).Amount);
                    }

                    if (_taskDone)
                    {
                        ActionCompleted?.Invoke(this, actor);
                        return true;
                    }

                    _currentItemsGathered = null;
                    _currentCellGathered = null;
                    CurrentGatherable = null;
                    _deliveringItem = false;
                }
                return false;
            }

            if (CurrentGatherable == null && _taskDone)
            {
                ActionCompleted?.Invoke(this, actor);
                return true;
            }

            if (CurrentGatherable == null)
            {
                CurrentGatherable = GetClosestGatherable(actor);
                if (CurrentGatherable == null)
                {
                    ActionCompleted?.Invoke(this, actor);
                    return true;
                }
            }

            // Check if the path towards the tree is valid
            if (!actor.CanMoveTowards(CurrentGatherable.Value.X, CurrentGatherable.Value.Y, out CustomPath path))
            {
                CurrentGatherable = null;
                if (_taskDone)
                    ActionCompleted?.Invoke(this, actor);
                return _taskDone;
            }

            if (actor.Position == CurrentGatherable.Value || !actor.MoveTowards(path))
            {
                if (GatherItem(actor))
                {
                    if (actor is Tribal)
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
