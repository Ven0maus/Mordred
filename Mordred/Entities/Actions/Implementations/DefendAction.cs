using Mordred.Entities.Animals;
using Mordred.Entities.Tribals;
using SadRogue.Primitives;
using System;
using System.Diagnostics;

namespace Mordred.Entities.Actions.Implementations
{
    public class DefendAction : BaseAction
    {
        public override event EventHandler<Actor> ActionCompleted;

        private Actor _defendee;

        private readonly int _ticksBetweenDefenceRetaliation = Game.TicksPerSecond;
        private int _ticksBetweenLastRetaliation;

        /// <summary>
        /// Constructor for defence action
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="defendee">default is null (self)</param>
        public DefendAction(Actor defendee = null)
        {
            _defendee = defendee;
            TribalState = Human.State.Combat;
        }

        public override bool Execute(Actor actor)
        {
            if (base.Execute(actor)) return true;

            if (_defendee == null)
                _defendee = actor;

            // If defendee is no longer attacked, defence is completed
            if (_defendee.CurrentAttacker == null)
            {
                ActionCompleted?.Invoke(this, actor);
                return true;
            }

            // Move to the attacker to strike back
            if (!MoveTowardsAttacker(actor, out bool validPath))
            {
                if (!validPath)
                {
                    ActionCompleted?.Invoke(this, actor);
                    return true;
                }
                return false;
            }

            if (_ticksBetweenLastRetaliation < _ticksBetweenDefenceRetaliation)
            {
                _ticksBetweenLastRetaliation++;
                return false;
            }
            _ticksBetweenLastRetaliation = 0;

            // Deal damage to the attacker
            int damage = actor is PredatorAnimal pA ? pA.AttackDamage : Game.Random.Next(5, 13);
            var name = _defendee.CurrentAttacker.Name;
            _defendee.CurrentAttacker.DealDamage(damage, actor);
            Debug.WriteLine($"{actor.Name} retaliated against {name} for: {damage} damage.");

            // Chance for attacker to lose interest when
            if (_defendee.CurrentAttacker.Health <= ((_defendee.CurrentAttacker.MaxHealth / 100f) * 25) && Game.Random.Next(0, 100) < 25)
            {
                if (_defendee.CurrentAttacker.CurrentAction != null)
                    _defendee.CurrentAttacker.CurrentAction.Cancel();
                if (_defendee.CurrentAttacker is PredatorAnimal predator)
                    predator.CurrentlyAttacking = null;
                _defendee.CurrentAttacker = null;
            }

            // If attacker is dead or no longer attacking the defendee, defence is completed
            if (_defendee.CurrentAttacker == null || !_defendee.CurrentAttacker.Alive)
            {
                if (_defendee.CurrentAttacker != null && _defendee.CurrentAttacker is PredatorAnimal predator)
                    predator.CurrentlyAttacking = null;
                _defendee.CurrentAttacker = null;

                // Complete action
                ActionCompleted?.Invoke(this, actor);
                return true;
            }
            return false;
        }

        private bool MoveTowardsAttacker(Actor actor, out bool validPath)
        {
            if (!actor.CanMoveTowards(_defendee.CurrentAttacker.Position.X, _defendee.CurrentAttacker.Position.Y, out CustomPath path))
            {
                validPath = false;
                return false;
            }

            if (actor.Position == _defendee.CurrentAttacker.Position || !actor.MoveTowards(path))
            {
                validPath = true;
                return true;
            }
            else if (((Point)actor.Position).SquaredDistance(_defendee.CurrentAttacker.Position) < 2)
            {
                validPath = true;
                return true;
            }
            validPath = true;
            return false;
        }
    }
}
