using Microsoft.Xna.Framework;
using Mordred.Config.ItemConfig;
using Mordred.Config.WorldGenConfig;
using Mordred.GameObjects;
using Mordred.WorldGen;
using Newtonsoft.Json;
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
                    new WorldCell(cell.id, foregroundColor, backgroundColor, cell.name, glyph, (int)layers[cell.layer], cell.walkable, cell.transparent)
                };
                foreach (var aGlyph in additionalGlyphs ?? new List<int>())
                {
                    var newColor = Color.Lerp(foregroundColor, Game.Random.Next(0, 2) == 1 ? Color.Black : Color.White, (float)Game.Random.Next(1, 4) / 10);
                    cells.Add(new WorldCell(cell.id, newColor, backgroundColor, cell.name, aGlyph, (int)layers[cell.layer], cell.walkable, cell.transparent));
                }
                dictionary.Add(cell.id, cells.ToArray());
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
                dictionary.Add(item.id, new WorldItem(item.id, foregroundColor, backgroundColor, item.glyph[0], item.edible, item.edibleWorth, 0, item.droppedBy));
            }
            return dictionary;
        }

        private static Color GetColorByString(string value)
        {
            var prop = typeof(Color).GetProperty(value);
            if (prop != null)
                return (Color)prop.GetValue(null, null);
            return default;
        }
    }
}
