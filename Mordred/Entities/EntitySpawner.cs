using Mordred.Graphics.Consoles;
using SadConsole.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities
{
    public class EntitySpawner
    {
        public readonly static List<IEntity> Entities = new();

        private static readonly object _addLock = new();
        private static void Add(IEntity entity)
        {
            lock(_addLock)
            {
                Entities.Add(entity);
            }
        }

        private static readonly object _removeLock = new();
        private static void Remove(IEntity entity)
        {
            lock (_removeLock)
            {
                Entities.Remove(entity);
            }
        }

        public static T Spawn<T>(params object[] args) where T : Entity, IEntity
        {
            var entity = (T)Activator.CreateInstance(typeof(T), args);
            while (MapConsole.Instance.Children.IsLocked) { }
            MapConsole.Instance.Children.Add(entity);
            Add(entity);
            MapConsole.Instance.EntityRenderer.Add(entity);
            return entity;
        }

        public static Entity Spawn(Type entity, params object[] args)
        {
            if (entity != typeof(Entity) && !entity.IsSubclassOf(typeof(Entity))) return null;
            var entityObj = (Entity)Activator.CreateInstance(entity, args);
            while (MapConsole.Instance.Children.IsLocked) { }
            MapConsole.Instance.Children.Add(entityObj);
            Add((IEntity)entityObj);
            MapConsole.Instance.EntityRenderer.Add(entityObj);
            return entityObj;
        }

        public static void Spawn(IEntity entity)
        {
            while (MapConsole.Instance.Children.IsLocked) { }
            MapConsole.Instance.Children.Add((Entity)entity);
            Add(entity);
            MapConsole.Instance.EntityRenderer.Add((Entity)entity);
        }

        public static void Destroy(IEntity entity)
        {
            entity.UnSubscribe();
            Remove(entity);
            MapConsole.Instance.EntityRenderer.Remove((Entity)entity);
            while (MapConsole.Instance.Children.IsLocked) { }
            MapConsole.Instance.Children.Remove((Entity)entity);
        }

        public static void DestroyAll<T>(Predicate<T> criteria) where T : IEntity
        {
            var entities = Entities.ToArray().OfType<T>();
            foreach (var entity in entities)
            {
                if (criteria.Invoke(entity))
                {
                    Destroy(entity);
                }
            }
        }
    }
}
