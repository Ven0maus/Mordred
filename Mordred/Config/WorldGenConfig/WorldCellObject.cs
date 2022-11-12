using Mordred.WorldGen;
using Newtonsoft.Json;
using System;

namespace Mordred.Config.WorldGenConfig
{
    [Serializable]
    public class WorldCellObject
    {
        // Filled by the config loader
        [JsonIgnore]
        public int mainId;
        [JsonIgnore]
        public WorldLayer layer;

        /// <summary>
        /// A unique code that can be used in scripting to reference a specific object
        /// </summary>
        public string code;
        public string glyph;
        public string[] additionalGlyphs;
        public string name;
        public string foreground;
        public string background;
        public bool seeThrough;
        public bool walkable;

        // World gen
        public int spawnChance; // 0-100%
        public double minHeight = -1;
        public double maxHeight = -1;
        public string biome;

        // Items
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
