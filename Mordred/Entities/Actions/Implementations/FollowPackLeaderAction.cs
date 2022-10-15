using Mordred.Entities.Animals;
using Mordred.Graphics.Consoles;
using SadRogue.Primitives;
using System;

namespace Mordred.Entities.Actions.Implementations
{
    public class FollowPackLeaderAction : BaseAction
    {
        public override event EventHandler<Actor> ActionCompleted;

        private const int _whileLoopLimit = 500;
        private int _whileLoopLimiter = 0;

        private const int _recalculatePositionInAmountTicks = 4;
        private int _positionCalculationCounter = 0;

        private Point _randomCoordinate;

        public override bool Execute(Actor actor)
        {
            if (base.Execute(actor)) return true;

            if (!(actor is IPackAnimal packAnimal))
            {
                Cancel();
                return false;
            }

            var packLeader = packAnimal.Leader as Animal;

            if (packLeader.Equals(actor))
            {
                Cancel();
                return false;
            }

            if (_positionCalculationCounter >= _recalculatePositionInAmountTicks)
            {
                // Get a random coordinate within the leader's position 6 square radius
                _randomCoordinate = ((Point)packLeader.WorldPosition).GetRandomCoordinateWithinSquareRadius(5);
                while (!MapConsole.World.CellWalkable(_randomCoordinate.X, _randomCoordinate.Y))
                {
                    if (_whileLoopLimiter >= _whileLoopLimit)
                    {
                        Cancel();
                        return false;
                    }
                    _randomCoordinate = ((Point)packLeader.WorldPosition).GetRandomCoordinateWithinSquareRadius(5);
                    _whileLoopLimiter++;
                }
            }
            _positionCalculationCounter++;

            // Move towards the selected position
            if (!MoveTowardsPackLeader(actor, out bool validPath))
            {
                if (!validPath)
                {
                    ActionCompleted?.Invoke(this, actor);
                    return true;
                }
                return false;
            }

            ActionCompleted?.Invoke(this, actor);
            return true;
        }


        private bool MoveTowardsPackLeader(Actor actor, out bool validPath)
        {
            if (!actor.CanMoveTowards(_randomCoordinate.X, _randomCoordinate.Y, out CustomPath path))
            {
                validPath = false;
                return false;
            }

            if (actor.WorldPosition == _randomCoordinate || !actor.MoveTowards(path))
            {
                validPath = true;
                return true;
            }
            else if (((Point)actor.WorldPosition).SquaredDistance(_randomCoordinate) < 2)
            {
                validPath = true;
                return true;
            }
            validPath = true;
            return false;
        }
    }
}
