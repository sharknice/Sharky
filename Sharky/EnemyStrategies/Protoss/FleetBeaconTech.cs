using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Protoss
{
    public class FleetBeaconTech : EnemyStrategy
    {
        EnemyData EnemyData;

        public FleetBeaconTech(DefaultSharkyBot defaultSharkyBot)
        {
            EnemyStrategyHistory = defaultSharkyBot.EnemyStrategyHistory;
            ChatService = defaultSharkyBot.ChatService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            DebugService = defaultSharkyBot.DebugService;
            UnitCountService = defaultSharkyBot.UnitCountService;

            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;

            EnemyData = defaultSharkyBot.EnemyData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_FLEETBEACON) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
