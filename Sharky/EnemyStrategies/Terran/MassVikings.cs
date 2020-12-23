using Sharky.Managers;

namespace Sharky.EnemyStrategies.Terran
{
    public class MassVikings : EnemyStrategy
    {
        public MassVikings(EnemyStrategyHistory enemyStrategyHistory, IChatManager chatManager, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, DebugManager debugManager, UnitCountService unitCountService)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatManager = chatManager;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            DebugManager = debugManager;
            UnitCountService = unitCountService;
        }

        protected override bool Detect(int frame)
        {
            if (UnitCountService.EnemyCount(UnitTypes.TERRAN_VIKINGASSAULT) + UnitCountService.EnemyCount(UnitTypes.TERRAN_VIKINGFIGHTER) >= 8)
            {
                return true;
            }

            return false;
        }
    }
}
