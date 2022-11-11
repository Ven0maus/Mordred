using Mordred.Graphics.Consoles;
using SadConsole;
using SadConsole.UI.Controls;
using SadRogue.Primitives;
using Venomaus.FlowVitae.Cells;
using AdjacencyRule = Venomaus.FlowVitae.Grids.AdjacencyRule;

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
        public int TerrainId { get; private set; } = Constants.WorldSettings.VoidTile;
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
        public WorldLayer Layer { get; private set; }
        /// <summary>
        /// The name of the cell
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The bitmask based on neighbor types
        /// </summary>
        public int BitMask
        {
            get { return GetBitMaskValue(); }
        }

        public int X { get; set; }
        public int Y { get; set; }
        /// <summary>
        /// The unique cell type based on the terrain
        /// </summary>
        public int CellType { get; set; }

        public WorldCell() { }

        public WorldCell(int terrainId, int cellId, Color foreground, Color background, string name, int glyph, WorldLayer layer, bool walkable, bool transparent, bool isAdditional = false) : 
            base(foreground, background, glyph)
        {
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
            X = original.X;
            Y = original.Y;
            TerrainId = original.TerrainId;
            CellType = original.CellType;
            Name = original.Name;
            Walkable = original.Walkable;
            SeeThrough = original.SeeThrough;
            Layer = original.Layer;
            IsVisible = original.IsVisible;
        }

        private int GetBitMaskValue()
        {
            var neighbors = WorldWindow.World.GetNeighbors(Layer, X, Y, AdjacencyRule.FourWay);
            int count = 0;
            foreach (var neighbor in neighbors)
            {
                if (neighbor.TerrainId != TerrainId) continue;
                if (neighbor.Y == Y + 1)
                    count += 1;
                else if (neighbor.X == X + 1)
                    count += 2;
                else if (neighbor.Y == Y - 1)
                    count += 4;
                else if (neighbor.X == X - 1)
                    count += 8;
            }
            return count;
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
