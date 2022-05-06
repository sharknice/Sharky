using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Terran
{
    public class MassVikings : EnemyStrategy
    {
        EnemyData EnemyData;

        public MassVikings(DefaultSharkyBot defaultSharkyBot)
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

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_VIKINGFIGHTER) >= 8)
            {
                return true;
            }

            return false;
        }
    }
}
