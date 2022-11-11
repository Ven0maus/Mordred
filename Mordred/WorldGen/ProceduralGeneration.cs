using Mordred.Config;
using Mordred.Config.WorldGenConfig;
using Mordred.Entities;
using Mordred.Entities.Animals;
using Mordred.Graphics.Consoles;
using SadConsole.Entities;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Venomaus.FlowVitae.Chunking;
using Venomaus.FlowVitae.Chunking.Generators;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Mordred.WorldGen
{
    public class ProceduralGeneration : IProceduralGen<int, WorldCell>
    {
        public int Seed { get; }
        private readonly OpenSimplex2F _simplex;
        private readonly WorldCellObject[] _proceduralTerrain;
        private readonly WorldLayer _layer;

        public ProceduralGeneration(int seed, WorldLayer layer)
        {
            Seed = seed;
            _layer = layer;
            _simplex = new OpenSimplex2F(Seed);
            _proceduralTerrain = ConfigLoader.GetTerrains(a => a.spawnChance > 0 && a.layer == _layer).ToArray();
        }

        public (int[] chunkCells, IChunkData chunkData) Generate(int seed, int width, int height, (int x, int y) chunkCoordinate)
        {
            var random = new Random(seed);
            var chunk = new int[width * height];

            var scale = 21d;
            var octaves = 1;
            var persistance = 0.286d;
            var lacunarity = 1.9d;
            var offset = new Vector2(chunkCoordinate.x, chunkCoordinate.y);

            // Generate noise map based on simplex noise
            var heightMap = NoiseGenerator.GenerateNoiseMap(_simplex, width, height, seed, scale, octaves, persistance, lacunarity, offset);
            
            /*
            // Generate heatMap
            var heatMap = NoiseGenerator.GenerateNoiseMap(_simplex, width, height, seed, 12, 1, 0.1246, 1.2, offset);

            // Substract height from heatMap
            var minheight = heightMap.Min();
            var maxHeight = heightMap.Max();
            NoiseGenerator.Substract(width, height, heatMap, heightMap, (height) => 
            NoiseGenerator.Lerp(0, 0.4, OpenSimplex2F.Normalize(maxHeight, minheight, height)));

            // TODO:
            // Create a moisture map based on wetter towards 0, dryer towards 1
            //var moistureMap = NoiseGenerator.GenerateNoiseMap(_simplex, width, height, seed, 7, 1, 0.0846, 0.8, offset);
            */

            // Normalize between 0 and 1
            heightMap = OpenSimplex2F.Normalize(heightMap);

            // Generate based on simplex noise created above
            GenerateLands(random, chunk, width, height, heightMap);
            return (chunk, null);
        }

        private void GenerateLands(Random random, int[] chunk, int width, int height, double[] simplexNoise)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Default value (empty)
                    chunk[y * width + x] = Constants.WorldSettings.VoidTile;

                    foreach (var terrain in _proceduralTerrain.OrderByDescending(a => a.spawnChance))
                    {
                        // Every layer starts at x and goes till < y, so the last layer needs to be included at the top
                        var maxLayer = terrain.maxSpawnLayer == 1 ? 1.1 : terrain.maxSpawnLayer;
                        if (simplexNoise[y * width + x] >= terrain.minSpawnLayer &&
                            simplexNoise[y * width + x] < maxLayer)
                        {
                            if (random.Next(0, 100) < terrain.spawnChance)
                            {
                                chunk[y * width + x] = ConfigLoader.GetRandomWorldCellTypeByTerrain(terrain.mainId, random);
                            }
                        }
                    }
                }
            }
        }

        public static void GenerateWildLife((int x, int y) chunkCoordinates, int passiveAmount, int predatorAmount, Random random = null)
        {
            var world = WorldWindow.World;
            random ??= new Random(world.GetChunkSeed(chunkCoordinates.x, chunkCoordinates.y));
            
            var (x, y) = (chunkCoordinates.x + world.ChunkWidth / 2, chunkCoordinates.y + world.ChunkHeight / 2);
            var spawnPositions = world.GetCellCoordsFromCenter(WorldLayer.TERRAIN, x, y, a => a.Walkable).ToList();

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
                        while (!WorldWindow.World.CellWalkable(newPos.X, newPos.Y))
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
                        entity.IsVisible = WorldWindow.World.IsWorldCoordinateOnViewPort(newPos.X, newPos.Y);
                        if (entity.IsVisible)
                        {
                            animal.Position = WorldWindow.World.WorldToScreenCoordinate(newPos.X, newPos.Y);
                        }
                    }
                }
            }
        }

        public static void GenerateWildLife(int chunkX, int chunkY)
        {
            var random = new Random(WorldWindow.World.GetChunkSeed(chunkX, chunkY));
            var wildLifeCount = random.Next(Constants.WorldSettings.WildLife.MinWildLifePerChunk, Constants.WorldSettings.WildLife.MaxWildLifePerChunk + 1);
            int predators = (int)Math.Round((double)wildLifeCount / 100 * Constants.WorldSettings.WildLife.PercentagePredators);
            int passives = wildLifeCount - predators;

            GenerateWildLife((chunkX, chunkY), passives, predators, random);
        }

        public static void GenerateVillages(int chunkX, int chunkY)
        {
            var world = WorldWindow.World;
            var random = new Random(world.GetChunkSeed(chunkX, chunkY));
            var spawnChance = random.Next(0, 100);
            if (spawnChance >= Constants.VillageSettings.VillageSpawnChange) 
                return;

            // TODO: Store villages per chunk somewhere? Do i need access to it to continue building/expanding?
            var walkableCoords = world.GetCellCoordsFromCenter(WorldLayer.TERRAIN, chunkX + world.ChunkWidth / 2, chunkY + world.ChunkHeight / 2, a => a.Walkable);
            var coord = walkableCoords.TakeRandom(random);
            var village = new Village(coord, 5, Color.MonoGameOrange, random);
            village.Initialize(world);
        }
    }
}
