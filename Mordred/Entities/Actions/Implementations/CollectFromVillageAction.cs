using Mordred.Entities.Tribals;
using System;

namespace Mordred.Entities.Actions.Implementations
{
    public class CollectFromVillageAction : BaseAction
    {
        public override event EventHandler<Actor> ActionCompleted;
        private readonly int _itemId, _amount;

        public CollectFromVillageAction(int itemId, int amount)
        {
            _itemId = itemId;
            _amount = amount;
        }

        public override bool Execute(Actor actor)
        {
            if (base.Execute(actor)) return true;

            if (!(actor is Tribeman tribeman) || !tribeman.Village.Inventory.HasItem(_itemId))
            {
                ActionCompleted?.Invoke(this, actor);
                return true;
            }

            tribeman = actor as Tribeman;

            // Go to the hut that belongs to this tribeman
            if (!tribeman.CanMoveTowards(tribeman.HutPosition.X, tribeman.HutPosition.Y, out CustomPath path))
            {
                ActionCompleted?.Invoke(this, actor);
                return true;
            }

            if (tribeman.Position == tribeman.HutPosition || !tribeman.MoveTowards(path))
            {
                tribeman.Inventory.Add(_itemId, tribeman.Village.Inventory.Take(_itemId, _amount).Amount);
                return true;
            }
            return false;
        }
    }
}
