using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.MicroControllers
{
    public interface IIndividualMicroController
    {
        List<Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);
        List<Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame);
        List<Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);
        List<Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame);
        List<Action> Scout(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool prioritizeVision = false);
        List<Action> Bait(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);
        bool NavigateToTarget(UnitCommander commander, Point2D target, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action);
        MicroPriority MicroPriority { get; set; }
    }
}
