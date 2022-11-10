using Mordred.Graphics.Consoles;
using Mordred.WorldGen;
using SadRogue.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.GameObjects.Effects
{
    public class Bleed : CellEffect
    {
        private readonly Color _startColor;
        private readonly float _amountPerTick;
        private float _amount;

        private static readonly WorldLayer[] _affectedLayers = new[] { WorldLayer.TERRAIN, WorldLayer.OBJECTS };
        private readonly Dictionary<WorldLayer, BleedCellInfo> _layerCells = new();

        public Bleed(Point position, int time, bool inSeconds = true) 
            : base(position, time, inSeconds)
        {
            // Don't add another stack
            if (WorldWindow.World.GetCellEffects(position.X, position.Y)
                .Any(a => a.Equals(this)))
            {
                Completed = true;
                return;
            }

            // Retrieve cell and store its state
            foreach (var layer in _affectedLayers)
            {
                var cell = WorldWindow.World.GetCell(layer, WorldPosition.X, WorldPosition.Y);
                _layerCells.Add(layer, new BleedCellInfo(cell));
            }

            _startColor = Color.Lerp(Color.DarkRed, Color.Transparent, 0.15f);
            _amountPerTick = 1f / TicksRemaining;
        }

        public override void Effect()
        {
            _amount += _amountPerTick;
            foreach (var layer in _affectedLayers)
            {
                var cellInfo = _layerCells[layer];
                var cell = cellInfo.Cell;
                cell.Foreground = Color.Lerp(_startColor, cellInfo.OriginColorFg, _amount);
                cell.Background = Color.Lerp(_startColor, cellInfo.OriginColorBg, _amount);
                WorldWindow.World.SetCell(layer, cell);
            }
        }

        public override void EffectEnd()
        {
            foreach (var layer in _affectedLayers)
            {
                var cellInfo = _layerCells[layer];
                var cell = cellInfo.Cell;
                cell.Foreground = cellInfo.OriginColorFg;
                cell.Background = cellInfo.OriginColorBg;
                WorldWindow.World.SetCell(layer, cell);
            }
        }

        struct BleedCellInfo
        {
            public readonly Color OriginColorBg, OriginColorFg;
            public readonly WorldCell Cell;
            public BleedCellInfo(WorldCell cell)
            {
                Cell = cell;
                OriginColorBg = cell.Background;
                OriginColorFg = cell.Foreground;
            }
        }
    }
}
