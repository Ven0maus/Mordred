using Mordred.Config;
using Mordred.Config.WorldGenConfig;
using Mordred.Entities;
using Mordred.Entities.Animals;
using Mordred.GameObjects.Effects;
using Mordred.GameObjects.ItemInventory;
using Mordred.Graphics.Consoles;
using Mordred.Helpers;
using SadConsole.Entities;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Venomaus.FlowVitae.Grids;
using Venomaus.FlowVitae.Chunking;
using Venomaus.FlowVitae.Procedural;
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
        public static readonly Dictionary<int, WorldCell[]> TerrainCells = ConfigLoader.LoadWorldCells();
        public static readonly Dictionary<int, WorldCell> WorldCells = TerrainCells
            .SelectMany(a => a.Value)
            .ToDictionary(a => a.CellType, a => a);

        private readonly MapConsole MapConsole;
        private readonly List<Village> _villages;
        private readonly List<CellEffect> _cellEffects;
        
        public IReadOnlyList<Village> Villages
        {
            get { return _villages; }
        }

        private readonly bool _worldInitialized = false;
        private readonly ConcurrentHashSet<Point> _chunkEntitiesLoaded;

        public World(int width, int height) : base(width, height, 
            Constants.WorldSettings.ChunkWidth, Constants.WorldSettings.ChunkHeight, new ProceduralGenerator<int, WorldCell>(1000, GenerateLands))
        {
            // Get map console reference
            MapConsole = Game.Container.GetConsole<MapConsole>();
            OnCellUpdate += MapConsole.OnCellUpdate;
            OnChunkUnload += UnloadEntities;
            OnChunkLoad += LoadEntities;
            RaiseOnlyOnCellTypeChange = false;

            // Initialize the arrays
            _villages = new List<Village>(Constants.VillageSettings.MaxVillages);
            _cellEffects = new List<CellEffect>();
            _chunkEntitiesLoaded = new();

            Game.GameTick += HandleEffects;
            _worldInitialized = true;

            // Re-initialize the starter chunks
            ClearCache();
        }

        private void LoadEntities(object sender, ChunkUpdateArgs args)
        {
            _ = Task.Factory.StartNew(() =>
            {
                if (_chunkEntitiesLoaded.Contains((args.ChunkX, args.ChunkY))) return;
                GenerateWildLife(args.ChunkX, args.ChunkY);
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

        protected override WorldCell Convert(int x, int y, int cellType)
        {
            // Get custom cell
            var cell = GetCellConfig(cellType, x, y);
            if (cell == null) return base.Convert(x, y, cellType);
            cell.X = x;
            cell.Y = y;
            return cell;
        }

        public static WorldCell GetCellConfig(int type, int x, int y, bool clone = true, Random customRandom = null)
        {
            var cell = clone ? WorldCells[type].Clone() : WorldCells[type];
            cell.X = x;
            cell.Y = y;
            return cell;
        }

        public static WorldCell GetRandomTerrainCell(int type, int x, int y, bool clone = true, Random customRandom = null)
        {
            var cell = clone ? TerrainCells[type].TakeRandom(customRandom).Clone() : TerrainCells[type].TakeRandom(customRandom);
            cell.X = x;
            cell.Y = y;
            return cell;
        }

        public static void GenerateLands(Random random, int[] chunk, int width, int height)
        {
            // Idk, need to rework..
            var simplex1 = new OpenSimplex2F(random.Next(-500000, 500000));
            var simplexNoise = new double[width * height];
            for (int y=0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double nx = (double)x / width - 0.5d;
                    double ny = (double)y / height - 0.5d;
                    simplexNoise[y * width + x] = simplex1.Noise2(nx, ny);
                }
            }

            // Normalize
            simplexNoise = OpenSimplex2F.Normalize(simplexNoise);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if ((x == 0 || y == 0 || x == width - 1 || y == height - 1) && Constants.GameSettings.DebugMode)
                        chunk[y * width + x] = GetRandomTerrainCell((int)WorldTiles.Border, x, y).CellType;
                    else if (simplexNoise[y * width + x] >= 0.75f && simplexNoise[y * width + x] <= 1f)
                    {
                        // Mountains
                        chunk[y * width + x] = GetRandomTerrainCell((int)WorldTiles.Mountain, x, y).CellType;
                    }
                    else
                    {
                        // Tree, berrybush or grass
                        int chance = random.Next(0, 100);
                        if (chance <= 1)
                            chunk[y * width + x] = GetRandomTerrainCell((int)WorldTiles.BerryBush, x, y).CellType;
                        else if (chance <= 7)
                            chunk[y * width + x] = GetRandomTerrainCell((int)WorldTiles.Tree, x, y).CellType;
                        else
                            chunk[y * width + x] = GetRandomTerrainCell((int)WorldTiles.Grass, x, y).CellType;
                    }
                }
            }
        }

        public override void Center(int x, int y)
        {
            base.Center(x, y);

            if (!_worldInitialized) return;

            foreach (var entity in EntitySpawner.Entities.ToArray())
            {
                entity.IsVisible = IsWorldCoordinateOnViewPort(entity.WorldPosition.X, entity.WorldPosition.Y);
                if (entity.IsVisible)
                {
                    entity.Position = WorldToScreenCoordinate(entity.WorldPosition.X, entity.WorldPosition.Y);
                }
            }
        }

        public void GenerateWildLife(int chunkX, int chunkY)
        {
            var random = new Random(GetChunkSeed(chunkX, chunkY));
            var wildLifeCount = random.Next(Constants.WorldSettings.WildLife.MinWildLifePerChunk, Constants.WorldSettings.WildLife.MaxWildLifePerChunk + 1);
            var (x, y) = (chunkX + ChunkWidth / 2, chunkY + ChunkHeight / 2);
            var spawnPositions = GetCellCoords(x, y, a => a.Walkable).ToList();

            // Get all classes that inherit from PassiveAnimal / PredatorAnimal
            var passiveAnimals = ReflectiveEnumerator.GetEnumerableOfType<PassiveAnimal>().ToList();
            var predatorAnimals = ReflectiveEnumerator.GetEnumerableOfType<PredatorAnimal>().ToList();

            // Divide wildlife into passive and predators based on percentage
            int predators = (int)Math.Round((double)wildLifeCount / 100 * Constants.WorldSettings.WildLife.PercentagePredators);

            var packAnimals = new Dictionary<Type, List<IPackAnimal>>();
            // Automatic selection of all predators
            for (int i = 0; i < predators; i++)
            {
                var animal = predatorAnimals.TakeRandom(random);
                var pos = spawnPositions.TakeRandom(random);
                spawnPositions.Remove(pos);
                var entity = EntitySpawner.Spawn(animal, pos, random.Next(0, 2) == 1 ? Gender.Male : Gender.Female);

                if (typeof(IPackAnimal).IsAssignableFrom(animal))
                {
                    if (!packAnimals.TryGetValue(animal, out List<IPackAnimal> pAnimals))
                    {
                        pAnimals = new List<IPackAnimal>();
                        packAnimals.Add(animal, pAnimals);
                    }
                    pAnimals.Add(entity as IPackAnimal);
                }
            }

            int nonPredators = wildLifeCount - predators;
            // Automatic selection of passive animals
            for (int i=0; i < nonPredators; i++)
            {
                var animal = passiveAnimals.TakeRandom(random);
                var pos = spawnPositions.TakeRandom(random);
                spawnPositions.Remove(pos);
                var entity = EntitySpawner.Spawn(animal, pos, random.Next(0, 2) == 1 ? Gender.Male : Gender.Female);

                if (typeof(IPackAnimal).IsAssignableFrom(animal))
                {
                    if (!packAnimals.TryGetValue(animal, out List<IPackAnimal> pAnimals))
                    {
                        pAnimals = new List<IPackAnimal>();
                        packAnimals.Add(animal, pAnimals);
                    }
                    pAnimals.Add(entity as IPackAnimal);
                }
            }

            // Link pack animals together
            const int whileLoopLimit = 500;
            foreach (var type in packAnimals)
            {
                var splits = (int)Math.Ceiling((double)type.Value.Count / Constants.ActorSettings.MaxPackSize);
                for (int i = 0; i < splits; i++)
                {
                    var animals = type.Value.Take(Constants.ActorSettings.MaxPackSize).ToList();
                    var leader = animals.TakeRandom(random);
                    var centerPoint = leader.WorldPosition;
                    foreach (var animal in animals)
                    {
                        var list = animal.PackMates ?? new List<IPackAnimal>();
                        list.AddRange(type.Value.Where(a => !a.Equals(animal)));
                        animal.PackMates = list;
                        animal.Leader = leader;

                        // Remove from list
                        type.Value.Remove(animal);

                        if (animal.Equals(leader)) continue;

                        int whileLoopLimiter = 0;

                        var newPos = centerPoint.GetRandomCoordinateWithinSquareRadius(5, customRandom: random);
                        while (!MapConsole.World.CellWalkable(newPos.X, newPos.Y))
                        {
                            if (whileLoopLimiter >= whileLoopLimit)
                            {
                                newPos = spawnPositions.TakeRandom(random);
                                break;
                            }
                            newPos = centerPoint.GetRandomCoordinateWithinSquareRadius(5, customRandom: random);
                            whileLoopLimiter++;
                        }

                        var entity = ((Entity)animal);
                        var wEntity = entity as IEntity;
                        wEntity.WorldPosition = newPos;
                        entity.IsVisible = MapConsole.World.IsWorldCoordinateOnViewPort(newPos.X, newPos.Y);
                        if (entity.IsVisible)
                        {
                            animal.Position = MapConsole.World.WorldToScreenCoordinate(newPos.X, newPos.Y);
                        }
                    }
                }
            }
        }

        public void GenerateVillages()
        {
            var coords = GetCellCoords(Width / 2, Height / 2, a => a.Walkable).TakeRandom(2).ToList();
            var village = new Village(coords.First(), 4, Color.Magenta);
            village.Initialize(this);
            _villages.Add(village);

            village = new Village(coords.Last(), 4, Color.Orange);
            village.Initialize(this);
            _villages.Add(village);
        }

        /// <summary>
        /// The actual visual displayed cell
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public override WorldCell GetCell(int x, int y)
        {
            var cell = base.GetCell(x, y);
            if (cell == null) return null;
            return cell;
        }

        public bool CellWalkable(int x, int y)
        {
            return GetCell(x, y).Walkable;
        }

        public IEnumerable<Point> GetCellCoords(int startX, int startY, Func<WorldCell, bool> criteria)
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
            var cellId = GetCell(coord.X, coord.Y).CellType;
            var items = Inventory.ItemCache.Where(a => a.Value.DroppedBy != null && a.Value.IsDroppedBy(cellId))
                .Select(a => a.Key)
                .ToList();
            return items;
        }

        public IEnumerable<WorldCell> GetCells(IEnumerable<Point> points)
        {
            return base.GetCells(points.Select(a => (a.X, a.Y)));
        }

        public IEnumerable<(int x, int y)> GetViewPortWorldCoordinates()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    yield return ScreenToWorldCoordinate(x, y);
                }
            }
        }

        public void HideObstructedCells()
        {
            // Rework this to skip neighbors that go off the screen
            var viewPortWorldPositions = GetCells(GetViewPortWorldCoordinates());
            var cache = viewPortWorldPositions.ToDictionary(a => (a.X, a.Y));
            foreach (var cell in viewPortWorldPositions)
            {
                var neighborPositions = new Point(cell.X, cell.Y).Get4Neighbors();
                var neighbors = neighborPositions.Select(a =>
                {
                    if (!cache.TryGetValue(a, out WorldCell neighborCell))
                        cache.Add(a, GetCell(a.X, a.Y));
                    return neighborCell ?? cache[a];
                });
                if (neighbors.All(a => !a.SeeThrough) && cell.CellType != 8)
                {
                    cell.IsVisible = false;
                    SetCell(cell);
                }
                else
                {
                    cell.IsVisible = true;
                    SetCell(cell);
                }
            }
        }
    }
}
