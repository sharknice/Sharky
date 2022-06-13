using Sharky.DefaultBot;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies.Protoss
{
    public class ProxyShieldBattery : EnemyStrategy
    {
        TargetingData TargetingData;

        public ProxyShieldBattery(DefaultSharkyBot defaultSharkyBot)
        {
            EnemyStrategyHistory = defaultSharkyBot.EnemyStrategyHistory;
            ChatService = defaultSharkyBot.ChatService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            DebugService = defaultSharkyBot.DebugService;
            UnitCountService = defaultSharkyBot.UnitCountService;
            TargetingData = defaultSharkyBot.TargetingData;
            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;
            EnemyData = defaultSharkyBot.EnemyData;

            TargetingData = defaultSharkyBot.TargetingData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (frame < SharkyOptions.FramesPerSecond * 60 * 5)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), u.Position) > (75 * 75)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
