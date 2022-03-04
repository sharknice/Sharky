using Sharky.Chat;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies
{
    public class WorkerRush : EnemyStrategy
    {
        TargetingData TargetingData;
        MacroData MacroData;

        public WorkerRush(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, TargetingData targetingData, DebugService debugService, UnitCountService unitCountService, FrameToTimeConverter frameToTimeConverter, MacroData macroData)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatService = chatService;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            TargetingData = targetingData;
            DebugService = debugService;
            UnitCountService = unitCountService;
            FrameToTimeConverter = frameToTimeConverter;
            MacroData = macroData;
        }

        protected override bool Detect(int frame)
        {
            if (frame < SharkyOptions.FramesPerSecond * 60 * 1.5)
            {
                if (ActiveUnitData.EnemyUnits.Values.Count(u => u.UnitClassifications.Contains(UnitClassification.Worker) && Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), u.Position) > (40 * 40)) >= 5)
                {
                    if (ActiveUnitData.EnemyUnits.Values.Count(u => u.UnitClassifications.Contains(UnitClassification.Worker) && Vector2.DistanceSquared(new Vector2(TargetingData.SelfMainBasePoint.X, TargetingData.SelfMainBasePoint.Y), u.Position) < (40 * 40)) < 5)
                    {
                        if (MacroData.Proxies.Any(p => p.Value.Enabled))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }

            return false;
        }
    }
}
