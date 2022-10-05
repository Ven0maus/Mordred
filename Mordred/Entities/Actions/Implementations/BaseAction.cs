using Mordred.Entities.Tribals;
using System;

namespace Mordred.Entities.Actions.Implementations
{
    public abstract class BaseAction : IAction
    {
        protected bool Canceled { get; set; }

        public abstract event EventHandler<Actor> ActionCompleted;
        public virtual event EventHandler<Actor> ActionCanceled;

        public Human.State TribalState { get; protected set; }

        public virtual void Cancel()
        {
            Canceled = true;
        }

        public virtual bool Execute(Actor actor)
        {
            if (Canceled)
            {
                ActionCanceled?.Invoke(this, actor);
                return true;
            }
            return false;
        }
    }
}
