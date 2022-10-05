using Mordred.Entities.Tribals;
using System;

namespace Mordred.Entities.Actions.Implementations
{
    public class VillageItemInteractionAction : BaseAction
    {
        public override event EventHandler<Actor> ActionCompleted;
        private readonly int _itemId, _amount;
        private readonly Interaction _interaction;

        public enum Interaction
        {
            Take,
            Give
        }

        public VillageItemInteractionAction(int itemId, int amount, Interaction interaction)
        {
            _itemId = itemId;
            _amount = amount;
            _interaction = interaction;
            TribalState = Tribal.State.Hauling;
        }

        public override bool Execute(Actor actor)
        {
            if (base.Execute(actor)) return true;

            if (!(actor is Tribal tribeman) || !tribeman.Village.Inventory.HasItem(_itemId))
            {
                ActionCompleted?.Invoke(this, actor);
                return true;
            }

            tribeman = actor as Tribal;

            // Go to the hut that belongs to this tribeman
            if (!tribeman.CanMoveTowards(tribeman.HutPosition.X, tribeman.HutPosition.Y, out CustomPath path))
            {
                ActionCompleted?.Invoke(this, actor);
                return true;
            }

            // Interact with tribe hut
            if (tribeman.Position == tribeman.HutPosition || !tribeman.MoveTowards(path))
            {
                if (_interaction == Interaction.Take)
                {
                    var item = tribeman.Village.Inventory.Take(_itemId, _amount);
                    tribeman.Inventory.Add(_itemId, item.Amount);
                }
                else
                {
                    tribeman.Village.Inventory.Add(_itemId, _amount);
                    tribeman.Inventory.Take(_itemId, _amount);
                }
                ActionCompleted?.Invoke(this, actor);
                return true;
            }
            return false;
        }
    }
}
