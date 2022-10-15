using GoRogue.Pathing;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mordred
{
    public static class Extensions
    {
        /// <summary>
        /// Converts seconds to game ticks
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static int ToTicks(this int seconds)
        {
            float ticksPerSecond = 1f / Constants.GameSettings.TimePerTickInSeconds;
            return (int)Math.Round(ticksPerSecond * seconds);
        }

        public static float SquaredDistance(this Point pos, Point target)
        {
            return (target.X - pos.X) * (target.X - pos.X) + (target.Y - pos.Y) * (target.Y - pos.Y);
        }

        public static float Distance(this Point pos, Point target)
        {
            return MathF.Sqrt((target.X - pos.X) * (target.X - pos.X) + (target.Y - pos.Y) * (target.Y - pos.Y));
        }

        public static T TakeRandom<T>(this IList<T> enumerable)
        {
            if (enumerable.Count == 0) return default;
            return enumerable[Game.Random.Next(0, enumerable.Count)];
        }

        public static T TakeRandom<T>(this T[] enumerable, Random customRandom = null)
        {
            if (enumerable.Length == 0) return default;
            var rand = customRandom ?? Game.Random;
            return enumerable[rand.Next(0, enumerable.Length)];
        }

        public static T TakeRandom<T>(this IEnumerable<T> enumerable, Random customRandom = null)
        {
            int count = enumerable.Count();
            if (count == 0) return default;
            var rand = customRandom ?? Game.Random;
            return enumerable.ElementAt(rand.Next(0, count));
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

        public static Point GetRandomCoordinateWithinSquareRadius(this Point center, int squareSize, bool matchXLength = true)
        {
            int halfSquareSize = squareSize / 2;
            int x;
            int y = Game.Random.Next(center.Y - halfSquareSize, center.Y + halfSquareSize);

            if (matchXLength)
                x = Game.Random.Next(center.X - squareSize, center.X + squareSize);
            else
                x = Game.Random.Next(center.X - halfSquareSize, center.X + halfSquareSize);

            return new Point(x, y);
        } 

        public static IEnumerable<Point> GetCirclePositions(this Point center, int radius)
        {
            var coords = new List<Point>(radius * radius);
            for (int y=center.Y - radius; y <= center.Y + radius; y++)
            {
                for (int x = center.X - radius; x <= center.X + radius; x++)
                {
                    coords.Add(new Point(x, y));
                }
            }

            foreach (var coord in coords)
            {
                if (((coord.X - center.X) * (coord.X - center.X)) + ((coord.Y - center.Y) * (coord.Y - center.Y)) <= radius * radius)
                    yield return coord;
            }
        }

        public static IEnumerable<Point> GetBorderCoords(this ICollection<Point> coords,
            Func<Point, bool> customCriteria = null)
        {
            var result = coords
                .Select(a => (Point: a, Neighbors: a.Get4Neighbors()))
                .Where(a => a.Neighbors.Any(neighbor => !coords.Contains(neighbor)))
                .Select(a => a.Point);
            if (customCriteria != null)
                result = result.Where(a => customCriteria.Invoke(a));
            return result;
        }

        public static IEnumerable<Point> Get4Neighbors(this Point coord)
        {
            for (int i = -1; i < 2; i++)
            {
                if (i == 0) continue;
                yield return new Point(coord.X + i, coord.Y);
                yield return new Point(coord.X, coord.Y + i);
            }
        }

        public static IEnumerable<Point> Get8Neighbors(this Point coord)
        {
            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    if (x == 0 && y == 0) continue; // Don't include own coord
                    yield return new Point(coord.X + x, coord.Y + y);
                }
            }
        }

        public static CustomPath ToCustomPath(this Path path)
        {
            return new CustomPath(path);
        }
    }

    public static class ReflectiveEnumerator
    {
        public static IEnumerable<Type> GetEnumerableOfType<T>() where T : class
        {
            List<Type> objects = new List<Type>();
            foreach (Type type in
                Assembly.GetAssembly(typeof(T)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))))
            {
                objects.Add(type);
            }
            return objects;
        }
    }

    public sealed class CustomPath : Path
    {
        private readonly List<Point> _coords = new List<Point>();
        public CustomPath(Path path) : base(path) 
        {
            var length = path.Length;
            for (int i=0; i < length; i++)
            {
                _coords.Add(GetStep(i));
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
