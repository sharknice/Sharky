using Sharky.Managers;

namespace Sharky.EnemyStrategies.Terran
{
    public class MarineRush : EnemyStrategy
    {
        public MarineRush(EnemyStrategyHistory enemyStrategyHistory, IChatManager chatManager, IUnitManager unitManager, SharkyOptions sharkyOptions, DebugManager debugManager)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatManager = chatManager;
            UnitManager = unitManager;
            SharkyOptions = sharkyOptions;
            DebugManager = debugManager;
        }

        protected override bool Detect(int frame)
        {
            if (UnitManager.EnemyCount(UnitTypes.TERRAN_MARINE) >= 8 && frame < SharkyOptions.FramesPerSecond * 4 * 60)
            {
                return true;
            }
            if (UnitManager.EnemyCount(UnitTypes.TERRAN_BARRACKS) >= 3 && UnitManager.EnemyCount(UnitTypes.TERRAN_REFINERY) == 0 && frame < SharkyOptions.FramesPerSecond * 4 * 60)
            {
                return true;
            }
            if (frame < SharkyOptions.FramesPerSecond * 5 * 60 && UnitManager.EnemyCount(UnitTypes.TERRAN_MARINE) >= 5 && UnitManager.EnemyCount(UnitTypes.TERRAN_BARRACKS) >= 2 && UnitManager.EnemyCount(UnitTypes.TERRAN_MARAUDER) == 0 && UnitManager.EnemyCount(UnitTypes.TERRAN_REAPER) == 0 && UnitManager.EnemyCount(UnitTypes.TERRAN_BARRACKSREACTOR) == 0 && UnitManager.EnemyCount(UnitTypes.TERRAN_BARRACKSTECHLAB) == 0 && UnitManager.EnemyCount(UnitTypes.TERRAN_REFINERY) == 0 && UnitManager.EnemyCount(UnitTypes.TERRAN_FACTORY) == 0)
            {
                return true;
            }

            return false;
        }
    }
}
