using System;

namespace Mordred.Config.ItemConfig
{
    [Serializable]
    public class WorldItemObject
    {
        public int id;
        public string glyph;
        public string name;
        public string foreground;
        public string background;
        public bool edible;
        public double edibleWorth;
        public string[] droppedBy;
    }

    [Serializable]
    public class ItemsObject
    {
        public WorldItemObject[] items;
    }
}
