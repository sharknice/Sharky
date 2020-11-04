using SC2APIProtocol;
using System.Collections.Generic;
using System.Numerics;

namespace Sharky.MicroControllers
{
    public class MicroController : IMicroController
    {
        public List<Action> Attack(List<UnitCommander> commanders, Point2D target, Point2D defensivePoint, int frame)
        {
            var actions = new List<Action>();
            foreach (var commander in commanders)
            {
                var action = commander.Order(frame, Abilities.ATTACK_ATTACK, target);
                if (action != null)
                {
                    actions.Add(action);
                }
            }
            return actions;
        }

        public List<Action> Retreat(List<UnitCommander> commanders, Point2D defensivePoint, int frame)
        {
            var actions = new List<Action>();
            foreach (var commander in commanders)
            {
                if (Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(defensivePoint.X, defensivePoint.Y)) > 100)
                {
                    var action = commander.Order(frame, Abilities.MOVE, defensivePoint);
                    if (action != null)
                    {
                        actions.Add(action);
                    }
                }
            }
            return actions;
        }

        public List<Action> Idle(List<UnitCommander> commanders, Point2D target, Point2D defensivePoint, int frame)
        {
            return new List<Action>();
        }
    }
}
