using Sharky.Chat;

namespace Sharky.EnemyStrategies.Terran
{
    public class ThreeRax : EnemyStrategy
    {
        public ThreeRax(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, DebugService debugService, UnitCountService unitCountService)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatService = chatService;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            DebugService = debugService;
            UnitCountService = unitCountService;
        }

        protected override bool Detect(int frame)
        {
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
