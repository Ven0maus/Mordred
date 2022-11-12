using Vector2 = Microsoft.Xna.Framework.Vector2;
using System;
using Newtonsoft.Json.Linq;

namespace Mordred.WorldGen
{
    public static class NoiseGenerator
    {
        public static double[] GenerateNoiseMap(OpenSimplex2F simplex, int mapWidth, int mapHeight, int seed, double scale, int octaves, double persistance, double lacunarity, Vector2 offset)
        {
            double[] noiseMap = new double[mapWidth * mapHeight];

            var random = new Random(seed);

            // We need atleast one octave
            if (octaves < 1)
            {
                octaves = 1;
            }

            Vector2[] octaveOffsets = new Vector2[octaves];
            for (int i = 0; i < octaves; i++)
            {
                double offsetX = random.Next(-100000, 100000) + offset.X;
                double offsetY = random.Next(-100000, 100000) + offset.Y;
                octaveOffsets[i] = new Vector2((float)offsetX, (float)offsetY);
            }

            if (scale <= 0f)
            {
                scale = 0.0001f;
            }

            // When changing noise scale, it zooms from top-right corner
            // This will make it zoom from the center
            double halfWidth = mapWidth / 2f;
            double halfHeight = mapHeight / 2f;

            for (int x = 0, y; x < mapWidth; x++)
            {
                for (y = 0; y < mapHeight; y++)
                {
                    // Define base values for amplitude, frequency and noiseHeight
                    double amplitude = 1;
                    double frequency = 1;
                    double noiseHeight = 0;

                    // Calculate noise for each octave
                    for (int i = 0; i < octaves; i++)
                    {
                        // We sample a point (x,y)
                        double sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].X;
                        double sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].Y;

                        // Use unity's implementation of perlin noise
                        double simplexValue = simplex.Noise2_XBeforeY(sampleX, sampleY) * 2 - 1;

                        // noiseHeight is our final noise, we add all octaves together here
                        noiseHeight += simplexValue * amplitude;
                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }

                    // Assign our noise
                    noiseMap[y * mapWidth + x] = noiseHeight;
                }
            }

            return noiseMap;
        }

        public static void Substract(int width, int height, double[] noise1, double[] noise2, Func<double, double> modifier = null)
        {
            for (int x=0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var value = noise2[y * width + x];
                    double substractModifier = 1f;
                    if (modifier != null)
                        substractModifier = modifier.Invoke(value);
                    noise1[y * width + x] -= substractModifier * value;
                }
            }
        }

        public static void Add(int width, int height, double[] noise1, double[] noise2, Func<double, double> modifier = null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var value = noise2[y * width + x];
                    double substractModifier = 1f;
                    if (modifier != null)
                        substractModifier = modifier.Invoke(value);
                    noise1[y * width + x] += substractModifier * value;
                }
            }
        }

        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * Clamp01(t);
        }

        private static double Clamp01(double value)
        {
            if (value < 0F)
                return 0F;
            else if (value > 1F)
                return 1F;
            else
                return value;
        }
    }
}
