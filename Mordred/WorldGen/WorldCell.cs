using Microsoft.Xna.Framework;
using SadConsole;
using SharpDX.XAudio2;

namespace Mordred.WorldGen
{
    /// <summary>
    /// Base class of a grid cell
    /// </summary>
    public class WorldCell : Cell
    {
        public int CellId { get; private set; }
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

        public WorldCell(int cellId, Color foreground, Color background, string name, int glyph, int layer, bool walkable, bool transparent) : 
            base(foreground, background, glyph)
        {
            CellId = cellId;
            Name = name;
            Walkable = walkable;
            Transparent = transparent;
            Layer = layer;
        }

        public WorldCell(WorldCell original) : base(original.Foreground, original.Background, original.Glyph)
        {
            CellId = original.CellId;
            Name = original.Name;
            Walkable = original.Walkable;
            Transparent = original.Transparent;
            Layer = original.Layer;
        }

        public new WorldCell Clone()
        {
            return new WorldCell(this);
        }

        public void CopyFrom(WorldCell cell)
        {
            CopyAppearanceFrom(cell);
            CellId = cell.CellId;
            Walkable = cell.Walkable;
            Transparent = cell.Transparent;
            Layer = cell.Layer;
            Name = cell.Name;
        }
    }
}
