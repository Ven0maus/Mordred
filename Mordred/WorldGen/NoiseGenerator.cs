using Vector2 = Microsoft.Xna.Framework.Vector2;
using System;

namespace Mordred.WorldGen
{
    public static class NoiseGenerator
    {
        public static double[] GenerateNoiseMap(OpenSimplex2F simplex, int mapWidth, int mapHeight, double scale, Vector2 offset)
        {
            double[] noiseMap = new double[mapWidth * mapHeight];

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
                    // Calculate noise for each octave
                    // We sample a point (x,y)
                    double chunkX = offset.X + x;
                    double chunkY = offset.Y + y;
                    double sampleX = (chunkX - halfWidth) / scale;
                    double sampleY = (chunkY - halfHeight) / scale;

                    // Use unity's implementation of perlin noise
                    double simplexValue = simplex.Noise2(sampleX, sampleY) * 2 - 1;

                    // Assign our noise
                    noiseMap[y * mapWidth + x] = simplexValue;
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
