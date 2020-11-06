using SC2APIProtocol;
using Sharky.Managers;
using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class AttackTask : IMicroTask
    {
        public List<UnitCommander> UnitCommanders { get; set; }

        IMicroController MicroController;
        ITargetingManager TargetingManager;
        MacroData MacroData;
        AttackData AttackData;

        public AttackTask(IMicroController microController, ITargetingManager targetingManager, MacroData macroData, AttackData attackData)
        {
            MicroController = microController;
            TargetingManager = targetingManager;
            MacroData = macroData;
            AttackData = attackData;

            UnitCommanders = new List<UnitCommander>();
        }

        public void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.ArmyUnit))
                {
                    commander.Value.Claimed = true;
                    UnitCommanders.Add(commander.Value);
                }
            }
        }

        public IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var vectors = UnitCommanders.Select(u => new Vector2(u.UnitCalculation.Unit.Pos.X, u.UnitCalculation.Unit.Pos.Y));
            if (vectors.Count() > 0)
            {
                AttackData.ArmyPoint = new Point2D { X = vectors.Average(v => v.X), Y = vectors.Average(v => v.Y) };
            }
            else
            {
                AttackData.ArmyPoint = TargetingManager.AttackPoint;
            }

            if (MacroData.FoodArmy >= 30)
            {
                AttackData.Attacking = true;
                return MicroController.Attack(UnitCommanders, TargetingManager.AttackPoint, TargetingManager.DefensePoint, frame);
            }
            else
            {
                AttackData.Attacking = false;
                return MicroController.Retreat(UnitCommanders, TargetingManager.DefensePoint, frame);
            }
        }
    }
}
