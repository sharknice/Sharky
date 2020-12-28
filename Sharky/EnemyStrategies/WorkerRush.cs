using Sharky.Managers;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies
{
    public class WorkerRush : EnemyStrategy
    {
        TargetingData TargetingData;

        public WorkerRush(EnemyStrategyHistory enemyStrategyHistory, IChatManager chatManager, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, TargetingData targetingData, DebugManager debugManager, UnitCountService unitCountService)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatManager = chatManager;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            TargetingData = targetingData;
            DebugManager = debugManager;
            UnitCountService = unitCountService;
        }

        protected override bool Detect(int frame)
        {
            if (frame < SharkyOptions.FramesPerSecond * 60)
            {
                if (ActiveUnitData.EnemyUnits.Values.Count(u => u.UnitClassifications.Contains(UnitClassification.Worker) && Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y)) > (40 * 40)) >= 5)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
