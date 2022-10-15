using Mordred.Graphics.Consoles;
using SadConsole.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities
{
    public class EntitySpawner
    {
        public readonly static List<IEntity> Entities = new List<IEntity>();

        public static T Spawn<T>(params object[] args) where T : Entity, IEntity
        {
            var entity = (T)Activator.CreateInstance(typeof(T), args);
            MapConsole.Instance.Children.Add(entity);
            Entities.Add(entity);
            MapConsole.Instance.EntityRenderer.Add(entity);
            return entity;
        }

        public static Entity Spawn(Type entity, params object[] args)
        {
            if (entity != typeof(Entity) && !entity.IsSubclassOf(typeof(Entity))) return null;
            var entityObj = (Entity)Activator.CreateInstance(entity, args);
            MapConsole.Instance.Children.Add(entityObj);
            Entities.Add((IEntity)entityObj);
            MapConsole.Instance.EntityRenderer.Add(entityObj);
            return entityObj;
        }

        public static void Spawn(IEntity entity)
        {
            MapConsole.Instance.Children.Add((Entity)entity);
            Entities.Add(entity);
            MapConsole.Instance.EntityRenderer.Add((Entity)entity);
        }

        public static void Destroy(IEntity entity)
        {
            Entities.Remove(entity);
            MapConsole.Instance.EntityRenderer.Remove((Entity)entity);
            MapConsole.Instance.Children.Remove((Entity)entity);
        }

        public static void DestroyAll<T>(Predicate<T> criteria) where T : IEntity
        {
            int entitiesDestroyed = 0;
            foreach (var entity in Entities.OfType<T>().ToArray())
            {
                if (criteria.Invoke(entity))
                {
                    Destroy(entity);
                    entitiesDestroyed++;
                }
            }
        }
    }
}
