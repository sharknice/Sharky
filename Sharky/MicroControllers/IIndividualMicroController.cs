namespace Sharky.MicroControllers
{
    public interface IIndividualMicroController
    {
        List<SC2Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);
        List<SC2Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame);
        List<SC2Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);
        List<SC2Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame);
        List<SC2Action> Scout(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool prioritizeVision = false, bool attack = true);
        List<SC2Action> Bait(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);
        List<SC2APIProtocol.Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame);
        List<SC2APIProtocol.Action> NavigateToPoint(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);
        List<SC2APIProtocol.Action> Contain(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);
        bool NavigateToTarget(UnitCommander commander, Point2D target, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action);
        List<SC2Action> AttackWithinArea(UnitCommander commander, List<Point2D> area, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);

        MicroPriority MicroPriority { get; set; }
    }
}
