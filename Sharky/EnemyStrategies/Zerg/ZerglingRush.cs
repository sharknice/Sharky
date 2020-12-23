using Sharky.Managers;

namespace Sharky.EnemyStrategies.Zerg
{
    public class ZerglingRush : EnemyStrategy
    {
        public ZerglingRush(EnemyStrategyHistory enemyStrategyHistory, IChatManager chatManager, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, DebugManager debugManager, UnitCountService unitCountService)
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
            if (UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING) >= 4 && frame < SharkyOptions.FramesPerSecond * 4 * 60)
            {
                return true;
            }

            if (frame < SharkyOptions.FramesPerSecond * 5 * 60 && UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING) >= 6 && UnitCountService.EnemyCount(UnitTypes.ZERG_ROACH) == 0 && UnitCountService.EnemyCount(UnitTypes.TERRAN_REFINERY) <= 1 && UnitCountService.EnemyCount(UnitTypes.ZERG_ROACHWARREN) == 0)
            {
                return true;
            }

            return false;
        }
    }
}
