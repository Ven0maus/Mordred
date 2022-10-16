using Mordred.Entities;
using Mordred.Entities.Animals;
using Mordred.Graphics.Consoles;
using System;
using System.Linq;
using System.Threading.Tasks;
using Venomaus.FlowVitae.Helpers;

namespace Mordred.WorldGen
{
    /// <summary>
    /// Handles respawning of resources like nature, wildlife
    /// </summary>
    public class WorldRegrowth
    {
        private static int _ticksUntilStatusCheck = Constants.WorldSettings.RegrowthStatusCheckTimeInSeconds * Game.TicksPerSecond;
        /// <summary>
        /// Checks and regrows what is required by the world standards
        /// </summary>
        public static void CheckRegrowthStatus(object sender, EventArgs args)
        {
            if (_ticksUntilStatusCheck > 0)
            {
                _ticksUntilStatusCheck--;
                return;
            }
            _ticksUntilStatusCheck = Constants.WorldSettings.RegrowthStatusCheckTimeInSeconds * Game.TicksPerSecond;

            _ = Task.Factory.StartNew(() =>
            {
                System.Diagnostics.Debug.WriteLine("Starting regrowth status checks.");

                // Get all loaded chunk coordinates
                var loadedChunkCoordinates = MapConsole.World.GetLoadedChunkCoordinates();
                var entitiesPerChunk = EntitySpawner.Entities.ToArray()
                    .GroupBy(a => MapConsole.World.GetChunkCoordinate(a.Position.X, a.Position.Y))
                    .ToArray();
                var comparer = new TupleComparer<int>();
                foreach (var chunkCoordinate in loadedChunkCoordinates)
                {
                    // Check wild life
                    WildLifeStatusCheck(chunkCoordinate, entitiesPerChunk, comparer);

                    // Check resources
                    ResourceStatusCheck(chunkCoordinate);
                }
            });
        }

        private static void WildLifeStatusCheck((int x, int y) chunkCoordinate, 
            IGrouping<(int x, int y), IEntity>[] entitiesPerChunk, TupleComparer<int> comparer)
        {
            var entityChunk = entitiesPerChunk.FirstOrDefault(a => comparer.Equals(a.Key, chunkCoordinate));
            if (entityChunk == null)
            {
                // No entities for this chunk??
                RegrowWildLife();
                return;
            }

            var totalWildLife = entityChunk.Count();
            var predators = entityChunk.OfType<PredatorAnimal>().Count();
            var minPercentagePredators = (int)((double)totalWildLife / 100 * Constants.WorldSettings.WildLife.PercentagePredators);
            if (predators < minPercentagePredators)
            {
                // Not enough predators
                RegrowWildLife();
                return;
            }

            var passive = entityChunk.OfType<PassiveAnimal>().Count();
            int maxPredators = (int)((double)passive / 100 * Constants.WorldSettings.WildLife.PercentagePredators);
            if (predators > (passive + maxPredators))
            {
                // Too many predators
                // Should we exterminate some predators, through disease etc?
                // Or should we spawn much stronger predators to hunt down the tree?
                // TODO: Investigate
            }

            var minWildLife = Constants.WorldSettings.WildLife.MinWildLifePerChunk;
            if (totalWildLife < minWildLife)
            {
                // Under the minimum for all wild life
                RegrowWildLife();
                return;
            }
        }

        private static void ResourceStatusCheck((int x, int y) chunkCoordinate)
        {
            var chunkCellPositions = MapConsole.World.GetChunkCellCoordinates(chunkCoordinate.x, chunkCoordinate.y);
            var resourceCells = MapConsole.World.GetCells(chunkCellPositions)
                .Where(a => a.IsResource)
                .GroupBy(a => a.TerrainId);
            foreach (var group in resourceCells)
            {
                var amount = group.Count();
                if (amount < Constants.WorldSettings.Resources.MinResourcePerChunk)
                    RegrowResource(group.Key);
            }
        }

        private static void RegrowWildLife()
        {
            // TODO
        }

        private static void RegrowResource(int terrainId)
        {
            // TODO
        }
    }
}
