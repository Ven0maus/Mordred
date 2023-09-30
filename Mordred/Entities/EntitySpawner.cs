using SadConsole.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities
{
    public class EntitySpawner
    {
        private static readonly List<IEntity> _entities = new();

        private static readonly object _entityLock = new();
        /// <summary>
        /// Returns a shallow copy of the internal list
        /// </summary>
        public static IReadOnlyList<IEntity> Entities 
        { 
            get 
            {
                lock (_entityLock)
                {
                    return _entities.ToList();
                }
            } 
        }

        public static readonly List<Entity> EntitiesToBeAdded = new();
        public static readonly List<Entity> EntitiesToBeRemoved = new();

        private static readonly object _addLock = new();
        private static void Add(Entity entity)
        {
            lock(_addLock)
            {
                _entities.Add((IEntity)entity);
                EntitiesToBeAdded.Add(entity);
            }
        }

        private static readonly object _removeLock = new();
        private static void Remove(Entity entity)
        {
            lock (_removeLock)
            {
                _entities.Remove((IEntity)entity);
                EntitiesToBeRemoved.Add(entity);
            }
        }

        public static T Spawn<T>(params object[] args) where T : Entity, IEntity
        {
            var entity = (T)Activator.CreateInstance(typeof(T), args);
            Add(entity);
            return entity;
        }

        public static Entity Spawn(Type entity, params object[] args)
        {
            if (entity != typeof(Entity) && !entity.IsSubclassOf(typeof(Entity))) return null;
            var entityObj = (Entity)Activator.CreateInstance(entity, args);
            Add(entityObj);
            return entityObj;
        }

        public static void Spawn(IEntity entity)
        {
            Add((Entity)entity);
        }

        public static void Destroy(IEntity entity)
        {
            entity.UnSubscribe();
            Remove((Entity)entity);
        }

        public static void DestroyAll<T>(Predicate<T> criteria) where T : IEntity
        {
            var currentEntities = Entities;
            var entities = currentEntities.OfType<T>();
            foreach (var entity in entities)
            {
                if (!currentEntities.Contains(entity)) continue;
                if (criteria.Invoke(entity))
                {
                    Destroy(entity);
                }
            }
        }
    }
}
