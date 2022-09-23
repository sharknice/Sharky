using Sharky.DefaultBot;
using Sharky.Extensions;
using System.Linq;

namespace Sharky.EnemyStrategies
{
    public class WorkerRush : EnemyStrategy
    {
        TargetingData TargetingData;
        MacroData MacroData;

        public WorkerRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            MacroData = defaultSharkyBot.MacroData;
        }

        protected override bool Detect(int frame)
        {
            if (frame < SharkyOptions.FramesPerSecond * 60 * 2)
            {
                if (ActiveUnitData.EnemyUnits.Values.Count(u => u.UnitClassifications.Contains(UnitClassification.Worker) && u.Position.DistanceSquared(TargetingData.EnemyMainBasePoint.ToVector2()) > (40 * 40)) > 5)
                {
                    if (ActiveUnitData.EnemyUnits.Values.Count(u => u.UnitClassifications.Contains(UnitClassification.Worker) && u.Position.DistanceSquared(TargetingData.SelfMainBasePoint.ToVector2()) < (40 * 40)) < 5)
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
