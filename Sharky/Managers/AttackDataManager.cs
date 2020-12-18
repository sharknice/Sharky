using SC2APIProtocol;
using Sharky.MicroTasks;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Managers
{
    public class AttackDataManager : SharkyManager
    {
        AttackData AttackData;
        IUnitManager UnitManager;
        AttackTask AttackTask;
        TargetPriorityService TargetPriorityService;
        ITargetingManager TargetingManager;
        MacroData MacroData;
        DebugManager DebugManager;

        public AttackDataManager(AttackData attackData, IUnitManager unitManager, AttackTask attackTask, TargetPriorityService targetPriorityService, ITargetingManager targetingManager, MacroData macroData, DebugManager debugManager)
        {
            AttackData = attackData;
            UnitManager = unitManager;
            AttackTask = attackTask;
            TargetPriorityService = targetPriorityService;
            TargetingManager = targetingManager;
            MacroData = macroData;
            DebugManager = debugManager;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            AttackData.CustomAttackFunction = true;
            AttackData.Attacking = false;
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            if (!AttackData.UseAttackDataManager)
            {
                return new List<SC2APIProtocol.Action>();
            }

            if (MacroData.FoodUsed > 185)
            {
                AttackData.Attacking = true;
                DebugManager.DrawText("Attacking: > 185 supply");
                return new List<SC2APIProtocol.Action>();
            }

            if (UnitManager.SelfUnits.Count(u => u.Value.UnitClassifications.Contains(UnitClassification.Worker)) == 0)
            {
                AttackData.Attacking = true;
                DebugManager.DrawText("Attacking: no workers");
                return new List<SC2APIProtocol.Action>();
            }

            if (UnitManager.SelfUnits.Count(u => u.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter)) == 0)
            {
                AttackData.Attacking = true;
                DebugManager.DrawText("Attacking: no base");
                return new List<SC2APIProtocol.Action>();
            }

            if (AttackTask.UnitCommanders.Count() < 1)
            {
                AttackData.Attacking = false;
                DebugManager.DrawText("Not Attacking: no attacking army");
                return new List<SC2APIProtocol.Action>();
            }

            var attackVector = new Vector2(TargetingManager.AttackPoint.X, TargetingManager.AttackPoint.Y);
            var enemyUnits = UnitManager.EnemyUnits.Values.Where(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) || (e.UnitClassifications.Contains(UnitClassification.DefensiveStructure) && Vector2.DistanceSquared(attackVector, new Vector2(e.Unit.Pos.X, e.Unit.Pos.Y)) < 625));

            if (enemyUnits.Count() < 1)
            {
                AttackData.Attacking = true;
                DebugManager.DrawText("Attacking: no enemy army");
                return new List<SC2APIProtocol.Action>();
            }

            var targetPriority = TargetPriorityService.CalculateTargetPriority(AttackTask.UnitCommanders.Select(c => c.UnitCalculation), enemyUnits);
            
            if (targetPriority.OverallWinnability >= 1 || targetPriority.GroundWinnability > 2 || targetPriority.AirWinnability > 2)
            {
                AttackData.Attacking = true;
                DebugManager.DrawText($"Attacking: O:{targetPriority.OverallWinnability:0.00}, G:{targetPriority.GroundWinnability:0.00}, A:{targetPriority.AirWinnability:0.00}");
            }
            else
            {
                AttackData.Attacking = false;
                DebugManager.DrawText($"Not Attacking: O:{targetPriority.OverallWinnability:0.00}, G:{targetPriority.GroundWinnability:0.00}, A:{targetPriority.AirWinnability:0.00}");
            }

            return new List<SC2APIProtocol.Action>();
        }


    }
}
