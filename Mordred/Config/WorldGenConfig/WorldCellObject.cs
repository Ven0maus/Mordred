using System;

namespace Mordred.Config.WorldGenConfig
{
    [Serializable]
    public class WorldCellObject
    {
        public int id;
        public string glyph;
        public string[] additionalGlyphs;
        public string name;
        public string foreground;
        public string background;
        public string layer;
        public bool seeThrough;
        public bool walkable;

        // World gen
        public int spawnChance; // 0-100%
        public float minSpawnLayer; // Inclusive
        public float maxSpawnLayer; // Exclusive
        public int[] spawnOnTerrain;

        // Resource regrowth
        public bool renawable;
        public int minResourceAmount;
    }

    [Serializable]
    public class TerrainObject
    {
        public WorldCellObject[] cells;
    }
}
