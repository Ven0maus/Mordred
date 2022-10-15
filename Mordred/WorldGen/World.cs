using GoRogue.Pathing;
using Mordred.Config;
using Mordred.Entities;
using Mordred.Entities.Animals;
using Mordred.GameObjects.Effects;
using Mordred.GameObjects.ItemInventory;
using Mordred.Graphics.Consoles;
using SadConsole.Entities;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;
using System;
using System.Collections.Generic;
using System.Linq;
using Venomaus.FlowVitae.Basics;
using Venomaus.FlowVitae.Basics.Chunking;
using Venomaus.FlowVitae.Basics.Procedural;
using Venomaus.FlowVitae.Grids;

namespace Mordred.WorldGen
{
    public enum WorldLayer
    {
        TERRAIN,
        OBJECTS,
        ITEMS,
        ENTITIES
    }

    public enum WorldTiles
    {
        Void = 0,
        Grass = 1,
        Tree = 2,
        Mountain = 3,
        Wall = 4,
        House = 5,
        BerryBush = 6,
        Border = 7
    }

    public class World : GridBase<int, WorldCell>
    {
        public static readonly Dictionary<int, WorldCell[]> TerrainCells = ConfigLoader.LoadWorldCells();
        public static readonly Dictionary<int, WorldCell> WorldCells = TerrainCells
            .SelectMany(a => a.Value)
            .ToDictionary(a => a.CellType, a => a);

        public readonly FastAStar Pathfinder;

        private readonly MapConsole MapConsole;
        private readonly List<Village> _villages;
        private readonly List<CellEffect> _cellEffects;
        
        public IReadOnlyList<Village> Villages
        {
            get { return _villages; }
        }

        protected readonly LambdaGridView<bool> Walkability;
        private bool _worldInitialized = false;

        public World(int width, int height) : base(width, height, 
            Constants.WorldSettings.ChunkWidth, Constants.WorldSettings.ChunkHeight, new ProceduralGenerator<int, WorldCell>(1000, GenerateLands))
        {
            // Get map console reference
            MapConsole = Game.Container.GetConsole<MapConsole>();
            OnCellUpdate += MapConsole.OnCellUpdate;
            RaiseOnlyOnCellTypeChange = false;

            // Initialize the arrays
            _villages = new List<Village>(Constants.VillageSettings.MaxVillages);
            _cellEffects = new List<CellEffect>();
            Walkability = new LambdaGridView<bool>(width, height, point => GetCell(point.X, point.Y).Walkable);
            Pathfinder = new FastAStar(Walkability, Distance.Manhattan);

            Game.GameTick += HandleEffects;
            _worldInitialized = true;
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
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        chunk[y * width + x] = GetRandomTerrainCell((int)WorldTiles.Border, x, y).CellType; // Border
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

            foreach (var entity in EntitySpawner.Entities.Where(a => a is IWorldEntity))
            {
                var wEntity = entity as IWorldEntity;
                entity.IsVisible = IsWorldCoordinateOnViewPort(wEntity.WorldPosition.X, wEntity.WorldPosition.Y);
                if (entity.IsVisible)
                {
                    entity.Position = WorldToScreenCoordinate(wEntity.WorldPosition.X, wEntity.WorldPosition.Y);
                }
            }
        }

        public void GenerateWildLife()
        {
            var wildLifeCount = Game.Random.Next(Constants.WorldSettings.MinWildLife, Constants.WorldSettings.MaxWildLife + 1);
            var spawnPositions = GetCellCoords(Width / 2, Height / 2, a => a.Walkable).ToList();

            // Get all classes that inherit from PassiveAnimal / PredatorAnimal
            var passiveAnimals = ReflectiveEnumerator.GetEnumerableOfType<PassiveAnimal>().ToList();
            var predatorAnimals = ReflectiveEnumerator.GetEnumerableOfType<PredatorAnimal>().ToList();

            // 20% Predators 80% Passive
            int predators = (int)Math.Round((double)wildLifeCount / 100 * 20);

            var packAnimals = new Dictionary<Type, List<IPackAnimal>>();
            // Automatic selection of all predators
            for (int i = 0; i < predators; i++)
            {
                var animal = predatorAnimals.TakeRandom();
                var pos = spawnPositions.TakeRandom();
                spawnPositions.Remove(pos);
                var entity = EntitySpawner.Spawn(animal, pos, Game.Random.Next(0, 2) == 1 ? Gender.Male : Gender.Female);

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
                var animal = passiveAnimals.TakeRandom();
                var pos = spawnPositions.TakeRandom();
                spawnPositions.Remove(pos);
                var entity = EntitySpawner.Spawn(animal, pos, Game.Random.Next(0, 2) == 1 ? Gender.Male : Gender.Female);

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
                    var leader = animals.TakeRandom();
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

                        var newPos = centerPoint.GetRandomCoordinateWithinSquareRadius(5);
                        while (!MapConsole.World.CellWalkable(newPos.X, newPos.Y))
                        {
                            if (whileLoopLimiter >= whileLoopLimit)
                            {
                                newPos = spawnPositions.TakeRandom();
                                break;
                            }
                            newPos = centerPoint.GetRandomCoordinateWithinSquareRadius(5);
                            whileLoopLimiter++;
                        }

                        var entity = ((Entity)animal);
                        var wEntity = entity as IWorldEntity;
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
            var sX = startX - Width / 2;
            var sY = startY - Height / 2;
            var eX = sX + Width;
            var eY = sY + Height;
            for (int y = sY; y < eY; y++)
            {
                for (int x = sX; x < eX; x++)
                {
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
                if (neighbors.All(a => !a.Transparent) && cell.CellType != 8)
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
