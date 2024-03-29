﻿using Mordred.Config;
using Mordred.Entities;
using Mordred.GameObjects.Effects;
using Mordred.Graphics.Consoles;
using Mordred.Helpers;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Venomaus.FlowVitae.Chunking;
using Venomaus.FlowVitae.Grids;
using Venomaus.FlowVitae.Helpers;

namespace Mordred.WorldGen
{
    public enum WorldLayer
    {
        TERRAIN,
        OBJECTS,
        ITEMS,
        ENTITIES
    }

    public class World : GridBase<int, WorldCell>
    {
        private readonly MapConsole MapConsole;
        private readonly List<CellEffect> _cellEffects;

        private readonly bool _worldInitialized = false;
        private readonly ConcurrentHashSet<Point> _chunkEntitiesLoaded;

        public int WorldSeed { get; }

        public World(int width, int height, int worldSeed) : base(width, height, 
            Constants.WorldSettings.ChunkWidth, Constants.WorldSettings.ChunkHeight, new ProceduralGeneration(worldSeed))
        {
            // Get map console reference
            WorldSeed = worldSeed;
            MapConsole = Game.Container.GetConsole<MapConsole>();
            OnCellUpdate += MapConsole.OnCellUpdate;
            OnChunkUnload += UnloadEntities;
            OnChunkLoad += LoadEntities;
            RaiseOnlyOnCellTypeChange = false;

            // Initialize the arrays
            _cellEffects = new List<CellEffect>();
            _chunkEntitiesLoaded = new();

            Game.GameTick += HandleEffects;
            _worldInitialized = true;
        }

        public void Initialize()
        {
            // Re-initialize the starter chunks
            ClearCache();
            UpdateScreenCells();
        }

        private void LoadEntities(object sender, ChunkUpdateArgs args)
        {
            _ = Task.Run(() =>
            {
                if (_chunkEntitiesLoaded.Contains((args.ChunkX, args.ChunkY))) return;
                ProceduralGeneration.GenerateWildLife(args.ChunkX, args.ChunkY);
                ProceduralGeneration.GenerateVillages(args.ChunkX, args.ChunkY);
                _chunkEntitiesLoaded.Add((args.ChunkX, args.ChunkY));
            }).ConfigureAwait(false);
        }

        private void UnloadEntities(object sender, ChunkUpdateArgs args)
        {
            _ = Task.Run(() =>
            {
                var chunkCellPositions = args.GetCellPositions().ToHashSet(new TupleComparer<int>());
                EntitySpawner.DestroyAll<IEntity>(a => chunkCellPositions.Contains(a.WorldPosition));
                _chunkEntitiesLoaded.Remove((args.ChunkX, args.ChunkY));
            }).ConfigureAwait(false);
        }

        public void AddEffect(CellEffect effect)
        {
            if (effect.TicksRemaining > 0 && !_cellEffects.Contains(effect))
                _cellEffects.Add(effect);
        }

        public IEnumerable<CellEffect> GetCellEffects(int x, int y)
        {
            var pos = new Point(x, y);
            return _cellEffects.Where(a => a.WorldPosition == pos);
        }

        private void HandleEffects(object sender, EventArgs args)
        {
            foreach (var effect in _cellEffects)
            {
                effect.Execute();
            }
            _cellEffects.RemoveAll(a => a.TicksRemaining <= 0 || a.Completed);
        }

        protected override WorldCell Convert(int x, int y, int cellType)
        {
            // Get custom cell
            var cell = ConfigLoader.GetNewWorldCell(cellType, x, y);
            if (cell == null) return base.Convert(x, y, cellType);
            cell.X = x;
            cell.Y = y;
            return cell;
        }

        public override void Center(int x, int y)
        {
            base.Center(x, y);

            if (!_worldInitialized) return;

            // Adjust entity visibiltiy when off screen
            foreach (var entity in EntitySpawner.Entities)
            {
                entity.IsVisible = IsWorldCoordinateOnViewPort(entity.WorldPosition.X, entity.WorldPosition.Y);
                if (entity.IsVisible)
                {
                    entity.Position = WorldToScreenCoordinate(entity.WorldPosition.X, entity.WorldPosition.Y);
                }
            }
        }

        public int GetDominatingTerrain(IEnumerable<Point> positions, Func<WorldCell, bool> criteria = null)
        {
            var cells = GetCells(positions);
            if (criteria != null)
                cells = cells.Where(criteria);
            return cells
                .GroupBy(a => a.CellType)
                .OrderByDescending(a => a.Key)
                .First().First().TerrainId;
        }

        public bool CellWalkable(int x, int y)
        {
            if (!IsChunkLoaded(x, y)) return false;
            return GetCell(x, y).Walkable;
        }

        public IEnumerable<Point> GetCellCoordsFromCenter(int startX, int startY, Func<WorldCell, bool> criteria)
        {
            var sX = startX - Constants.WorldSettings.ChunkWidth / 2;
            var sY = startY - Constants.WorldSettings.ChunkHeight / 2;
            var eX = sX + Constants.WorldSettings.ChunkWidth;
            var eY = sY + Constants.WorldSettings.ChunkHeight;
            for (int y = sY; y < eY; y++)
            {
                for (int x = sX; x < eX; x++)
                {
                    if (!IsChunkLoaded(x, y)) continue;
                    if (criteria.Invoke(GetCell(x, y)))
                        yield return (x, y);
                }
            }
        }

        /// <summary>
        /// Returns a list of all the WorldItem Id's that the given WorldCell drops
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public List<int> GetItemIdDropsByCellId(Point coord)
        {
            var terrainId = GetCell(coord.X, coord.Y).TerrainId;
            var items = ConfigLoader.Items.Where(a => a.Value.DroppedBy != null && a.Value.IsDroppedBy(terrainId))
                .Select(a => a.Key)
                .ToList();
            return items;
        }

        public IEnumerable<WorldCell> GetCells(IEnumerable<Point> points)
        {
            return base.GetCells(points.Select(a => (a.X, a.Y)));
        }
    }
}
