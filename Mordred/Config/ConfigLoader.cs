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
        public static Dictionary<int, WorldCell[]> LoadWorldCells()
        {
            var json = File.ReadAllText("Config\\WorldGenConfig\\WorldCells.json");
            var worldCellObjects = JsonConvert.DeserializeObject<TerrainObject>(json);
            var dictionary = new Dictionary<int, WorldCell[]>();
            var layers = ((WorldLayer[])Enum.GetValues(typeof(WorldLayer))).ToDictionary(a => a.ToString(), a => a, StringComparer.OrdinalIgnoreCase);
            int uniqueCellId = 0;
            foreach (var cell in worldCellObjects.cells)
            {
                var foregroundColor = GetColorByString(cell.foreground);
                var backgroundColor = GetColorByString(cell.background);
                var additionalGlyphs = cell.additionalGlyphs?.Select(a => (int)a[0]).ToList();
                if (!int.TryParse(cell.glyph, out int glyph))
                {
                    glyph = cell.glyph[0];
                }
                var cells = new List<WorldCell>
                {
                    new WorldCell(cell.id, uniqueCellId, foregroundColor, backgroundColor, cell.name, glyph, (int)layers[cell.layer], cell.walkable, cell.transparent, cell.isResource)
                };
                foreach (var aGlyph in additionalGlyphs ?? new List<int>())
                {
                    uniqueCellId++;
                    var newColor = Color.Lerp(foregroundColor, Game.Random.Next(0, 2) == 1 ? Color.Black : Color.White, (float)Game.Random.Next(1, 4) / 10);
                    cells.Add(new WorldCell(cell.id, uniqueCellId, newColor, backgroundColor, cell.name, aGlyph, (int)layers[cell.layer], cell.walkable, cell.transparent, cell.isResource));
                }
                dictionary.Add(cell.id, cells.ToArray());
                uniqueCellId++;
            }
            return dictionary;
        }

        public static Dictionary<int, WorldItem> LoadWorldItems()
        {
            var json = File.ReadAllText("Config\\ItemConfig\\ItemConfig.json");
            var worldItemObjects = JsonConvert.DeserializeObject<ItemsObject>(json);
            var dictionary = new Dictionary<int, WorldItem>();
            var layers = ((WorldLayer[])Enum.GetValues(typeof(WorldLayer))).ToDictionary(a => a.ToString(), a => a, StringComparer.OrdinalIgnoreCase);
            foreach (var item in worldItemObjects.items)
            {
                var foregroundColor = GetColorByString(item.foreground);
                var backgroundColor = GetColorByString(item.background);
                WorldItem itemToAdd;
                if (item.edible)
                {
                    itemToAdd = new EdibleItem(item.id, item.name, item.edibleWorth, foregroundColor, backgroundColor, item.glyph[0], 0, item.droppedBy);
                }
                else
                {
                    itemToAdd = new WorldItem(item.id, item.name, foregroundColor, backgroundColor, item.glyph[0], 0, item.droppedBy);
                }
                dictionary.Add(item.id, itemToAdd);
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
