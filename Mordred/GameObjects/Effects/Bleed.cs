using Mordred.Graphics.Consoles;
using Mordred.WorldGen;
using SadRogue.Primitives;
using System.Linq;

namespace Mordred.GameObjects.Effects
{
    public class Bleed : CellEffect
    {
        private readonly WorldCell _cell;
        private readonly Color _originColor;
        private readonly float _amountPerTick;
        private float _amount;

        public Bleed(Point position, int time, bool inSeconds = true) 
            : base(position, time, inSeconds)
        {
            // Don't add another stack
            if (MapConsole.World.GetCellEffects(position.X, position.Y).Any(a => a is Bleed && a.Equals(this)))
            {
                Completed = true;
                return;
            }

            // Retrieve cell and store its state
            _cell = MapConsole.World.GetCell(Position.X, Position.Y);

            _amountPerTick = 1f / TicksRemaining;
        }

        public override void Effect()
        {
            _amount += _amountPerTick;
            _cell.Background = Color.Lerp(Color.Red, _originColor, _amount);
            MapConsole.World.SetCell(_cell);
            System.Diagnostics.Debug.WriteLine("Bleed effect triggered");
        }

        public override void EffectEnd()
        {
            _cell.Background = _originColor;
        }
    }
}
