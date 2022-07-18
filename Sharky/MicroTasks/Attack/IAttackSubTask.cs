using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.MicroTasks.Attack
{
    public interface IAttackSubTask : IMicroTask
    {
        IAttackTask ParentTask { get; set; }
        void ClaimUnitsFromParent(IEnumerable<UnitCommander> commanders);
        IEnumerable<SC2APIProtocol.Action> Attack(Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame);
        IEnumerable<SC2APIProtocol.Action> Retreat(Point2D defensePoint, Point2D armyPoint, int frame);
        IEnumerable<Action> Support(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame);
        IEnumerable<Action> SupportRetreat(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame);
    }
}
