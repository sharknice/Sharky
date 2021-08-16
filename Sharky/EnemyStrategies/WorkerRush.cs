using Sharky.Chat;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies
{
    public class WorkerRush : EnemyStrategy
    {
        TargetingData TargetingData;

        public WorkerRush(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, TargetingData targetingData, DebugService debugService, UnitCountService unitCountService, FrameToTimeConverter frameToTimeConverter)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatService = chatService;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            TargetingData = targetingData;
            DebugService = debugService;
            UnitCountService = unitCountService;
            FrameToTimeConverter = frameToTimeConverter;
        }

        protected override bool Detect(int frame)
        {
            if (frame < SharkyOptions.FramesPerSecond * 60 * 1.5)
            {
                if (ActiveUnitData.EnemyUnits.Values.Count(u => u.UnitClassifications.Contains(UnitClassification.Worker) && Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), u.Position) > (40 * 40)) >= 5)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
