using GoRogue;
using Mordred.Graphics.Consoles;
using System;
using System.Linq;

namespace Mordred.Entities.Actions.Implementations
{
    public class WanderAction : BaseAction
    {
        private Coord? _destination;

        public override event EventHandler<ActionArgs> ActionCompleted;

        public Coord? GetWanderingPosition(Tribeman tribeman)
        {
            // Set random destination
            var positions = tribeman.HutPosition
                .GetCirclePositions(5)
                .Where(a => MapConsole.World.InBounds(a.X, a.Y))
                .ToList();

            _destination = positions.TakeRandom();
            positions.Remove(_destination.Value);
            while (!MapConsole.World.GetCell(_destination.Value.X, _destination.Value.Y).Walkable)
            {
                if (positions.Count == 0) return null;
                _destination = positions.TakeRandom();
                positions.Remove(_destination.Value);
            }
            return _destination;
        }

        public bool Wander(Tribeman tribeman)
        {
            if (tribeman.Position != _destination)
            {
                return !tribeman.MoveTowards(_destination.Value.X, _destination.Value.Y) || tribeman.Position == _destination.Value;
            }
            return true;
        }

        public override bool Execute(Actor actor)
        {
            // Check for canceled state
            if (base.Execute(actor)) return true;

            if (actor is Tribeman tribeman)
            {
                if (_destination == null)
                {
                    var coord = GetWanderingPosition(tribeman);
                    if (coord == null) return true;
                    _destination = coord.Value;
                }
                var result = Wander(tribeman);
                if (result)
                {
                    ActionCompleted?.Invoke(this, new ActionArgs() { Actor = actor });
                }
                return result;
            }
            ActionCompleted?.Invoke(this, new ActionArgs() { Actor = actor });
            return true;
        }
    }
}
