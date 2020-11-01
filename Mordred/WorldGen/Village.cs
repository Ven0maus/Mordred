using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities;
using Mordred.Entities.Animals;
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
        public readonly List<Tribal> Tribemen;

        public Village(Coord position, int radius, Color color)
        {
            Position = position;
            Color = color;
            Radius = radius;

            Inventory = new Inventory();
            HutPositions = new List<Coord>();
            Tribemen = new List<Tribal>();
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
                var cell = World.WorldCells[6].TakeRandom();
                cell.Foreground = Color;
                world.SetCell(hutPosition.X, hutPosition.Y, cell);
            }
        }

        private void SpawnVillagers(int amount)
        {
            var hutPositions = new List<Coord>();
            foreach (var hut in HutPositions)
            {
                for (int i = 0; i < Constants.VillageSettings.TribemenPerHut; i++)
                    hutPositions.Add(hut);
            }
            int males = 0, females = 0;
            for (int i=0; i < amount; i++)
            {
                var hutPos = hutPositions.TakeRandom();
                var pos = hutPos
                    .GetCirclePositions(3)
                    .Where(a => MapConsole.World.CellWalkable(a.X, a.Y) && a != hutPos)
                    .TakeRandom();
                hutPositions.Remove(hutPos);

                // Get equal amount of genders if possible
                Gender gender = Gender.Female;
                if (males < amount / 2)
                    gender = Gender.Male;
                else if (females < amount / 2)
                    gender = Gender.Female;

                var tribeman = new Tribal(this, hutPos, pos, Color, gender);
                EntitySpawner.Spawn(tribeman);
                Tribemen.Add(tribeman);
            }
        }
    }
}
