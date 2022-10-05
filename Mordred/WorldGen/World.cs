using GoRogue;
using GoRogue.MapViews;
using GoRogue.Pathing;
using Microsoft.Xna.Framework;
using Mordred.Config;
using Mordred.Entities;
using Mordred.Entities.Animals;
using Mordred.GameObjects.ItemInventory;
using Mordred.Graphics.Consoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Mordred.WorldGen
{
    public enum WorldLayer
    {
        TERRAIN,
        OBJECTS,
        ITEMS,
        ENTITIES
    }

    public class World
    {
        public readonly int Width, Height;
        public static readonly Dictionary<int, WorldCell[]> WorldCells = ConfigLoader.LoadWorldCells();

        public readonly FastAStar Pathfinder;

        protected readonly MapConsole MapConsole;
        protected readonly List<Village> Villages;
        /// <summary>
        /// The visual cells that are displayed.
        /// </summary>
        protected readonly WorldCell[] Cells;
        /// <summary>
        /// The actual terrain values, which objects can overlap
        /// </summary>
        protected readonly int[] Terrain;
        protected readonly ArrayMap<bool> Walkability;

        public World(int width, int height)
        {
            Width = width;
            Height = height;

            // Get map console reference
            MapConsole = Game.Container.GetConsole<MapConsole>();

            // Initialize the arrays
            Cells = new WorldCell[Width * Height];
            Terrain = new int[Width * Height];
            Villages = new List<Village>(Constants.VillageSettings.MaxVillages);
            Walkability = new ArrayMap<bool>(Width, Height);
            Pathfinder = new FastAStar(Walkability, Distance.MANHATTAN);
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
                        SetCell(x, y, WorldCells[3].TakeRandom());
                        Terrain[y * Width + x] = 3;
                    }
                    else
                    {
                        // Tree, berrybush or grass
                        int chance = Game.Random.Next(0, 100);
                        if (chance <= 1)
                            SetCell(x, y, WorldCells[7].TakeRandom());
                        else if (chance <= 7)
                            SetCell(x, y, WorldCells[2].TakeRandom());
                        else
                            SetCell(x, y, WorldCells[1].TakeRandom());
                        Terrain[y * Width + x] = 1;
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
                    var centerPoint = (Coord)(leader as Animal).Position;
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
            Villages.Add(village);

            village = new Village(coords.Last(), 4, Color.Orange);
            village.Initialize(this);
            Villages.Add(village);
        }

        /// <summary>
        /// The underlying terrain of a cell
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public WorldCell GetTerrain(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return WorldCells[Terrain[y * Width + x]].TakeRandom().Clone();
        }

        /// <summary>
        /// The actual visual displayed cell
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public WorldCell GetCell(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return Cells[y * Width + x].Clone();
        }

        public bool CellWalkable(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            return Cells[y * Width + x].Walkable;
        }

        public IEnumerable<WorldCell> GetCells(Func<WorldCell, bool> criteria)
        {
            for (int y=0; y < Height; y++)
            {
                for (int x=0; x < Width; x++)
                {
                    if (criteria.Invoke(Cells[y * Width + x]))
                        yield return Cells[y * Width + x].Clone();
                }
            }
        }

        public IEnumerable<Coord> GetCellCoords(Func<WorldCell, bool> criteria)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (criteria.Invoke(Cells[y * Width + x]))
                        yield return new Coord(x, y);
                }
            }
        }

        /// <summary>
        /// Returns a list of all the WorldItem Id's that the given WorldCell drops
        /// </summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        public List<int> GetItemIdDropsByCellId(int cellId)
        {
            var items = Inventory.ItemCache.Where(a => a.Value.DroppedBy != null && a.Value.IsDroppedBy(cellId))
                .Select(a => a.Key)
                .ToList();
            return items;
        }

        /// <summary>
        /// Returns a list of all the WorldItem Id's that the given WorldCell drops
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public List<int> GetItemIdDropsByCellId(Coord coord)
        {
            var cellId = GetCell(coord.X, coord.Y).CellId;
            var items = Inventory.ItemCache.Where(a => a.Value.DroppedBy != null && a.Value.IsDroppedBy(cellId))
                .Select(a => a.Key)
                .ToList();
            return items;
        }

        /// <summary>
        /// Returns a list of all the WorldCell Id's that drop the given item id
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public IEnumerable<int> GetCellIdDropsByItemId(int itemId)
        {
            return Inventory.ItemCache[itemId].GetCellDropIds();
        }

        public void SetCell(int x, int y, WorldCell cell)
        {
            if (!InBounds(x, y)) return;

            if (Cells[y * Width + x] == null)
                Cells[y * Width + x] = cell.Clone();
            else
                Cells[y * Width + x].CopyFrom(cell);

            Walkability[y * Width + x] = cell.Walkable;
        }

        public List<WorldCell> Get4Neighbors(int x, int y)
        {
            var cells = new List<WorldCell>();
            if (!InBounds(x, y)) return cells;

            if (InBounds(x + 1, y)) cells.Add(GetCell(x+1, y));
            if (InBounds(x - 1, y)) cells.Add(GetCell(x-1, y));
            if (InBounds(x, y + 1)) cells.Add(GetCell(x, y+1));
            if (InBounds(x, y - 1)) cells.Add(GetCell(x, y-1));

            return cells;
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        private void HideObstructedCells()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cells = Get4Neighbors(x, y);
                    if (cells.All(a => !a.Transparent))
                    {
                        Cells[y * Width + x].IsVisible = false;
                    }
                    else
                    {
                        Cells[y * Width + x].IsVisible = true;
                    }
                }
            }
        }

        public void Render(bool hideObstructedCells = true, bool setSurface = false)
        {
            if (hideObstructedCells)
                HideObstructedCells();

            if (setSurface)
                MapConsole.SetSurface(Cells, Width, Height);
            else
                MapConsole.IsDirty = true;
        }
    }
}
