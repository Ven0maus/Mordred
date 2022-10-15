using Mordred.Entities.Tribals;
using Mordred.GameObjects.ItemInventory;
using Mordred.GameObjects.ItemInventory.Items;
using Mordred.Graphics.Consoles;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Entities.Actions.Implementations
{
    public class EatAction : BaseAction
    {
        public override event EventHandler<Actor> ActionCompleted;

        public EatAction()
        {
            TribalState = Human.State.Eating;
        }

        public override bool Execute(Actor actor)
        {
            if (base.Execute(actor)) return true;

            var edibles = Inventory.ItemCache.Where(a => a.Value is EdibleItem).Select(a => (int?)a.Key).ToList();
            var items = actor.Inventory.Peek()
                .Where(a => edibles.Contains(a.Key))
                .OrderByDescending(a => a.Value)
                .Select(a => new { EdibleId = a.Key })
                .ToList();

            // Add action to eat edibles if the actor has some in his inventory
            var edible = items.FirstOrDefault();
            if (edible != null)
            {
                var amount = (int)Math.Ceiling((Constants.ActorSettings.DefaultMaxHunger - actor.Hunger) / (Inventory.ItemCache[edible.EdibleId] as EdibleItem).EdibleWorth);
                EatEdibles(actor, edible.EdibleId, amount);
                return true;
            }

            // Add action to collect from the house if it contains edibles
            if (actor is Human human)
            {
                items = human.Village.Inventory.Peek()
                    .Where(a => edibles.Contains(a.Key))
                    .OrderByDescending(a => a.Value)
                    .Select(a => new { EdibleId = a.Key })
                    .ToList();
                edible = items.FirstOrDefault();
                if (edible != null)
                {
                    // Add action to collect item from house
                    var amount = (int)Math.Ceiling((Constants.ActorSettings.DefaultMaxHunger - actor.Hunger) / (Inventory.ItemCache.First(a => a.Key == edible.EdibleId).Value as EdibleItem).EdibleWorth);
                    actor.AddAction(new VillageItemInteractionAction(edible.EdibleId, amount, VillageItemInteractionAction.Interaction.Take));

                    // Add another eat task after this task
                    actor.AddAction(new EatAction());
                    return true;
                }
            }

            // TODO: Add action to gather edibles if house doesn't contain edibles
            var closestEdible = GetEdibleCells(actor).OrderBy(a => a.Value.Key.SquaredDistance(actor.WorldPosition)).FirstOrDefault();
            if (closestEdible != null)
            {
                actor.AddAction(new GatheringAction(new Point[] { closestEdible.Value.Key }));
                if (actor is Human tribal)
                {
                    // Add action to collect item from house
                    var amount = (int)Math.Ceiling((Constants.ActorSettings.DefaultMaxHunger - actor.Hunger) / (Inventory.ItemCache.First(a => a.Key == closestEdible.Value.Value).Value as EdibleItem).EdibleWorth);
                    actor.AddAction(new VillageItemInteractionAction(closestEdible.Value.Value, amount, VillageItemInteractionAction.Interaction.Take));
                }
                actor.AddAction(new EatAction());
            }

            ActionCompleted?.Invoke(this, actor);
            return true;
        }

        private List<KeyValuePair<Point, int>?> GetEdibleCells(Actor actor)
        {
            var cellIds = Inventory.ItemCache.Where(a => a.Value is EdibleItem edible && edible.DroppedBy != null)
                .Select(a => new { a.Value, EdibleId = a.Value.Id })
                .ToList();
            var kvps = new List<KeyValuePair<Point, int>?>();
            
            foreach (var id in cellIds)
            {
                var coords = MapConsole.World.GetCellCoords(actor.WorldPosition.X, actor.WorldPosition.Y, a => id.Value.IsDroppedBy(a.CellType));
                kvps.AddRange(coords.Select(a => new KeyValuePair<Point, int>?(new KeyValuePair<Point, int>(a, id.EdibleId))));
            }
            return kvps;
        }

        private void EatEdibles(Actor actor, int edibleId, int amount)
        {
            var item = actor.Inventory.Take(edibleId, amount) as EdibleItem;
            actor.Eat(item, item.Amount);
        }
    }
}
