using System;

namespace Mordred.Entities.Actions.Implementations
{
    public abstract class BaseAction : IAction
    {
        protected bool Canceled { get; private set; }

        public abstract event EventHandler<ActionArgs> ActionCompleted;
        public virtual event EventHandler<ActionArgs> ActionCanceled;

        public virtual void Cancel()
        {
            Canceled = true;
        }

        public virtual bool Execute(Actor actor)
        {
            if (Canceled)
            {
                ActionCanceled?.Invoke(this, new ActionArgs { Actor = actor });
                return true;
            }
            return false;
        }
    }
}
