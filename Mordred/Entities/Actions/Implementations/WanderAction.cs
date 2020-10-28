using GoRogue;
using Mordred.Entities.Tribals;
using Mordred.Graphics.Consoles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities.Actions.Implementations
{
    public class WanderAction : BaseAction
    {
        private Coord? _destination;
        private CustomPath _path;

        public override event EventHandler<Actor> ActionCompleted;

        public Coord? GetWanderingPosition(Actor actor)
        {
            // Set random destination
            List<Coord> positions;
            if (actor is Tribeman tribeman)
            {
                positions = tribeman.HutPosition
                    .GetCirclePositions(5)
                    .Where(a => MapConsole.World.InBounds(a.X, a.Y))
                    .ToList();
            }
            else
            {
                positions = ((Coord)actor.Position)
                    .GetCirclePositions(5)
                    .Where(a => MapConsole.World.InBounds(a.X, a.Y))
                    .ToList();
            }

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

        public bool Wander(Actor actor)
        {
            if (actor.Position != _destination)
            {
                return !actor.MoveTowards(_path) || actor.Position == _destination.Value;
            }
            return true;
        }

        public override bool Execute(Actor actor)
        {
            // Check for canceled state
            if (base.Execute(actor)) return true;

            if (_destination == null)
            {
                var coord = GetWanderingPosition(actor);
                if (coord == null) return true;
                _destination = coord.Value;
                if (!actor.CanMoveTowards(_destination.Value.X, _destination.Value.Y, out _path))
                {
                    ActionCompleted?.Invoke(this, actor);
                    return true;
                }
            }
            var result = Wander(actor);
            if (result)
            {
                ActionCompleted?.Invoke(this, actor);
            }
            return result;
        }
    }
}
