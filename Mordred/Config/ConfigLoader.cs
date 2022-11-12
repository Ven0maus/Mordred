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
        // Items config
        private const string ItemsConfigPath = "Config\\ItemConfig\\ItemConfig.json";

        // Tiles config
        private const string WorldCellsConfigPath = "Config\\WorldGenConfig\\Json\\WorldCells.json";
        private const string ObjectCellsConfigPath = "Config\\WorldGenConfig\\Json\\ObjectCells.json";

        // Biomes config
        private const string BiomeLevelsConfigPath = "Config\\WorldGenConfig\\Json\\BiomeLevels.json";
        private const string BiomeMappingsConfigPath = "Config\\WorldGenConfig\\Csv\\BiomeMappings.csv";

        // Items
        public static readonly Dictionary<int, WorldItem> Items = LoadWorldItems();

        // Tiles
        private static readonly TerrainObject _terrainData = new();
        private static readonly Dictionary<int, WorldCellObject> _terrainCellConfig = new();
        public static readonly Dictionary<string, int> TerrainCellsByCode = new(StringComparer.OrdinalIgnoreCase);
        public static readonly Dictionary<int, WorldCell[]> TerrainCells = LoadWorldCells();
        public static readonly Dictionary<int, WorldCell> WorldCells = TerrainCells
            .SelectMany(a => a.Value)
            .ToDictionary(a => a.CellType, a => a);

        // Biomes
        private static readonly BiomeMapping[] _biomeMapping = ParseBiomeMappings().ToArray();

        public static string GetBiome(string moisture, string temperature)
        {
            foreach (var mapping in _biomeMapping)
            {
                if (mapping.moisture.name.Equals(moisture, StringComparison.OrdinalIgnoreCase) &&
                    mapping.temperature.name.Equals(temperature, StringComparison.OrdinalIgnoreCase))
                    return mapping.biome;
            }
            throw new Exception($"No biome exists for combination (Moisture: '{moisture}' Temperature: '{temperature}').");
        }

        public static string GetMoisture(double value)
        {
            foreach (var mapping in _biomeMapping)
            {
                var layer = mapping.moisture.layer == 1 ? 1.01 : mapping.moisture.layer;
                if (value < layer)
                    return mapping.moisture.name;
            }
            throw new Exception("No moisture mapping exists for value: " + value);
        }

        public static string GetTemperature(double value)
        {
            foreach (var mapping in _biomeMapping)
            {
                var layer = mapping.temperature.layer == 1 ? 1.01 : mapping.temperature.layer;
                if (value < layer)
                    return mapping.temperature.name;
            }
            throw new Exception("No moisture mapping exists for value: " + value);
        }

        private static IEnumerable<BiomeMapping> ParseBiomeMappings()
        {
            var biomeLevels = ParseBiomeLevels();
            var csv = File.ReadAllLines(BiomeMappingsConfigPath);
            // Skip header and first column
            for (int i=1; i < csv.Length; i++)
            {
                var columns = csv[i].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int cI=1; cI < columns.Length; cI++)
                {
                    var moisture = biomeLevels.moisture[i - 1];
                    var temperature = biomeLevels.temperature[cI - 1];
                    var biome = columns[cI];
                    yield return new BiomeMapping(temperature, moisture, biome);
                }
            }
        }

        private static BiomeLevels ParseBiomeLevels()
        {
            return JsonConvert.DeserializeObject<BiomeLevels>(File.ReadAllText(BiomeLevelsConfigPath));
        }

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
            var json = File.ReadAllText(ItemsConfigPath);
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
