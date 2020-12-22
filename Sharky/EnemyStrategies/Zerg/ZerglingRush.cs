using Sharky.Managers;

namespace Sharky.EnemyStrategies.Zerg
{
    public class ZerglingRush : EnemyStrategy
    {
        public ZerglingRush(EnemyStrategyHistory enemyStrategyHistory, IChatManager chatManager, IUnitManager unitManager, SharkyOptions sharkyOptions, DebugManager debugManager)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatManager = chatManager;
            UnitManager = unitManager;
            SharkyOptions = sharkyOptions;
            DebugManager = debugManager;
        }

        protected override bool Detect(int frame)
        {
            if (UnitManager.EnemyCount(UnitTypes.ZERG_ZERGLING) >= 4 && frame < SharkyOptions.FramesPerSecond * 4 * 60)
            {
                return true;
            }

            if (frame < SharkyOptions.FramesPerSecond * 5 * 60 && UnitManager.EnemyCount(UnitTypes.ZERG_ZERGLING) >= 6 && UnitManager.EnemyCount(UnitTypes.ZERG_ROACH) == 0 && UnitManager.EnemyCount(UnitTypes.TERRAN_REFINERY) <= 1 && UnitManager.EnemyCount(UnitTypes.ZERG_ROACHWARREN) == 0)
            {
                return true;
            }

            return false;
        }
    }
}
