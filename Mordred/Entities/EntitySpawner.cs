﻿using Mordred.Graphics.Consoles;
using SadConsole.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities
{
    public class EntitySpawner
    {
        private static readonly List<IEntity> _entities = new();
        public static IReadOnlyList<IEntity> Entities 
        {
            get 
            {
                lock (_entitiesLock)
                {
                    return _entities;
                }
            } 
        }

        private static readonly object _entitiesLock = new();

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
            while (MapConsole.Instance.Children.IsLocked) { }
            Add((Entity)entity);
        }

        public static void Destroy(IEntity entity)
        {
            entity.UnSubscribe();
            Remove((Entity)entity);
        }

        public static void DestroyAll<T>(Predicate<T> criteria) where T : IEntity
        {
            var entities = Entities.ToArray().OfType<T>();
            foreach (var entity in entities)
            {
                if (!Entities.Contains(entity)) continue;
                if (criteria.Invoke(entity))
                {
                    Destroy(entity);
                }
            }
        }
    }
}
