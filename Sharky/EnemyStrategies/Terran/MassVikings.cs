using Sharky.Managers;

namespace Sharky.EnemyStrategies.Terran
{
    public class MassVikings : EnemyStrategy
    {
        public MassVikings(EnemyStrategyHistory enemyStrategyHistory, IChatManager chatManager, IUnitManager unitManager, SharkyOptions sharkyOptions, DebugManager debugManager)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatManager = chatManager;
            UnitManager = unitManager;
            SharkyOptions = sharkyOptions;
            DebugManager = debugManager;
        }

        protected override bool Detect(int frame)
        {
            if (UnitManager.EnemyCount(UnitTypes.TERRAN_VIKINGASSAULT) + UnitManager.EnemyCount(UnitTypes.TERRAN_VIKINGFIGHTER) >= 8)
            {
                return true;
            }

            return false;
        }
    }
}
