using GoRogue;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mordred
{
    public static class Extensions
    {
        public static float SquaredDistance(this Coord pos, Coord target)
        {
            return (target.X - pos.X) * (target.X - pos.X) + (target.Y - pos.Y) * (target.Y - pos.Y);
        }

        public static float Distance(this Coord pos, Coord target)
        {
            return MathF.Sqrt((target.X - pos.X) * (target.X - pos.X) + (target.Y - pos.Y) * (target.Y - pos.Y));
        }

        public static T TakeRandom<T>(this IList<T> enumerable)
        {
            return enumerable[Game.Random.Next(0, enumerable.Count)];
        }

        public static T TakeRandom<T>(this T[] enumerable)
        {
            return enumerable[Game.Random.Next(0, enumerable.Length)];
        }

        public static T TakeRandom<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.ElementAt(Game.Random.Next(0, enumerable.Count()));
        }

        public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> enumerable, int amount)
        {
            var total = amount;
            var newCollection = new List<T>(enumerable);
            while (newCollection.Count > 0 && total > 0)
            {
                var value = newCollection[Game.Random.Next(0, newCollection.Count)];
                newCollection.Remove(value);
                total--;
                yield return value;
            }
        }

        public static IEnumerable<Coord> GetCirclePositions(this Coord center, int radius)
        {
            var coords = new List<Coord>(radius * radius);
            for (int y=center.Y - radius; y <= center.Y + radius; y++)
            {
                for (int x = center.X - radius; x <= center.X + radius; x++)
                {
                    coords.Add(new Coord(x, y));
                }
            }

            foreach (var coord in coords)
            {
                if (((coord.X - center.X) * (coord.X - center.X)) + ((coord.Y - center.Y) * (coord.Y - center.Y)) <= radius * radius)
                    yield return coord;
            }
        }
    }
}
