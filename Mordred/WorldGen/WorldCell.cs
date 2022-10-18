using SadConsole;
using SadRogue.Primitives;
using Venomaus.FlowVitae.Cells;

namespace Mordred.WorldGen
{
    /// <summary>
    /// Base class of a grid cell
    /// </summary>
    public class WorldCell : ColoredGlyph, ICell<int>
    {
        /// <summary>
        /// The type of terrain this cell belongs to
        /// </summary>
        public int TerrainId { get; private set; }
        /// <summary>
        /// The unique cell type based on the terrain
        /// </summary>
        public int CellType { get; set; }
        /// <summary>
        /// Can entities walk on this cell
        /// </summary>
        public bool Walkable { get; set; }
        /// <summary>
        /// Can other cells be seen through this cell
        /// </summary>
        public bool SeeThrough { get; set; }
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

        private readonly bool _isAdditional = false;

        public WorldCell(int terrainId, int cellId, Color foreground, Color background, string name, int glyph, int layer, bool walkable, bool transparent, bool isAdditional = false) : 
            base(foreground, background, glyph)
        {
            _isAdditional = isAdditional;

            if (Foreground == Color.Transparent && Background == Color.Transparent)
            {
                IsVisible = false;
            }

            TerrainId = terrainId;
            CellType = cellId;
            Name = name;
            Walkable = walkable;
            SeeThrough = transparent;
            Layer = layer;
        }

        public WorldCell(WorldCell original) : base(original.Foreground, original.Background, original.Glyph)
        {
            _isAdditional = original._isAdditional;
            X = original.X;
            Y = original.Y;
            TerrainId = original.TerrainId;
            CellType = original.CellType;
            Name = original.Name;
            Walkable = original.Walkable;
            SeeThrough = original.SeeThrough;
            Layer = original.Layer;
            IsVisible = original.IsVisible;

            if (IsVisible && _isAdditional)
            {
                var newColor = Color.Lerp(Foreground, Game.Random.Next(0, 2) == 1 ? Color.Black : Color.White, (float)Game.Random.Next(1, 4) / 10);
                Foreground = newColor;
            }
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
