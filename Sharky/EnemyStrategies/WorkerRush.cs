using Sharky.DefaultBot;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies
{
    public class WorkerRush : EnemyStrategy
    {
        TargetingData TargetingData;
        MacroData MacroData;

        public WorkerRush(DefaultSharkyBot defaultSharkyBot)
        {
            EnemyStrategyHistory = defaultSharkyBot.EnemyStrategyHistory;
            ChatService = defaultSharkyBot.ChatService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            DebugService = defaultSharkyBot.DebugService;
            UnitCountService = defaultSharkyBot.UnitCountService;
            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;
            EnemyData = defaultSharkyBot.EnemyData;

            TargetingData = defaultSharkyBot.TargetingData;
            MacroData = defaultSharkyBot.MacroData;
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
