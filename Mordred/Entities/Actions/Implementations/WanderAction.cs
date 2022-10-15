using Mordred.Entities.Tribals;
using Mordred.Graphics.Consoles;
using SadRogue.Primitives;
using System;

namespace Mordred.Entities.Actions.Implementations
{
    public class WanderAction : BaseAction
    {
        private Point? _destination;
        private CustomPath _path;
        private const int _whileLoopCheck = 500;

        public override event EventHandler<Actor> ActionCompleted;

        public WanderAction()
        {
            TribalState = Human.State.Wandering;
        }

        public Point? GetWanderingPosition(Actor actor)
        {
            // Set random destination
            Point center;
            if (actor is Human human)
            {
                center = human.HousePosition;
            }
            else
            {
                center = actor.WorldPosition;
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
            if (actor.WorldPosition != _destination)
            {
                return !actor.MoveTowards(_path) || actor.WorldPosition == _destination.Value;
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

                // Don't go for too long paths
                if (_path.Length > 20)
                {
                    ActionCompleted?.Invoke(this, actor);
                    return false;
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
