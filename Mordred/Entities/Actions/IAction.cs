using System;

namespace Mordred.Entities.Actions
{
    public interface IAction
    {
        bool Execute(Actor actor);
        void Cancel();
        event EventHandler<ActionArgs> ActionCompleted;
        event EventHandler<ActionArgs> ActionCanceled;
    }

    public class ActionArgs : EventArgs
    {
        public Actor Actor;
    }
}
