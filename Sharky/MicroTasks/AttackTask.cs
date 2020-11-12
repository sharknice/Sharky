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
                var average = new Vector2(vectors.Average(v => v.X), vectors.Average(v => v.Y));
                var trimmed = vectors.Where(v => Vector2.DistanceSquared(average, v) < 200);
                if (trimmed.Count() > 0)
                {
                    var trimmedAverage = new Point2D { X = trimmed.Average(v => v.X), Y = trimmed.Average(v => v.Y) };
                    AttackData.ArmyPoint = trimmedAverage;
                }
                else
                {
                    AttackData.ArmyPoint = new Point2D { X = average.X, Y = average.Y };
                }
            }
            else
            {
                AttackData.ArmyPoint = TargetingManager.AttackPoint;
            }

            var attackPoint = TargetingManager.GetAttackPoint(AttackData.ArmyPoint);

            if (!AttackData.CustomAttackFunction)
            {
                AttackData.Attacking = MacroData.FoodArmy >= AttackData.ArmyFoodAttack;
            }

            if (AttackData.Attacking)
            {
                return MicroController.Attack(UnitCommanders, attackPoint, TargetingManager.DefensePoint, AttackData.ArmyPoint, frame);
            }
            else
            {
                return MicroController.Retreat(UnitCommanders, TargetingManager.DefensePoint, AttackData.ArmyPoint, frame);
            }
        }
    }
}
