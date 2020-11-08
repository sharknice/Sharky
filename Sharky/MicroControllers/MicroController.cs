using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers
{
    public class MicroController : IMicroController
    {
        Dictionary<UnitTypes, IIndividualMicroController> IndividualMicroControllers;
        IIndividualMicroController IndividualMicroController;

        public MicroController(Dictionary<UnitTypes, IIndividualMicroController> individualMicroControllers, IIndividualMicroController individualMicroController)
        {
            IndividualMicroControllers = individualMicroControllers;
            IndividualMicroController = individualMicroController;
        }

        public List<Action> Attack(List<UnitCommander> commanders, Point2D target, Point2D defensivePoint, int frame)
        {
            var actions = new List<Action>();

            var groupCenter = GetGroupCenter(commanders);
            foreach (var commander in commanders)
            {
                Action action;

                if (IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Attack(commander, target, defensivePoint, groupCenter, frame);
                }
                else
                {
                    action = IndividualMicroController.Attack(commander, target, defensivePoint, groupCenter, frame);
                }

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

            var groupCenter = GetGroupCenter(commanders);
            foreach (var commander in commanders)
            {
                Action action;

                if (IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                }
                else
                {
                    action = IndividualMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                }

                if (action != null)
                {
                    actions.Add(action);
                }
            }
            return actions;
        }

        public List<Action> Idle(List<UnitCommander> commanders, Point2D target, Point2D defensivePoint, int frame)
        {
            return new List<Action>();
        }

        protected virtual Point2D GetGroupCenter(List<UnitCommander> commanders)
        {
            var vectors = commanders.Select(u => new Vector2(u.UnitCalculation.Unit.Pos.X, u.UnitCalculation.Unit.Pos.Y));
            if (vectors.Count() > 0)
            {
                return new Point2D { X = vectors.Average(v => v.X), Y = vectors.Average(v => v.Y) };
            }
            return null;
        }
    }
}
