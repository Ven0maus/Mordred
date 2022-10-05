using GoRogue;
using GoRogue.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            if (enumerable.Count == 0) return default;
            return enumerable[Game.Random.Next(0, enumerable.Count)];
        }

        public static T TakeRandom<T>(this T[] enumerable)
        {
            if (enumerable.Length == 0) return default;
            return enumerable[Game.Random.Next(0, enumerable.Length)];
        }

        public static T TakeRandom<T>(this IEnumerable<T> enumerable)
        {
            int count = enumerable.Count();
            if (count == 0) return default;
            return enumerable.ElementAt(Game.Random.Next(0, count));
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

        public static Coord GetRandomCoordinateWithinSquareRadius(this Coord center, int squareSize, bool matchXLength = true)
        {
            int halfSquareSize = squareSize / 2;
            int x;
            int y = Game.Random.Next(center.Y - halfSquareSize, center.Y + halfSquareSize);

            if (matchXLength)
                x = Game.Random.Next(center.X - squareSize, center.X + squareSize);
            else
                x = Game.Random.Next(center.X - halfSquareSize, center.X + halfSquareSize);

            return new Coord(x, y);
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

        public static IEnumerable<Coord> GetBorderCoords(this ICollection<Coord> coords,
            Func<Coord, bool> customCriteria = null)
        {
            foreach (var coord in coords)
            {
                var neighbors = coord.Get4Neighbors();
                foreach (var neighbor in neighbors)
                {
                    if (!coords.Contains(neighbor))
                    {
                        if (customCriteria != null && customCriteria.Invoke(coord))
                        {
                            yield return coord;
                            break;
                        }
                        else if (customCriteria == null)
                        {
                            yield return coord;
                            break;
                        }
                    }
                }
            }
        }

        public static IEnumerable<Coord> Get4Neighbors(this Coord coord)
        {
            for (int i = -1; i < 2; i++)
            {
                if (i == 0) continue;
                yield return new Coord(coord.X + i, coord.Y);
                yield return new Coord(coord.X, coord.Y + i);
            }
        }

        public static IEnumerable<Coord> Get8Neighbors(this Coord coord)
        {
            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    if (x == 0 && y == 0) continue; // Don't include own coord
                    yield return new Coord(coord.X + x, coord.Y + y);
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
        private readonly List<Coord> _coords = new List<Coord>();
        public CustomPath(Path path) : base(path) 
        {
            var length = path.Length;
            for (int i=0; i < length; i++)
            {
                _coords.Add(GetStep(i));
            }
        }

        public Coord TakeStep(int step)
        {
            var value = _coords[step];
            _coords.RemoveAt(step);
            return value;
        }
    }
}
