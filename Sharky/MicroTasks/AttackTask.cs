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
        public int Priority { get; private set; }

        IMicroController MicroController;
        ITargetingManager TargetingManager;
        MacroData MacroData;
        AttackData AttackData;

        public AttackTask(IMicroController microController, ITargetingManager targetingManager, MacroData macroData, AttackData attackData, int priority)
        {
            MicroController = microController;
            TargetingManager = targetingManager;
            MacroData = macroData;
            AttackData = attackData;
            Priority = priority;

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

            // if there are enemies near base

            // if armypoint is closer to enemies than it is to attackpoint
            // send enough army there to win fight with winprobability > 2
            // send the rest of the army to attack point, if not attacking send rest to defense too

            // find all the enemies near friendly structures, 
            // get enemy and it's nearbyallies, make that a group, get any other enemies not in it

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
