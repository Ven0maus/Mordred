using Mordred.Entities.Tribals;
using System;

namespace Mordred.Entities.Actions
{
    public interface IAction
    {
        Tribal.State TribalState { get; }
        bool Execute(Actor actor);
        void Cancel();
        event EventHandler<Actor> ActionCompleted;
        event EventHandler<Actor> ActionCanceled;
    }
}
