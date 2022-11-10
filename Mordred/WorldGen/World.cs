using Mordred.Config;
using Mordred.Entities;
using Mordred.GameObjects.Effects;
using Mordred.Graphics.Consoles;
using Mordred.Helpers;
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
    }

    public class World
    {
        private readonly WorldWindow MapConsole;
        private readonly List<CellEffect> _cellEffects;

        private readonly bool _worldInitialized = false;
        private readonly ConcurrentHashSet<Point> _chunkEntitiesLoaded;

        public int WorldSeed { get; }

        private bool _useThreading = true;
        public bool UseThreading
        {
            get { return _useThreading; }
            set 
            { 
                _useThreading = value;
                _terrainMap.UseThreading = value;
                _objectMap.UseThreading = value;
            }
        }

        public int ChunkWidth { get; private set; }
        public int ChunkHeight { get; private set; }

        // The maps that represent the game world
        private readonly Grid<int, WorldCell> _terrainMap;
        private readonly Grid<int, WorldCell> _objectMap;

        public World(int width, int height, int worldSeed)
        {
            // Get map console reference
            WorldSeed = worldSeed;
            MapConsole = Game.Container.GetConsole<WorldWindow>();

            // Initialize the arrays
            _cellEffects = new List<CellEffect>();
            _chunkEntitiesLoaded = new();

            // Initialize object layer
            _terrainMap = new Grid<int, WorldCell>(width, height, Constants.WorldSettings.ChunkWidth, Constants.WorldSettings.ChunkHeight,
                new ProceduralGeneration(worldSeed, WorldLayer.TERRAIN));
            _objectMap = new Grid<int, WorldCell>(width, height, Constants.WorldSettings.ChunkWidth, Constants.WorldSettings.ChunkHeight,
                new ProceduralGeneration(worldSeed, WorldLayer.OBJECTS));

            // Set properties
            ChunkWidth = _terrainMap.ChunkWidth;
            ChunkHeight = _terrainMap.ChunkHeight;

            // Set custom converters for tile conversion
            _terrainMap.SetCustomConverter(Converter);
            _objectMap.SetCustomConverter(Converter);

            // Subscribe events
            _terrainMap.OnChunkLoad += LoadEntities;
            _terrainMap.OnChunkUnload += UnloadEntities;
            _terrainMap.OnCellUpdate += MapConsole.OnTerrainUpdate;
            _objectMap.OnCellUpdate += MapConsole.OnObjectUpdate;

            // Terrain properties
            _terrainMap.RaiseOnlyOnCellTypeChange = false;
            _objectMap.RaiseOnlyOnCellTypeChange = false;

            Game.GameTick += HandleEffects;
            _worldInitialized = true;
        }

        public void Initialize()
        {
            // Re-initialize the starter chunks
            _terrainMap.ClearCache();
            _objectMap.ClearCache();
        }

        private void LoadEntities(object sender, ChunkUpdateArgs args)
        {
            _ = Task.Factory.StartNew(() =>
            {
                if (_chunkEntitiesLoaded.Contains((args.ChunkX, args.ChunkY))) return;
                ProceduralGeneration.GenerateWildLife(args.ChunkX, args.ChunkY);
                ProceduralGeneration.GenerateVillages(args.ChunkX, args.ChunkY);
                _chunkEntitiesLoaded.Add((args.ChunkX, args.ChunkY));
            }).ConfigureAwait(false);
        }

        private void UnloadEntities(object sender, ChunkUpdateArgs args)
        {
            _ = Task.Factory.StartNew(() =>
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

        private WorldCell Converter(int x, int y, int cellType)
        {
            // Return a default tile
            if (cellType == Constants.WorldSettings.VoidTile)
            {
                return new WorldCell 
                { 
                    X = x, 
                    Y = y, 
                    CellType = cellType, 
                    Walkable = true, 
                    IsVisible = false, 
                    SeeThrough = true 
                };
            }

            // Get custom cell
            var cell = ConfigLoader.GetNewWorldCell(cellType, x, y);
            if (cell == null) 
                return new WorldCell() { X = x, Y = y, CellType = cellType };
            cell.X = x;
            cell.Y = y;
            return cell;
        }

        public void Center(int x, int y)
        {
            _terrainMap.Center(x, y);
            _objectMap.Center(x, y);

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

        public int GetDominatingCell(WorldLayer layer, IEnumerable<Point> positions, Func<WorldCell, bool> criteria = null)
        {
            var cells = GetCells(layer, positions);
            if (criteria != null)
                cells = cells.Where(criteria);
            return cells
                .GroupBy(a => a.TerrainId)
                .OrderByDescending(a => a.Key)
                .First().Key;
        }

        public bool CellWalkable(int x, int y)
        {
            if (!_terrainMap.IsChunkLoaded(x, y)) return false;
            return GetCell(WorldLayer.TERRAIN, x, y).Walkable && GetCell(WorldLayer.OBJECTS, x, y).Walkable;
        }

        public IEnumerable<Point> GetCellCoordsFromCenter(WorldLayer layer, int startX, int startY, Func<WorldCell, bool> criteria)
        {
            var sX = startX - Constants.WorldSettings.ChunkWidth / 2;
            var sY = startY - Constants.WorldSettings.ChunkHeight / 2;
            var eX = sX + Constants.WorldSettings.ChunkWidth;
            var eY = sY + Constants.WorldSettings.ChunkHeight;
            for (int y = sY; y < eY; y++)
            {
                for (int x = sX; x < eX; x++)
                {
                    if (!_terrainMap.IsChunkLoaded(x, y)) continue;
                    var cell = GetCell(layer, x, y);
                    if (criteria.Invoke(cell))
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
            var terrainId = GetCell(WorldLayer.OBJECTS, coord.X, coord.Y).TerrainId;
            var items = ConfigLoader.Items.Where(a => a.Value.DroppedBy != null && a.Value.IsDroppedBy(terrainId))
                .Select(a => a.Key)
                .ToList();
            return items;
        }

        public IEnumerable<WorldCell> GetCells(WorldLayer layer, IEnumerable<Point> points)
        {
            var map = GetMapLayer(layer);
            return map.GetCells(points.Select(a => (a.X, a.Y)));
        }

        public IEnumerable<WorldCell> GetCells(WorldLayer layer, IEnumerable<(int x, int y)> points)
        {
            var map = GetMapLayer(layer);
            return map.GetCells(points);
        }

        public WorldCell GetCell(WorldLayer layer, int x, int y)
        {
            var map = GetMapLayer(layer);
            return map.GetCell(x, y);
        }

        public void SetCell(WorldLayer layer, int x, int y, int cellType)
        {
            var map = GetMapLayer(layer);
            map.SetCell(x, y, cellType);
        }

        public void SetCell(WorldLayer layer, WorldCell cell)
        {
            var map = GetMapLayer(layer);
            map.SetCell(cell);
        }

        public void ClearCell(WorldLayer layer, int x, int y)
        {
            var map = GetMapLayer(layer);
            map.SetCell(x, y, -1);
        }

        private Grid<int, WorldCell> GetMapLayer(WorldLayer layer)
        {
            switch (layer)
            {
                case WorldLayer.TERRAIN:
                    return _terrainMap;
                case WorldLayer.OBJECTS:
                    return _objectMap;
                default:
                    throw new Exception("No map found for layer '"+layer+"'");
            }
        }

        public Point WorldToScreenCoordinate(int x, int y)
        {
            return _terrainMap.WorldToScreenCoordinate(x, y);
        }

        public bool IsWorldCoordinateOnViewPort(int x, int y)
        {
            return _terrainMap.IsWorldCoordinateOnViewPort(x, y);
        }

        public int GetChunkSeed(int x, int y)
        {
            return _terrainMap.GetChunkSeed(x, y);
        }

        public IEnumerable<(int x, int y)> GetLoadedChunkCoordinates()
        {
            return _terrainMap.GetLoadedChunkCoordinates();
        }

        public IEnumerable<(int x, int y)> GetChunkCellCoordinates(int x, int y)
        {
            return _terrainMap.GetChunkCellCoordinates(x, y);
        }

        public (int x, int y) GetChunkCoordinate(int x, int y)
        {
            return _terrainMap.GetChunkCoordinate(x, y);
        }

        public bool IsChunkLoaded(int x, int y)
        {
            return _terrainMap.IsChunkLoaded(x, y);
        }

        public void SetCells(WorldLayer layer, IEnumerable<WorldCell> newCells)
        {
            var map = GetMapLayer(layer);
            map.SetCells(newCells);
        }
    }
}
