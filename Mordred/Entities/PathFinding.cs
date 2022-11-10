using GoRogue.Pathing;
using Mordred.Graphics.Consoles;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;
using System.Collections.Generic;

namespace Mordred.Entities
{
    public class PathFinding
    {
        public IEntity Entity { get; }
        public readonly FastAStar Pathfinder;
        protected readonly LambdaGridView<bool> Walkability;

        private readonly int _rangeWidth, _rangeHeight;

        public PathFinding(IEntity entity, int rangeWidth, int rangeHeight)
        {
            Entity = entity;
            _rangeWidth = rangeWidth;
            _rangeHeight = rangeHeight;

            // Initialize pathfinder required objects
            Walkability = new LambdaGridView<bool>(_rangeWidth, _rangeHeight, IsWalkable);
            Pathfinder = new FastAStar(Walkability, Distance.Manhattan);
        }

        public CustomPath ShortestPath(Point start, Point end, bool assumeEndpointsWalkable = true)
        {
            var startOffset = ConvertToArrayPosition(start.X, start.Y);
            var endOffset = ConvertToArrayPosition(end.X, end.Y);
            return Pathfinder.ShortestPath(startOffset, endOffset, assumeEndpointsWalkable)?.ToCustomPath(this);
        }

        private bool IsWalkable(Point point)
        {
            var pointOffset = ConvertFromArrayPosition(point.X, point.Y);
            return WorldWindow.World.CellWalkable(pointOffset.x, pointOffset.y);
        }

        private (int x, int y) ConvertToArrayPosition(int x, int y)
        {
            var halfCenterX = Entity.WorldPosition.X - (_rangeWidth / 2);
            var halfCenterY = Entity.WorldPosition.Y - (_rangeHeight / 2);
            var modifiedPos = (x: x - halfCenterX, y: y - halfCenterY);
            return modifiedPos;
        }

        private (int x, int y) ConvertFromArrayPosition(int x, int y)
        {
            var halfCenterX = Entity.WorldPosition.X - (_rangeWidth / 2);
            var halfCenterY = Entity.WorldPosition.Y - (_rangeHeight / 2);
            var modifiedPos = (x: x + halfCenterX, y: y + halfCenterY);
            return modifiedPos;
        }

        public sealed class CustomPath : Path
        {
            private readonly List<Point> _coords = new();
            public CustomPath(PathFinding pathfinder, Path path) : base(path)
            {
                var length = path.Length;
                for (int i = 0; i < length; i++)
                {
                    var step = GetStep(i);
                    var offSetStep = pathfinder.ConvertFromArrayPosition(step.X, step.Y);
                    _coords.Add(offSetStep);
                }
            }

            public Point? TakeStep()
            {
                if (_coords.Count == 0) return null;
                var value = _coords[0];
                _coords.RemoveAt(0);
                return value;
            }
        }
    }
}
