using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities;
using Mordred.Entities.Tribals;
using Mordred.GameObjects.ItemInventory;
using Mordred.Graphics.Consoles;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.WorldGen
{
    public class Village
    {
        /// <summary>
        /// The center position of the village.
        /// </summary>
        public readonly Coord Position;
        /// <summary>
        /// The radius the village spans.
        /// </summary>
        public readonly int Radius;
        /// <summary>
        /// The color the village is shown by on the world.
        /// </summary>
        public readonly Color Color;
        /// <summary>
        /// The item collection for the village.
        /// </summary>
        public readonly Inventory Inventory;

        public readonly List<Coord> HutPositions;
        public readonly List<Tribeman> Tribemen;

        public Village(Coord position, int radius, Color color)
        {
            Position = position;
            Color = color;
            Radius = radius;

            Inventory = new Inventory();
            HutPositions = new List<Coord>();
            Tribemen = new List<Tribeman>();
        }

        internal void Initialize(World world)
        {
            // Basic item resources
            Inventory.Add(0, 75); // Wood
            Inventory.Add(1, 35); // Stone
            Inventory.Add(2, 50); // Berries

            // Build the village
            Build(world, 1);

            // Spawn some villagers
            SpawnVillagers(2);
        }

        private void Build(World world, int starterHuts)
        {
            // Clear out the village zone, make it all grass
            var positions = Position
                .GetCirclePositions(Radius)
                .Where(a => MapConsole.World.InBounds(a.X, a.Y))
                .ToList();
            foreach  (var pos in positions)
            {
                world.SetCell(pos.X, pos.Y, World.WorldCells[1].TakeRandom());
            }

            // Spawn the village hut(s)
            var hutPositions = positions.TakeRandom(starterHuts);
            foreach (var hutPosition in hutPositions)
            {
                HutPositions.Add(hutPosition);
                var cell = World.WorldCells[6].TakeRandom().Clone();
                cell.Foreground = Color;
                world.SetCell(hutPosition.X, hutPosition.Y, cell);

                // Draw circle area around hut
                var circlePositions = hutPosition
                    .GetCirclePositions(3)
                    .Where(a => MapConsole.World.InBounds(a.X, a.Y))
                    .ToList();
                foreach (var pos in circlePositions)
                {
                    var worldCell = world.GetCell(pos.X, pos.Y);
                    if (worldCell == null) continue;
                    worldCell.Background = Color.Lerp(Color, Color.Black, 0.96f);
                    world.SetCell(pos.X, pos.Y, worldCell);
                }
            }
        }

        private void SpawnVillagers(int amount)
        {
            var circlePositions = Position
                .GetCirclePositions(Radius)
                .Where(a => MapConsole.World.InBounds(a.X, a.Y) && MapConsole.World.GetCell(a.X, a.Y).Walkable)
                .ToList();
            var hutPositions = new List<Coord>();
            foreach (var hut in HutPositions)
            {
                for (int i = 0; i < Constants.VillageSettings.TribemenPerHut; i++)
                    hutPositions.Add(hut);
            }

            for (int i=0; i < amount; i++)
            {
                var hutPos = hutPositions.TakeRandom();
                var pos = hutPos
                    .GetCirclePositions(3)
                    .Where(a => MapConsole.World.InBounds(a.X, a.Y) && MapConsole.World.GetCell(a.X, a.Y).Walkable)
                    .TakeRandom();
                circlePositions.Remove(pos);
                hutPositions.Remove(hutPos);

                // TODO: Make some kind of entity spawner class to handle this?
                var tribeman = new Tribeman(this, hutPos, pos, Color);
                EntitySpawner.Spawn(tribeman);
                Tribemen.Add(tribeman);
            }
        }
    }
}
