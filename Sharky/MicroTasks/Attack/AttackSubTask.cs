using SC2APIProtocol;
using Sharky.MicroControllers;
using System;
using System.Collections.Generic;

namespace Sharky.MicroTasks.Attack
{
    public abstract class AttackSubTask: MicroTask, IAttackSubTask
    {
        protected IMicroController MicroController;
        protected MicroTaskData MicroTaskData;
        protected TargetingData TargetingData;
        protected ArmySplitter ArmySplitter;

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

        public virtual IEnumerable<SC2APIProtocol.Action> SplitArmy(int frame, IEnumerable<UnitCalculation> closerEnemies, Point2D attackPoint)
        {
            return ArmySplitter.SplitArmy(frame, closerEnemies, TargetingData.AttackPoint, UnitCommanders, false);
        }

        public virtual IEnumerable<SC2APIProtocol.Action> Support(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame)
        {
            return MicroController.Support(UnitCommanders, mainUnits, attackPoint, defensePoint, armyPoint, frame);
        }

        public virtual IEnumerable<SC2APIProtocol.Action> SupportRetreat(IEnumerable<UnitCommander> mainUnits, Point2D attackPoint, Point2D defensePoint, Point2D armyPoint, int frame)
        {
            return MicroController.Support(UnitCommanders, mainUnits, attackPoint, defensePoint, armyPoint, frame);
        }



        protected void ResetAttackTaskClaims()
        {
            if (MicroTaskData.ContainsKey(typeof(AttackTask).Name))
            {
                MicroTaskData[typeof(AttackTask).Name].ResetClaimedUnits();
            }
        }
    }
}
