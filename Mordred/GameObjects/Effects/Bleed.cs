using SadRogue.Primitives;

namespace Mordred.GameObjects.Effects
{
    public class Bleed : CellEffect
    {
        public Bleed(Point position, int time, bool inSeconds = true) 
            : base(position, time, inSeconds)
        { }

        public override void Effect()
        {
            
        }
    }
}
