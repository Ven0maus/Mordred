using Mordred.Entities.Actions;
using Mordred.Entities.Actions.Implementations;
using Mordred.Entities.Animals;
using Mordred.WorldGen;
using SadRogue.Primitives;
using System;

namespace Mordred.Entities.Tribals
{
    public class Human : Actor
    {
        public Point HousePosition { get; private set; }

        public enum State
        {
            Nothing,
            Idle,
            Wandering,
            Gathering,
            Hauling,
            Eating,
            Combat
        }

        public State CurrentState { get; private set; }

        public readonly Village Village;

        public readonly Gender Gender;

        public Human(Village village, Point housePosition, Point actorPosition, Color color, Gender gender, int health = 100) : base(color, Color.Black, 'T', health)
        {
            HousePosition = housePosition;
            WorldPosition = actorPosition;
            IsVisible = IsOnScreen(actorPosition);
            Village = village;
            CurrentState = State.Idle;

            Gender = gender;
        }

        protected override void GameTick(object sender, EventArgs args)
        {
            // Make sure we handle our stats automatically
            base.GameTick(sender, args);

            if (Health <= 0) return;
            if (CurrentAction == null && CurrentState == State.Idle)
            {
                var wa = new WanderAction();
                wa.ActionCompleted += ResetStateOnCompletionOrCanceled;
                wa.ActionCanceled += ResetStateOnCompletionOrCanceled;
                AddAction(wa);
            }
            else if (CurrentAction != null && 
                CurrentAction.TribalState != State.Nothing &&
                CurrentState != CurrentAction.TribalState)
            {
                // Keep tribal current state updated
                CurrentState = CurrentAction.TribalState;
            }
        }

        protected override void OnAttacked(int damage, Actor attacker)
        {
            if (Health <= 0) return;
            if (!HasActionOfType<DefendAction>())
            {
                if (CurrentAction != null)
                    CurrentAction.Cancel();
                var da = new DefendAction();
                da.ActionCompleted += ResetStateOnCompletionOrCanceled;
                da.ActionCanceled += ResetStateOnCompletionOrCanceled;
                AddAction(da, true, false);

                // Let tribals know who to attack
                foreach (var tribal in Village.Humans)
                {
                    if (!tribal.HasActionOfType<DefendAction>() && tribal.Health > 0)
                    {
                        if (tribal.CurrentAction != null)
                            tribal.CurrentAction.Cancel();
                        var daTribe = new DefendAction(this);
                        daTribe.ActionCompleted += ResetStateOnCompletionOrCanceled;
                        daTribe.ActionCanceled += ResetStateOnCompletionOrCanceled;
                        tribal.AddAction(daTribe, true, false);
                    }
                }
            }
        }

        private void ResetStateOnCompletionOrCanceled(object sender, Actor arg)
        {
            var action = (IAction)sender;
            action.ActionCanceled -= ResetStateOnCompletionOrCanceled;
            action.ActionCompleted -= ResetStateOnCompletionOrCanceled;
            if (arg is Human tribal)
                tribal.CurrentState = State.Idle;
        }
    }
}
