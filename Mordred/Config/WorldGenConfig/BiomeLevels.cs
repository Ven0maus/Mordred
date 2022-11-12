using System;

namespace Mordred.Config.WorldGenConfig
{
    [Serializable]
    public class BiomeLevels
    {
        public Level[] moisture;
        public Level[] temperature;
    }

    [Serializable]
    public class Level
    {
        public string name;
        public double layer;
    }
}
