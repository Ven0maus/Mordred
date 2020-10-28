using System;

namespace Mordred.Entities.Actions
{
    public interface IAction
    {
        bool Execute(Actor actor);
        void Cancel();
        event EventHandler<Actor> ActionCompleted;
        event EventHandler<Actor> ActionCanceled;
    }
}
