using Mordred.Config;
using Mordred.Config.WorldGenConfig;
using System;
using System.Linq;
using Venomaus.FlowVitae.Chunking;
using Venomaus.FlowVitae.Procedural;

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
            _simplex = new OpenSimplex2F(seed);
            _proceduralTerrain = ConfigLoader.GetProceduralTerrains().ToArray();
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
                        chunk[y * width + x] = ConfigLoader.GetNewTerrainCell((int)WorldTiles.Border, x, y).CellType;
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
                                chunk[y * width + x] = ConfigLoader.GetRandomWorldCellTypeByTerrain(terrain.id);
                            }
                        }
                    }
                }
            }
        }
    }
}
