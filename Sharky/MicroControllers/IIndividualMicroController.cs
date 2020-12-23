using SC2APIProtocol;

namespace Sharky.MicroControllers
{
    public interface IIndividualMicroController
    {
        Action Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);
        Action Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame);
        Action Idle(UnitCommander commander, Point2D defensivePoint, int frame);
        Action Scout(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool prioritizeVision = false);
        Action Bait(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);
        bool NavigateToTarget(UnitCommander commander, Point2D target, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out SC2APIProtocol.Action action);
        MicroPriority MicroPriority { get; set; }
    }
}
