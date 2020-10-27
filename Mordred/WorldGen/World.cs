using GoRogue;
using GoRogue.MapViews;
using GoRogue.Pathing;
using Microsoft.Xna.Framework;
using Mordred.Config;
using Mordred.Graphics.Consoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Mordred.WorldGen
{
    public enum WorldLayer
    {
        TERRAIN,
        OBJECTS,
        ITEMS,
        ENTITIES
    }

    public class World
    {
        public readonly int Width, Height;
        public static readonly Dictionary<int, WorldCell[]> WorldCells = ConfigLoader.LoadWorldCells();

        public readonly FastAStar Pathfinder;

        protected readonly MapConsole MapConsole;
        protected readonly List<Village> Villages;
        /// <summary>
        /// The visual cells that are displayed.
        /// </summary>
        protected readonly WorldCell[] Cells;
        /// <summary>
        /// The actual terrain values, which objects can overlap
        /// </summary>
        protected readonly int[] Terrain;
        protected readonly ArrayMap<bool> Walkability;

        public World(int width, int height)
        {
            Width = width;
            Height = height;

            // Get map console reference
            MapConsole = Game.Container.GetConsole<MapConsole>();

            // Initialize the arrays
            Cells = new WorldCell[Width * Height];
            Terrain = new int[Width * Height];
            Villages = new List<Village>(Constants.VillageSettings.MaxVillages);
            Walkability = new ArrayMap<bool>(Width, Height);
            Pathfinder = new FastAStar(Walkability, Distance.MANHATTAN);
        }

        public void GenerateLands()
        {
            var map = new ArrayMap<bool>(Width, Height);
            GoRogue.MapGeneration.Generators.CellularAutomataAreaGenerator.Generate(map, null, 33, 7, 4);
            for (int y=0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (map[y * Width + x])
                    {
                        // Mountains
                        SetCell(x, y, WorldCells[3].TakeRandom());
                        Terrain[y * Width + x] = 3;
                    }
                    else
                    {
                        // Tree, berrybush or grass
                        int chance = Game.Random.Next(0, 100);
                        if (chance <= 1)
                            SetCell(x, y, WorldCells[7].TakeRandom());
                        else if (chance <= 7)
                            SetCell(x, y, WorldCells[2].TakeRandom());
                        else
                            SetCell(x, y, WorldCells[1].TakeRandom());
                        Terrain[y * Width + x] = 1;
                    }
                }
            }
        }

        public void GenerateVillages()
        {
            var village = new Village(new Coord(12, 6), 4, Color.Magenta);
            village.Initialize(this);
            Villages.Add(village);

            village = new Village(new Coord(Constants.GameSettings.GameWindowWidth - 12, Constants.GameSettings.GameWindowHeight - 6), 4, Color.Orange);
            village.Initialize(this);
            Villages.Add(village);
        }

        /// <summary>
        /// The underlying terrain of a cell
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public WorldCell GetTerrain(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return WorldCells[Terrain[y * Width + x]].TakeRandom().Clone();
        }

        /// <summary>
        /// The actual visual displayed cell
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public WorldCell GetCell(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return Cells[y * Width + x].Clone();
        }

        public IEnumerable<WorldCell> GetCells(Func<WorldCell, bool> criteria)
        {
            for (int y=0; y < Height; y++)
            {
                for (int x=0; x < Width; x++)
                {
                    if (criteria.Invoke(Cells[y * Width + x]))
                        yield return Cells[y * Width + x].Clone();
                }
            }
        }

        public IEnumerable<Coord> GetCellCoords(Func<WorldCell, bool> criteria)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (criteria.Invoke(Cells[y * Width + x]))
                        yield return new Coord(x, y);
                }
            }
        }


        public void SetCell(int x, int y, WorldCell cell)
        {
            if (!InBounds(x, y)) return;

            if (Cells[y * Width + x] == null)
                Cells[y * Width + x] = cell.Clone();
            else
                Cells[y * Width + x].CopyFrom(cell);

            Walkability[y * Width + x] = cell.Walkable;
        }

        public List<WorldCell> Get4Neighbors(int x, int y)
        {
            var cells = new List<WorldCell>();
            if (!InBounds(x, y)) return cells;

            if (InBounds(x + 1, y)) cells.Add(GetCell(x+1, y));
            if (InBounds(x - 1, y)) cells.Add(GetCell(x-1, y));
            if (InBounds(x, y + 1)) cells.Add(GetCell(x, y+1));
            if (InBounds(x, y - 1)) cells.Add(GetCell(x, y-1));

            return cells;
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        private void HideObstructedCells()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cells = Get4Neighbors(x, y);
                    if (cells.All(a => !a.Transparent))
                    {
                        Cells[y * Width + x].IsVisible = false;
                    }
                    else
                    {
                        Cells[y * Width + x].IsVisible = true;
                    }
                }
            }
        }

        public void Render(bool hideObstructedCells = true, bool setSurface = false)
        {
            if (hideObstructedCells)
                HideObstructedCells();

            if (setSurface)
                MapConsole.SetSurface(Cells, Width, Height);
            else
                MapConsole.IsDirty = true;
        }
    }
}
