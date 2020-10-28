using Mordred.Entities.Animals;
using System;

namespace Mordred.Entities.Actions.Implementations
{
    /// <summary>
    /// Action for a predator animal to hunt for food
    /// </summary>
    public class PredatorAction : BaseAction
    {
        public override event EventHandler<Actor> ActionCompleted;

        public override bool Execute(Actor actor)
        {
            if (base.Execute(actor)) return true;

            // This action cannot be assigned to actor's that aren't of type PredatorAnimal
            if (!(actor is PredatorAnimal predator)) 
            { 
                Cancel();
                return false;
            }

            // TODO list
            // Find a body in the world that is dead but not rotten
            // No body found?: then find the nearest passive animal
            // No passive animal found?: then find the nearest predator animal
            // No predator animal found?: find nearest tribeman

            // Assign attack order on the entity
            // If the entity is dead eat the carcass

            ActionCompleted?.Invoke(this, actor);
            return true;
        }
    }
}
