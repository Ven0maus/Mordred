using GoRogue;
using Mordred.Entities.Tribals;
using Mordred.Graphics.Consoles;
using System;

namespace Mordred.Entities.Actions.Implementations
{
    public class WanderAction : BaseAction
    {
        private Coord? _destination;
        private CustomPath _path;
        private const int _whileLoopCheck = 500;

        public override event EventHandler<Actor> ActionCompleted;

        public Coord? GetWanderingPosition(Actor actor)
        {
            // Set random destination
            Coord center;
            if (actor is Tribal tribeman)
            {
                center = tribeman.HutPosition;
            }
            else
            {
                center = actor.Position;
            }

            _destination = center.GetRandomCoordinateWithinSquareRadius(10);
            int whileLoopCheck = 0;
            while (!MapConsole.World.CellWalkable(_destination.Value.X, _destination.Value.Y) || _destination.Value == center)
            {
                if (whileLoopCheck >= _whileLoopCheck)
                {
                    _destination = null;
                    break;
                }
                whileLoopCheck++;
                _destination = center.GetRandomCoordinateWithinSquareRadius(10);
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
                if (coord == null)
                {
                    Cancel();
                    return false;
                }
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
