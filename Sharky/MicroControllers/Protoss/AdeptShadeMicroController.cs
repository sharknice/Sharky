using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Protoss
{
    public class AdeptShadeMicroController : IndividualMicroController
    {
        public AdeptShadeMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return false;
        }

        protected override bool AvoidReaperCharges(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.BuffDurationRemain > 5 || commander.ParentUnitCalculation == null) { return false; }

            if (commander.ParentUnitCalculation.EnemiesInRangeOf.Count() < commander.UnitCalculation.EnemiesInRangeOf.Count(e => !e.UnitClassifications.Contains(UnitClassification.Worker)))
            {
                action = commander.Order(frame, Abilities.CANCEL_ADEPTPHASESHIFT);
                return true;
            }

            return false;
        }

        protected override bool Move(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return true; }

            return NavigateToTarget(commander, target, groupCenter, bestTarget, formation, frame, out action);
        }
    }
}
