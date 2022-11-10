using Mordred.Config.ItemConfig;
using Mordred.Config.WorldGenConfig;
using Mordred.GameObjects.ItemInventory.Items;
using Mordred.WorldGen;
using Newtonsoft.Json;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mordred.Config
{
    public class ConfigLoader
    {
        private const string WorldCellsConfigPath = "Config\\WorldGenConfig\\WorldCells.json";
        private const string ObjectCellsConfigPath = "Config\\WorldGenConfig\\ObjectCells.json";
        private static readonly TerrainObject _terrainData = new();
        private static readonly Dictionary<int, WorldCellObject> _terrainCellConfig = new();

        public static readonly Dictionary<string, int> TerrainCellsByCode = new(StringComparer.OrdinalIgnoreCase);
        public static readonly Dictionary<int, WorldItem> Items = LoadWorldItems();
        public static readonly Dictionary<int, WorldCell[]> TerrainCells = LoadWorldCells();
        public static readonly Dictionary<int, WorldCell> WorldCells = TerrainCells
            .SelectMany(a => a.Value)
            .ToDictionary(a => a.CellType, a => a);

        public static IEnumerable<WorldCellObject> GetTerrains(Func<WorldCellObject, bool> predicate)
        {
            return _terrainData.cells.Where(predicate);
        }

        private static int _uniqueIdCount = 0;
        private static int GetUniqueId()
        {
            return _uniqueIdCount++;
        }

        /// <summary>
        /// Returns the config object of the terrain id
        /// </summary>
        /// <param name="terrainId"></param>
        /// <returns></returns>
        public static WorldCellObject GetConfigForTerrain(int terrainId)
        {
            return _terrainCellConfig.TryGetValue(terrainId, out var cell) ? cell : 
                throw new Exception("Invalid terrain id: " + terrainId);
        }

        public static int GetRandomWorldCellTypeByTerrain(int terrainId, Random customRandom = null)
        {
            return TerrainCells[terrainId].TakeRandom(customRandom).CellType;
        }

        /// <summary>
        /// Get a new world cell of the given world cell type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="clone"></param>
        /// <param name="customRandom"></param>
        /// <returns></returns>
        public static WorldCell GetNewWorldCell(int type, int x, int y, bool clone = true, Random customRandom = null)
        {
            var cell = clone ? WorldCells[type].Clone() : WorldCells[type];
            cell.X = x;
            cell.Y = y;
            return cell;
        }

        /// <summary>
        /// Get a new random world cell, based on the given terrain type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="clone"></param>
        /// <param name="customRandom"></param>
        /// <returns></returns>
        public static WorldCell GetNewTerrainCell(int type, int x, int y, bool clone = true, Random customRandom = null)
        {
            var cell = clone ? TerrainCells[type].TakeRandom(customRandom).Clone() : TerrainCells[type].TakeRandom(customRandom);
            cell.X = x;
            cell.Y = y;
            return cell;
        }

        public static WorldCell GetNewTerrainCell(string code, int x, int y, bool clone = true, Random customRandom = null)
        {
            if (!TerrainCellsByCode.TryGetValue(code, out var mainId))
                throw new Exception("No cell with code '" + code + "' exists in the configuration.");
            var cell = clone ? TerrainCells[mainId].TakeRandom(customRandom).Clone() : TerrainCells[mainId].TakeRandom(customRandom);
            cell.X = x;
            cell.Y = y;
            return cell;
        }

        private static Dictionary<int, WorldCell[]> LoadWorldCells()
        {
            var worldCells = JsonConvert.DeserializeObject<TerrainObject>(File.ReadAllText(WorldCellsConfigPath));
            var objectCells = JsonConvert.DeserializeObject<TerrainObject>(File.ReadAllText(ObjectCellsConfigPath));

            // Set layers
            foreach (var obj in worldCells.cells)
                obj.layer = WorldLayer.TERRAIN;
            foreach (var obj in objectCells.cells)
                obj.layer = WorldLayer.OBJECTS;

            var allCells = worldCells.cells.Concat(objectCells.cells);
            _terrainData.cells = allCells.ToArray();

            var dictionary = new Dictionary<int, WorldCell[]>();
            foreach (var cell in _terrainData.cells)
            {
                var foregroundColor = GetColorByString(cell.foreground);
                var backgroundColor = GetColorByString(cell.background);
                var additionalGlyphs = cell.additionalGlyphs?.Select(a => (int)a[0]).ToList();
                if (!int.TryParse(cell.glyph, out int glyph))
                {
                    glyph = cell.glyph[0];
                }
                cell.mainId = GetUniqueId();
                var cells = new List<WorldCell>
                {
                    new WorldCell(cell.mainId, GetUniqueId(), foregroundColor, backgroundColor, cell.name, glyph, cell.layer, cell.walkable, cell.seeThrough)
                };
                foreach (var aGlyph in additionalGlyphs ?? new List<int>())
                {
                    var newColor = Color.Lerp(foregroundColor, Game.Random.Next(0, 2) == 1 ? Color.Black : Color.White, (float)Game.Random.Next(1, 4) / 10);
                    cells.Add(new WorldCell(cell.mainId, GetUniqueId(), newColor, backgroundColor, cell.name, aGlyph, cell.layer, cell.walkable, cell.seeThrough, true));
                }
                if (!string.IsNullOrWhiteSpace(cell.code))
                {
                    if (TerrainCellsByCode.ContainsKey(cell.code))
                        throw new Exception("A cell with code '" + cell.code + "' already exists.");
                    TerrainCellsByCode.Add(cell.code, cell.mainId);
                }
                _terrainCellConfig.Add(cell.mainId, cell);
                dictionary.Add(cell.mainId, cells.ToArray());
            }
            return dictionary;
        }

        public static Dictionary<int, WorldItem> LoadWorldItems()
        {
            var json = File.ReadAllText("Config\\ItemConfig\\ItemConfig.json");
            var worldItemObjects = JsonConvert.DeserializeObject<ItemsObject>(json);
            var dictionary = new Dictionary<int, WorldItem>();
            foreach (var item in worldItemObjects.items)
            {
                var foregroundColor = GetColorByString(item.foreground);
                var backgroundColor = GetColorByString(item.background);
                WorldItem itemToAdd;
                if (item.edible)
                {
                    itemToAdd = new EdibleItem(GetUniqueId(), item.name, item.edibleWorth, foregroundColor, backgroundColor, item.glyph[0], 0, item.droppedBy);
                }
                else
                {
                    itemToAdd = new WorldItem(GetUniqueId(), item.name, foregroundColor, backgroundColor, item.glyph[0], 0, item.droppedBy);
                }
                dictionary.Add(itemToAdd.Id, itemToAdd);
            }
            return dictionary;
        }

        private static Color GetColorByString(string value)
        {
            var field = typeof(Color).GetField(value);
            if (field != null)
                return (Color)field.GetValue(null);
            return default;
        }
    }
}
