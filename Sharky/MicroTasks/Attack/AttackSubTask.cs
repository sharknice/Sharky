using SC2APIProtocol;
using Sharky.MicroControllers;
using System.Collections.Generic;

namespace Sharky.MicroTasks.Attack
{
    public abstract class AttackSubTask: MicroTask, IAttackSubTask
    {
        protected IMicroController MicroController;

        public IAttackTask ParentTask { get; set; }

        public virtual IEnumerable<SC2APIProtocol.Action> Attack(Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame)
        {
            return MicroController.Attack(UnitCommanders, attackPoint, defensePoint, armyPoint, frame);
        }

        public virtual void ClaimUnitsFromParent(IEnumerable<UnitCommander> commanders)
        {
            throw new System.NotImplementedException();
        }

        public virtual IEnumerable<SC2APIProtocol.Action> Retreat(Point2D defensePoint, Point2D armyPoint, int frame)
        {
            return MicroController.Retreat(UnitCommanders, defensePoint, armyPoint, frame);
        }

        public virtual IEnumerable<Action> Support(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame)
        {
            return MicroController.Support(UnitCommanders, mainUnits, attackPoint, defensePoint, armyPoint, frame);
        }

        public virtual IEnumerable<Action> SupportRetreat(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame)
        {
            return MicroController.Support(UnitCommanders, mainUnits, attackPoint, defensePoint, armyPoint, frame);
        }
    }
}
