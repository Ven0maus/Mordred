using GoRogue;
using GoRogue.Pathing;
using SharpDX.Direct2D1.Effects;
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
