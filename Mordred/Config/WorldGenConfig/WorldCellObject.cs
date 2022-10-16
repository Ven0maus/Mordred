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
        public bool transparent;
        public bool walkable;
        public bool isResource;
    }

    [Serializable]
    public class TerrainObject
    {
        public WorldCellObject[] cells;
    }
}
