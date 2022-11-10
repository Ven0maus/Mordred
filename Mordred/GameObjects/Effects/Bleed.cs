using Mordred.Graphics.Consoles;
using Mordred.WorldGen;
using SadRogue.Primitives;
using System.Linq;

namespace Mordred.GameObjects.Effects
{
    public class Bleed : CellEffect
    {
        private readonly WorldCell _cell;
        private readonly Color _startColor, _originColorBg, _originColorFg;
        private readonly float _amountPerTick;
        private float _amount;

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
            _cell = WorldWindow.World.GetCell(WorldLayer.TERRAIN, WorldPosition.X, WorldPosition.Y);
            _originColorFg = _cell.Foreground;
            _originColorBg = _cell.Background;
            _startColor = Color.Lerp(Color.DarkRed, Color.Transparent, 0.15f);

            _amountPerTick = 1f / TicksRemaining;
        }

        public override void Effect()
        {
            _amount += _amountPerTick;
            _cell.Foreground = Color.Lerp(_startColor, _originColorFg, _amount);
            _cell.Background = Color.Lerp(_startColor, _originColorBg, _amount);
            WorldWindow.World.SetCell(WorldLayer.TERRAIN, _cell);
        }

        public override void EffectEnd()
        {
            _cell.Foreground = _originColorFg;
            _cell.Background = _originColorBg;
            WorldWindow.World.SetCell(WorldLayer.TERRAIN, _cell);
        }
    }
}
