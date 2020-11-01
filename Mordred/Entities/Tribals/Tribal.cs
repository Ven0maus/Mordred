using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions;
using Mordred.Entities.Actions.Implementations;
using Mordred.Entities.Animals;
using Mordred.WorldGen;
using System;

namespace Mordred.Entities.Tribals
{
    public class Tribal : Actor
    {
        public Coord HutPosition { get; private set; }

        public enum State
        {
            Idle,
            Wandering,
            Gathering,
            Farming,
            Woodcutting,
            Mining,
            Building,
            Hunting,
            Combat
        }

        public State CurrentState { get; private set; }

        public readonly Village Village;

        public readonly Gender Gender;

        public Tribal(Village village, Coord hutPosition, Coord actorPosition, Color color, Gender gender, int health = 100) : base(color, Color.Black, 'T', health)
        {
            HutPosition = hutPosition;
            Position = actorPosition;
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
                CurrentState = State.Wandering;
            }
        }

        private void ResetStateOnCompletionOrCanceled(object sender, Actor arg)
        {
            var action = (IAction)sender;
            action.ActionCanceled -= ResetStateOnCompletionOrCanceled;
            action.ActionCompleted -= ResetStateOnCompletionOrCanceled;
            CurrentState = State.Idle;
        }
    }
}
