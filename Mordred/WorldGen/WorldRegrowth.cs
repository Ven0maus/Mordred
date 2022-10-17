using Mordred.Config;
using Mordred.Config.WorldGenConfig;
using Mordred.Entities;
using Mordred.Entities.Animals;
using Mordred.Graphics.Consoles;
using System;
using System.Collections.Generic;
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
        private static World World { get { return MapConsole.World; } }
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
                var loadedChunkCoordinates = World.GetLoadedChunkCoordinates().ToArray();
                var entitiesPerChunk = EntitySpawner.Entities
                    .GroupBy(a => World.GetChunkCoordinate(a.Position.X, a.Position.Y))
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
                RegrowWildLife(chunkCoordinate);
                return;
            }

            var totalWildLife = entityChunk.Count();
            var predators = entityChunk.OfType<PredatorAnimal>().Count();
            var minPercentagePredators = (int)((double)totalWildLife / 100 * Constants.WorldSettings.WildLife.PercentagePredators);
            if (predators < minPercentagePredators)
            {
                // Not enough predators
                RegrowWildLife(chunkCoordinate);
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
                RegrowWildLife(chunkCoordinate);
                return;
            }
        }

        private static void ResourceStatusCheck((int x, int y) chunkCoordinate)
        {
            // TODO: Find bug that always sets one cell still after run
            var chunkCellPositions = World.GetChunkCellCoordinates(chunkCoordinate.x, chunkCoordinate.y);
            var renawableCells = ConfigLoader.GetTerrains(a => a.renawable)
                .Where(a => a.renawable)
                .ToDictionary(a => a.id, a => a);
            var resourceCells = World.GetCells(chunkCellPositions)
                .GroupBy(a => a.TerrainId)
                .Where(a => renawableCells.ContainsKey(a.Key))
                .ToArray();
            foreach (var cell in renawableCells)
            {
                var group = resourceCells.FirstOrDefault(a => a.Key == cell.Key);
                var amount = group == null ? 0 : group.Count();
                if (amount < cell.Value.minResourceAmount)
                    RegrowResource(chunkCoordinate, cell.Value, amount);
            }
        }

        private static void RegrowWildLife((int x, int y) chunkCoordinate)
        {
            // TODO
        }

        private static void RegrowResource((int x, int y) chunkCoordinate, WorldCellObject terrainConfig, int currentAmount)
        {
            // Don't regrow for unloaded chunk, (it could have unloaded in the meantime)
            if (!World.IsChunkLoaded(chunkCoordinate.x, chunkCoordinate.y)) return;

            var minAmount = terrainConfig.minResourceAmount;
            var neededAmount = minAmount - currentAmount;
            var spawnsOnTerrain = terrainConfig.spawnOnTerrain.ToHashSet();

            var newCells = World.GetCells(World.GetChunkCellCoordinates(chunkCoordinate.x, chunkCoordinate.y))
                .Where(a => spawnsOnTerrain.Contains(a.TerrainId))
                .TakeRandom(neededAmount)
                .Select(cell =>
                {
                    return ConfigLoader.GetNewTerrainCell(terrainConfig.id, cell.X, cell.Y);
                }).ToArray(); // Remove ToArray once debugging is succesful

            World.SetCells(newCells);

            System.Diagnostics.Debug.WriteLine("Found only " + currentAmount + " of " + terrainConfig.name + " in the chunk "+chunkCoordinate+".");
            System.Diagnostics.Debug.WriteLine("Added " + newCells.Length + " cells to the chunk.");
        }
    }
}
