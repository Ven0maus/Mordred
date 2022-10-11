using Mordred.Graphics.Consoles;
using SadConsole.Entities;
using System;
using System.Collections.Generic;

namespace Mordred.Entities
{
    public class EntitySpawner
    {
        public readonly static List<Entity> Entities = new List<Entity>();

        public static T Spawn<T>(params object[] args) where T : Entity
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
            Entities.Add(entityObj);
            MapConsole.Instance.EntityRenderer.Add(entityObj);
            return entityObj;
        }

        public static void Spawn(Entity entity)
        {
            MapConsole.Instance.Children.Add(entity);
            Entities.Add(entity);
            MapConsole.Instance.EntityRenderer.Add(entity);
        }

        public static void Destroy(Entity entity)
        {
            Entities.Remove(entity);
            MapConsole.Instance.EntityRenderer.Remove(entity);
            MapConsole.Instance.Children.Remove(entity);
        }
    }
}
