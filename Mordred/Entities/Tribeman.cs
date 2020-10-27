using GoRogue;
using Microsoft.Xna.Framework;
using Mordred.Entities.Actions.Implementations;
using Mordred.WorldGen;
using System;

namespace Mordred.Entities
{
    public class Tribeman : Actor
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

        public Tribeman(Village village, Coord hutPosition, Coord actorPosition, Color color, int health = 100) : base(color, Color.Black, 'T', health)
        {
            HutPosition = hutPosition;
            Position = actorPosition;
            Village = village;
            CurrentState = State.Idle;
        }

        protected override void GameTick(object sender, EventArgs args)
        {
            if (Health <= 0) return;
            if (CurrentAction == null && CurrentState == State.Idle)
            {
                var wa = new WanderAction();
                wa.ActionCompleted += (sender, args) => { CurrentState = State.Idle; };
                AddAction(wa);
                CurrentState = State.Wandering;
            }
        }
    }
}
