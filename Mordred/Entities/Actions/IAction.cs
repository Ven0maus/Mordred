using System;

namespace Mordred.Entities.Actions
{
    public interface IAction
    {
        bool Execute(Actor actor);
        event EventHandler<ActionCompletedArgs> ActionCompleted;
    }

    public class ActionCompletedArgs : EventArgs
    {
        public Actor Actor;
    }
}
