using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.MicroControllers
{
    public class MicroController : IMicroController
    {
        public List<Action> Attack(List<UnitCommander> commanders, Point2D target, Point2D defensivePoint, int frame)
        {
            var actions = new List<Action>();
            foreach (var commander in commanders)
            {
                var unitCommand = commander.Order(frame, Abilities.ATTACK_ATTACK, target);
                if (unitCommand != null)
                {
                    var action = new Action
                    {
                        ActionRaw = new ActionRaw
                        {
                            UnitCommand = unitCommand
                        }
                    };
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
                var unitCommand = commander.Order(frame, Abilities.MOVE, defensivePoint);
                if (unitCommand != null)
                {
                    var action = new Action
                    {
                        ActionRaw = new ActionRaw
                        {
                            UnitCommand = unitCommand
                        }
                    };
                    actions.Add(action);
                }
            }
            return actions;
        }
    }
}
