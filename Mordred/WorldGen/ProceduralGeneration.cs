using Mordred.Config;
using Mordred.Config.WorldGenConfig;
using System;
using Venomaus.FlowVitae.Chunking;
using Venomaus.FlowVitae.Procedural;

namespace Mordred.WorldGen
{
    public class ProceduralGeneration : IProceduralGen<int, WorldCell>
    {
        public int Seed { get; }

        public ProceduralGeneration(int seed)
        {
            Seed = seed;
        }

        public (int[] chunkCells, IChunkData chunkData) Generate(int seed, int width, int height, (int x, int y) chunkCoordinate)
        {
            var random = new Random(seed);
            var chunk = new int[width * height];
            GenerateLands(random, chunk, width, height, chunkCoordinate);
            return (chunk, null);
        }

        public void GenerateLands(Random random, int[] chunk, int width, int height, (int x, int y) chunkCoordinate)
        {
            // Idk, need to rework..
            var simplex1 = new OpenSimplex2F(Seed);
            var simplexNoise = new double[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int chunkX = chunkCoordinate.x + x;
                    int chunkY = chunkCoordinate.y + y;
                    double nx = (double)chunkX / width - 0.5d;
                    double ny = (double)chunkY / height - 0.5d;
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
                        chunk[y * width + x] = ConfigLoader.GetRandomTerrainCell((int)WorldTiles.Border, x, y).CellType;
                    else if (simplexNoise[y * width + x] >= 0.75f && simplexNoise[y * width + x] <= 1f)
                    {
                        // Mountains
                        chunk[y * width + x] = ConfigLoader.GetRandomTerrainCell((int)WorldTiles.Mountain, x, y).CellType;
                    }
                    else
                    {
                        // Tree, berrybush or grass
                        int chance = random.Next(0, 100);
                        if (chance <= 1)
                            chunk[y * width + x] = ConfigLoader.GetRandomTerrainCell((int)WorldTiles.BerryBush, x, y).CellType;
                        else if (chance <= 7)
                            chunk[y * width + x] = ConfigLoader.GetRandomTerrainCell((int)WorldTiles.Tree, x, y).CellType;
                        else
                            chunk[y * width + x] = ConfigLoader.GetRandomTerrainCell((int)WorldTiles.Grass, x, y).CellType;
                    }
                }
            }
        }
    }
}
