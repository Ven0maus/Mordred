using GoRogue.Pathing;
using Mordred.Config;
using Mordred.Entities;
using Mordred.Entities.Animals;
using Mordred.GameObjects.ItemInventory;
using Mordred.Graphics.Consoles;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;
using System;
using System.Collections.Generic;
using System.Linq;
using Venomaus.FlowVitae.Basics;
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

    public class World : GridBase<int, WorldCell>
    {
        public static readonly Dictionary<int, WorldCell[]> WorldCells = ConfigLoader.LoadWorldCells();

        public readonly FastAStar Pathfinder;

        protected readonly MapConsole MapConsole;
        protected readonly List<Village> _villages;
        
        public IReadOnlyList<Village> Villages
        {
            get { return _villages; }
        }

        /// <summary>
        /// The actual terrain values, which objects can overlap
        /// </summary>
        protected readonly Grid<int, WorldCell> Terrain;
        protected readonly ArrayView<bool> Walkability;

        public World(int width, int height) : base(width, height)
        {
            // Get map console reference
            MapConsole = Game.Container.GetConsole<MapConsole>();
            OnCellUpdate += MapConsole.OnCellUpdate;
            RaiseOnlyOnCellTypeChange = false;

            // Initialize the arrays
            Terrain = new Grid<int, WorldCell>(width, height);
            _villages = new List<Village>(Constants.VillageSettings.MaxVillages);
            Walkability = new ArrayView<bool>(Width, Height);
            Pathfinder = new FastAStar(Walkability, Distance.Manhattan);
        }

        protected override WorldCell Convert(int x, int y, int cellType)
        {
            // Get custom cell
            var cell = GetRandomCellConfig(cellType, x, y);
            if (cell == null) return base.Convert(x, y, cellType);
            cell.X = x;
            cell.Y = y;
            return cell;
        }

        public static WorldCell GetRandomCellConfig(int type, int x, int y, bool clone = true)
        {
            var cell = clone ? WorldCells[type].TakeRandom().Clone() : WorldCells[type].TakeRandom();
            cell.X = x;
            cell.Y = y;
            return cell;
        }

        public void GenerateLands()
        {
            // Idk, need to rework..
            var simplex1 = new OpenSimplex2F(Game.Random.Next(-500000, 500000));
            var simplexNoise = new double[Width * Height];
            for (int y=0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    double nx = (double)x / Width - 0.5d;
                    double ny = (double)y / Height - 0.5d;
                    simplexNoise[y * Width + x] = simplex1.Noise2(nx, ny);
                }
            }

            // Normalize
            simplexNoise = OpenSimplex2F.Normalize(simplexNoise);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (simplexNoise[y * Width + x] >= 0.75f && simplexNoise[y * Width + x] <= 1f)
                    {
                        // Mountains
                        SetCell(x, y, 3);
                        Terrain.SetCell(x, y, 3);
                    }
                    else
                    {
                        // Tree, berrybush or grass
                        int chance = Game.Random.Next(0, 100);
                        if (chance <= 1)
                            SetCell(x, y, 7);
                        else if (chance <= 7)
                            SetCell(x, y, 2);
                        else
                            SetCell(x, y, 1);
                        Terrain.SetCell(x, y, 1);
                    }
                }
            }
        }

        public void GenerateWildLife()
        {
            var wildLifeCount = Game.Random.Next(Constants.WorldSettings.MinWildLife, Constants.WorldSettings.MaxWildLife + 1);
            var spawnPositions = GetCellCoords(a => a.Walkable).ToList();

            // Get all classes that inherit from PassiveAnimal
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
                    var centerPoint = (Point)(leader as Animal).Position;
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

                        (animal as Animal).Position = newPos;
                    }
                    
                }
            }
        }

        public void GenerateVillages()
        {
            var coords = GetCellCoords(a => a.Walkable).TakeRandom(2).ToList();
            var village = new Village(coords.First(), 4, Color.Magenta);
            village.Initialize(this);
            _villages.Add(village);

            village = new Village(coords.Last(), 4, Color.Orange);
            village.Initialize(this);
            _villages.Add(village);
        }

        /// <summary>
        /// The underlying terrain of a cell
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int GetTerrain(int x, int y)
        {
            if (!InBounds(x, y)) return -1;
            return Terrain.GetCellType(x, y);
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
            if (!InBounds(x, y)) return false;
            return GetCell(x, y).Walkable;
        }

        public IEnumerable<Point> GetCellCoords(Func<WorldCell, bool> criteria)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (criteria.Invoke(GetCell(x, y)))
                        yield return new Point(x, y);
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

        public override void SetCell(WorldCell cell, bool storeState = false)
        {
            if (!InBounds(cell.X, cell.Y)) return;
            Walkability[cell.Y * Width + cell.X] = cell.Walkable;
            base.SetCell(cell, storeState);
        }

        public IEnumerable<WorldCell> GetCells(IEnumerable<Point> points)
        {
            return base.GetCells(points.Select(a => (a.X, a.Y)));
        }

        public void HideObstructedCells()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cells = GetCells(new Point(x, y)
                        .Get4Neighbors()
                        .Where(a => InBounds(a.X, a.Y)));
                    var cell = GetCell(x, y);
                    if (cells.All(a => !a.Transparent))
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
}
