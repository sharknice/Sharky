namespace Sharky.MicroTasks.Attack
{
    public interface IAttackSubTask : IMicroTask
    {
        IAttackTask ParentTask { get; set; }
        void ClaimUnitsFromParent(IEnumerable<UnitCommander> commanders);
        IEnumerable<SC2Action> Attack(Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame);
        IEnumerable<SC2Action> Retreat(Point2D defensePoint, Point2D armyPoint, int frame);
        IEnumerable<SC2Action> Support(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame);
        IEnumerable<SC2Action> SupportRetreat(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame);
        IEnumerable<SC2Action> SplitArmy(int frame, IEnumerable<UnitCalculation> closerEnemies, Point2D attackPoint, Point2D defensePoint, Point2D armyPoint);
    }
}
