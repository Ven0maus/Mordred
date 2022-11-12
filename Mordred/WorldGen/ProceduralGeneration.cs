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

            // Generate heat and moisture map
            var heatMap = NoiseGenerator.GenerateNoiseMap(_simplex, width, height, seed, 12, 1, 0.1246, 1.2, offset);
            NoiseGenerator.Substract(width, height, heatMap, heightMap, (height) =>
            {
                if (height < 0.5) return 0.2;
                if (height < 0.65) return 0.3;
                if (height < 0.8) return 0.4;
                if (height <= 1) return 0.5;
                return 0.1;
            });
            var moistureMap = NoiseGenerator.GenerateNoiseMap(_simplex, width, height, seed, 12, 1, 0.1246, 1.2, offset);
            NoiseGenerator.Add(width, height, heatMap, heightMap, (height) =>
            {
                if (height < 0.1) return 8;
                if (height < 0.2) return 3;
                if (height < 0.3) return 1;
                return 0.25;
            });

            // Normalize between 0 and 1
            heightMap = OpenSimplex2F.Normalize(heightMap);
            heatMap = OpenSimplex2F.Normalize(heatMap);
            moistureMap = OpenSimplex2F.Normalize(moistureMap);

            // Generate based on simplex noise created above
            GenerateBiomes(random, chunk, width, height, heightMap, heatMap, moistureMap);
            return (chunk, null);
        }

        private void GenerateBiomes(Random random, int[] chunk, int width, int height, double[] heightMap, double[] heatMap, double[] moistureMap)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var index = y * width + x;

                    // Set default value (empty)
                    chunk[index] = Constants.WorldSettings.VoidTile;

                    // Set heightmap tile if it fits
                    var heightValue = heightMap[index];
                    foreach (var terrain in _proceduralTerrain)
                    {
                        var maxHeight = terrain.maxHeight == 1 ? terrain.maxHeight + 0.1 : terrain.maxHeight;
                        if (heightValue >= terrain.minHeight && heightValue < maxHeight)
                        {
                            chunk[index] = ConfigLoader.GetRandomWorldCellTypeByTerrain(terrain.mainId, random);
                            break;
                        }
                    }

                    if (chunk[index] != Constants.WorldSettings.VoidTile) continue;

                    // Set biome tile
                    var moisture = ConfigLoader.GetMoisture(moistureMap[index]);
                    var temperature = ConfigLoader.GetTemperature(heatMap[index]);
                    var biome = ConfigLoader.GetBiome(moisture, temperature);
                    var terrains = _proceduralTerrain
                        .Where(a => a.biome != null && a.biome.Equals(biome, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(a => a.spawnChance);

                    int terrainPicked = Constants.WorldSettings.VoidTile;
                    foreach (var terrain in terrains)
                    {
                        // higher spawn chance goes first, so each terrain has a chance to be picked
                        if (random.Next(0, 100) < terrain.spawnChance)
                        {
                            terrainPicked = terrain.mainId;
                        }
                    }

                    if (terrainPicked != Constants.WorldSettings.VoidTile)
                        chunk[index] = ConfigLoader.GetRandomWorldCellTypeByTerrain(terrainPicked, random);
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
