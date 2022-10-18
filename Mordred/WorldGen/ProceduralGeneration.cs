using Mordred.Config;
using Mordred.Config.WorldGenConfig;
using Mordred.Entities.Animals;
using Mordred.Entities;
using Mordred.Graphics.Consoles;
using SadConsole.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Venomaus.FlowVitae.Chunking;
using Venomaus.FlowVitae.Procedural;
using SadRogue.Primitives;

namespace Mordred.WorldGen
{
    public class ProceduralGeneration : IProceduralGen<int, WorldCell>
    {
        public int Seed { get; }
        private readonly OpenSimplex2F _simplex;
        private readonly WorldCellObject[] _proceduralTerrain;

        public ProceduralGeneration(int seed)
        {
            Seed = seed;
            _simplex = new OpenSimplex2F(Seed);
            _proceduralTerrain = ConfigLoader.GetTerrains(a => a.spawnChance > 0).ToArray();
        }

        public (int[] chunkCells, IChunkData chunkData) Generate(int seed, int width, int height, (int x, int y) chunkCoordinate)
        {
            var random = new Random(seed);
            var chunk = new int[width * height];

            // Some basic simplex noise. Idk, need to rework..
            var simplexNoise = new double[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int chunkX = chunkCoordinate.x + x;
                    int chunkY = chunkCoordinate.y + y;
                    double nx = (double)chunkX / width - 0.5d;
                    double ny = (double)chunkY / height - 0.5d;
                    simplexNoise[y * width + x] = _simplex.Noise2(nx, ny);
                }
            }

            // Normalize
            simplexNoise = OpenSimplex2F.Normalize(simplexNoise);

            // Generate based on simplex noise created above
            GenerateLands(random, chunk, width, height, simplexNoise);
            return (chunk, null);
        }

        private void GenerateLands(Random random, int[] chunk, int width, int height, double[] simplexNoise)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if ((x == 0 || y == 0 || x == width - 1 || y == height - 1) && Constants.GameSettings.DebugMode)
                    {
                        chunk[y * width + x] = ConfigLoader.GetRandomWorldCellTypeByTerrain(7, random);
                        continue;
                    }

                    foreach (var terrain in _proceduralTerrain.OrderByDescending(a => a.spawnChance))
                    {
                        var maxLayer = terrain.maxSpawnLayer == 1 ? 1.1 : terrain.maxSpawnLayer;
                        if (simplexNoise[y * width + x] >= terrain.minSpawnLayer &&
                            simplexNoise[y * width + x] < maxLayer)
                        {
                            if (random.Next(0, 100) < terrain.spawnChance)
                            {
                                chunk[y * width + x] = ConfigLoader.GetRandomWorldCellTypeByTerrain(terrain.id, random);
                            }
                        }
                    }
                }
            }

            // TODO: Revisit this to solve border problem with !InBounds being on the border, but next tile is see through
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var neighbors = new Point(x, y)
                        .Get4Neighbors()
                        .Count(a => !InBounds(a.X, a.Y, width, height) || !ConfigLoader.WorldCells[chunk[a.Y * width + a.X]].SeeThrough);
                    if (neighbors == 4)
                        chunk[y * width + x] = ConfigLoader.GetRandomWorldCellTypeByTerrain(0, random);
                }
            }
        }
        
        private static bool InBounds(int x, int y, int width, int height)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }

        public static void GenerateWildLife((int x, int y) chunkCoordinates, int passiveAmount, int predatorAmount, Random random = null)
        {
            var world = MapConsole.World;
            random ??= new Random(world.GetChunkSeed(chunkCoordinates.x, chunkCoordinates.y));
            
            var (x, y) = (chunkCoordinates.x + world.ChunkWidth / 2, chunkCoordinates.y + world.ChunkHeight / 2);
            var spawnPositions = world.GetCellCoordsFromCenter(x, y, a => a.Walkable).ToList();

            // Get all classes that inherit from PassiveAnimal / PredatorAnimal
            var passiveAnimals = ReflectiveEnumerator.GetEnumerableOfType<PassiveAnimal>().ToList();
            var predatorAnimals = ReflectiveEnumerator.GetEnumerableOfType<PredatorAnimal>().ToList();

            var packAnimals = new Dictionary<Type, List<IPackAnimal>>();
            // Automatic selection of all predators
            for (int i = 0; i < predatorAmount; i++)
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

            int nonPredators = passiveAmount;
            // Automatic selection of passive animals
            for (int i = 0; i < nonPredators; i++)
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

        public static void GenerateWildLife(int chunkX, int chunkY)
        {
            var random = new Random(MapConsole.World.GetChunkSeed(chunkX, chunkY));
            var wildLifeCount = random.Next(Constants.WorldSettings.WildLife.MinWildLifePerChunk, Constants.WorldSettings.WildLife.MaxWildLifePerChunk + 1);
            int predators = (int)Math.Round((double)wildLifeCount / 100 * Constants.WorldSettings.WildLife.PercentagePredators);
            int passives = wildLifeCount - predators;

            GenerateWildLife((chunkX, chunkY), passives, predators, random);
        }

        public static void GenerateVillages(int chunkX, int chunkY)
        {
            var world = MapConsole.World;
            var random = new Random(world.GetChunkSeed(chunkX, chunkY));
            var villagesToCreate = random.Next(0, Constants.VillageSettings.MaxVillagesPerChunk + 1);
            if (villagesToCreate == 0) return;

            // TODO: Store villages per chunk somewhere? Do i need access to it to continue building/expanding?
            var villages = new List<Village>(villagesToCreate);
            var walkableCoords = world.GetCellCoordsFromCenter(chunkX + world.ChunkWidth / 2, chunkY + world.ChunkHeight / 2, a => a.Walkable);
            for (int i=0; i < villagesToCreate; i++)
            {
                var coord = walkableCoords.TakeRandom(random);
                var village = new Village(coord, 5, Color.MonoGameOrange);
                village.Initialize(world);
                villages.Add(village);
            }
        }
    }
}
