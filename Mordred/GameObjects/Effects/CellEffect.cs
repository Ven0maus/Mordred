using SadRogue.Primitives;

namespace Mordred.GameObjects.Effects
{
    public abstract class CellEffect
    {
        private readonly int _initialTicks;
        public int TicksRemaining { get; private set; }
        public Point WorldPosition { get; }
        public bool Completed { get; protected set; }

        public CellEffect(Point position, int effectTime, bool inSeconds = true)
        {
            WorldPosition = position;
            TicksRemaining = inSeconds ? effectTime.ToTicks() : effectTime;
            _initialTicks = TicksRemaining;
        }

        public void Execute()
        {
            if (TicksRemaining <= 0) return;

            if (TicksRemaining == _initialTicks)
                EffectStart();

            TicksRemaining -= 1;

            Effect();

            if (TicksRemaining == 0)
                EffectEnd();
        }

        public abstract void Effect();

        public virtual void EffectEnd()
        {
            Completed = true;
        }

        public virtual void EffectStart()
        { }
    }
}
