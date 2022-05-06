using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Terran
{
    public class ThreeRax : EnemyStrategy
    {
        EnemyData EnemyData;

        public ThreeRax(DefaultSharkyBot defaultSharkyBot)
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
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (frame > SharkyOptions.FramesPerSecond * 2 * 60 || UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_FACTORY) > 0 || UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_STARPORT) > 0)
            {
                return false;
            }

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_BARRACKS) >= 3)
            {
                return true;
            }

            return false;
        }
    }
}
