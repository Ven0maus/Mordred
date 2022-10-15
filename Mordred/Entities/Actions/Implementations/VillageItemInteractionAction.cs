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
            TribalState = Human.State.Hauling;
        }

        public override bool Execute(Actor actor)
        {
            if (base.Execute(actor)) return true;

            if (!(actor is Human human) || !human.Village.Inventory.HasItem(_itemId))
            {
                ActionCompleted?.Invoke(this, actor);
                return true;
            }

            human = actor as Human;

            // Go to the house that belongs to this human
            if (!human.CanMoveTowards(human.HousePosition.X, human.HousePosition.Y, out PathFinding.CustomPath path))
            {
                ActionCompleted?.Invoke(this, actor);
                return true;
            }

            // Interact with tribe house
            if (human.WorldPosition == human.HousePosition || !human.MoveTowards(path))
            {
                if (_interaction == Interaction.Take)
                {
                    var item = human.Village.Inventory.Take(_itemId, _amount);
                    human.Inventory.Add(_itemId, item.Amount);
                }
                else
                {
                    human.Village.Inventory.Add(_itemId, _amount);
                    human.Inventory.Take(_itemId, _amount);
                }
                ActionCompleted?.Invoke(this, actor);
                return true;
            }
            return false;
        }
    }
}
