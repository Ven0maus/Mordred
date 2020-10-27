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
            Game.Container.GetConsole<MapConsole>().Children.Add(entity);
            Entities.Add(entity);
            return entity;
        }

        public static Entity Spawn(Type entity, params object[] args)
        {
            if (entity != typeof(Entity) && !entity.IsSubclassOf(typeof(Entity))) return null;
            var entityObj = (Entity)Activator.CreateInstance(entity, args);
            Game.Container.GetConsole<MapConsole>().Children.Add(entityObj);
            Entities.Add(entityObj);
            return entityObj;
        }

        public static void Spawn(Entity entity)
        {
            Game.Container.GetConsole<MapConsole>().Children.Add(entity);
            Entities.Add(entity);
        }

        public static void Destroy(Entity entity)
        {
            Entities.Remove(entity);
            Game.Container.GetConsole<MapConsole>().Children.Remove(entity);
        }
    }
}
