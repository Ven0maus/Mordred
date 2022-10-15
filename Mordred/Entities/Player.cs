using Mordred.Graphics.Consoles;
using SadConsole.Entities;
using SadConsole.Input;
using SadRogue.Primitives;
using System.Collections.Generic;

namespace Mordred.Entities
{
    public class Player : Entity, IWorldEntity
    {
        public Point WorldPosition { get; set; }

        private readonly bool _isStaticGrid;

        public Player(Point position, bool isStaticGrid) : base(Color.Magenta, Color.Transparent, '@', 1)
        {
            Position = position;
            WorldPosition = position;
            _isStaticGrid = isStaticGrid;
        }

        public void MoveTowards(Direction dir, bool checkCanMove = true)
        {
            var point = WorldPosition;
            point += dir;
            MoveTowards(point.X, point.Y, checkCanMove);
        }

        public void MoveTowards(int x, int y, bool checkCanMove = true)
        {
            var cell = MapConsole.World.GetCell(x, y);
            if (cell == null || (checkCanMove && !cell.Walkable)) return;
            WorldPosition = new Point(x, y);

            // If we are on a static grid we don't need to center, but move the actual player coord on screen
            if (_isStaticGrid)
                Position = WorldPosition;
            else
                MapConsole.World.Center(WorldPosition.X, WorldPosition.Y);
        }

        public override bool ProcessKeyboard(Keyboard keyboard)
        {
            foreach (var key in _playerMovements.Keys)
            {
                if (keyboard.IsKeyPressed(key))
                {
                    var moveDirection = _playerMovements[key];
                    MoveTowards(moveDirection, false);
                    return true;
                }
            }

            return base.ProcessKeyboard(keyboard);
        }

        private readonly Dictionary<Keys, Direction> _playerMovements =
            new()
            {
            {Keys.Z, Direction.Up},
            {Keys.S, Direction.Down},
            {Keys.Q, Direction.Left},
            {Keys.D, Direction.Right}
        };
    }
}
