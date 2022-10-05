using Mordred.Entities.Tribals;
using System;

namespace Mordred.Entities.Actions.Implementations
{
    public class StunAction : BaseAction
    {
        public override event EventHandler<Actor> ActionCompleted;

        private readonly int _stunTime;
        private int _timeElapsed = 0;

        /// <summary>
        /// Constructor for stun action
        /// </summary>
        /// <param name="stunTime"></param>
        /// <param name="inSeconds">If false, it will be in ticks instead</param>
        public StunAction(int stunTime, bool inSeconds = true)
        {
            if (inSeconds)
            {
                float ticksPerSecond = 1f / Constants.GameSettings.TimePerTickInSeconds;
                _stunTime = (int)Math.Round(ticksPerSecond * stunTime);
            }
            else
            {
                _stunTime = stunTime;
            }
            TribalState = Human.State.Combat;
        }

        public override void Cancel()
        {
            // Stun action is not cancelable
        }

        public override bool Execute(Actor actor)
        {
            if (base.Execute(actor)) return true;

            if (_timeElapsed < _stunTime)
            {
                _timeElapsed++;
                return false;
            }
            ActionCompleted?.Invoke(this, actor);
            return true;
        }
    }
}
