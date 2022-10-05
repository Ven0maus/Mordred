﻿using GoRogue;
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

        public readonly List<Coord> HousePositions;
        public readonly List<Human> Humans;

        public Village(Coord position, int radius, Color color)
        {
            Position = position;
            Color = color;
            Radius = radius;

            Inventory = new Inventory();
            HousePositions = new List<Coord>();
            Humans = new List<Human>();
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

        private void Build(World world, int starterHouses)
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

            // Spawn the village house(s)
            var housePositions = positions.TakeRandom(starterHouses);
            foreach (var housePosition in housePositions)
            {
                HousePositions.Add(housePosition);
                var cell = World.WorldCells[6].TakeRandom();
                cell.Foreground = Color;
                world.SetCell(housePosition.X, housePosition.Y, cell);
            }
        }

        private void SpawnVillagers(int amount)
        {
            var housePositions = new List<Coord>();
            foreach (var house in HousePositions)
            {
                for (int i = 0; i < Constants.VillageSettings.HumansPerHouse; i++)
                    housePositions.Add(house);
            }
            int males = 0, females = 0;
            for (int i=0; i < amount; i++)
            {
                var housePos = housePositions.TakeRandom();
                var pos = housePos
                    .GetCirclePositions(3)
                    .Where(a => MapConsole.World.CellWalkable(a.X, a.Y) && a != housePos)
                    .TakeRandom();
                housePositions.Remove(housePos);

                // Get equal amount of genders if possible
                Gender gender = Gender.Female;
                if (males < amount / 2)
                    gender = Gender.Male;
                else if (females < amount / 2)
                    gender = Gender.Female;

                var human = new Human(this, housePos, pos, Color, gender);
                EntitySpawner.Spawn(human);
                Humans.Add(human);
            }
        }
    }
}
