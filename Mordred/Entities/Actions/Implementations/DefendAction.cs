using System;

namespace Mordred.Entities.Actions.Implementations
{
    public class DefendAction : BaseAction
    {
        public override event EventHandler<Actor> ActionCompleted;
        private readonly Actor _attacker;

        public DefendAction(Actor attacker)
        {
            _attacker = attacker;
        }

        public override bool Execute(Actor actor)
        {
            if (base.Execute(actor)) return true;

            // TODO:
            // - Move to attacker
            // - Attack attacker, (stun it)

            return true;
        }
    }
}
