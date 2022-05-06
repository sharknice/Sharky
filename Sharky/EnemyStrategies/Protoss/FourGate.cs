using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Protoss
{
    public class FourGate : EnemyStrategy
    {
        EnemyData EnemyData;
        public FourGate(DefaultSharkyBot defaultSharkyBot)
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

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.PROTOSS_GATEWAY) >= 4 && frame < SharkyOptions.FramesPerSecond * 4 * 60)
            {
                return true;
            }

            return false;
        }
    }
}
