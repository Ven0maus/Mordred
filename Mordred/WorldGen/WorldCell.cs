using SadConsole;
using SadRogue.Primitives;
using Venomaus.FlowVitae.Basics;

namespace Mordred.WorldGen
{
    /// <summary>
    /// Base class of a grid cell
    /// </summary>
    public class WorldCell : ColoredGlyph, ICell<int>
    {
        public int CellType { get; set; }
        /// <summary>
        /// Can entities walk on this cell
        /// </summary>
        public bool Walkable { get; set; }
        /// <summary>
        /// Can other cells be seen through this cell
        /// </summary>
        public bool Transparent { get; set; }
        /// <summary>
        /// The layer this cell is build on
        /// </summary>
        public int Layer { get; private set; }
        /// <summary>
        /// The name of the cell
        /// </summary>
        public string Name { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }

        public WorldCell() { }

        public WorldCell(int cellId, Color foreground, Color background, string name, int glyph, int layer, bool walkable, bool transparent) : 
            base(foreground, background, glyph)
        {
            CellType = cellId;
            Name = name;
            Walkable = walkable;
            Transparent = transparent;
            Layer = layer;
        }

        public WorldCell(WorldCell original) : base(original.Foreground, original.Background, original.Glyph)
        {
            X = original.X;
            Y = original.Y;
            CellType = original.CellType;
            Name = original.Name;
            Walkable = original.Walkable;
            Transparent = original.Transparent;
            Layer = original.Layer;
            IsVisible = original.IsVisible;
        }

        public new WorldCell Clone()
        {
            return new WorldCell(this);
        }

        public bool Equals(ICell<int> other)
        {
            if (other == null) return false;
            return X == other.X && Y == other.Y;
        }

        public bool Equals((int x, int y) other)
        {
            return other.x == X && other.y == Y;
        }
    }
}
