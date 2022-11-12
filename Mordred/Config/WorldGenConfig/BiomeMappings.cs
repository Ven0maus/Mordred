using System;

namespace Mordred.Config.WorldGenConfig
{
    public class BiomeMapping
    {
        public Level temperature;
        public Level moisture;
        public string biome;

        public BiomeMapping(Level temperature, Level moisture, string biome)
        {
            this.temperature = temperature;
            this.moisture = moisture;
            this.biome = biome;
        }
    }
}
