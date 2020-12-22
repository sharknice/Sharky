using Sharky.Managers;

namespace Sharky.EnemyStrategies.Protoss
{
    public class AdeptRush : EnemyStrategy
    {
        public AdeptRush(EnemyStrategyHistory enemyStrategyHistory, IChatManager chatManager, IUnitManager unitManager, SharkyOptions sharkyOptions, DebugManager debugManager)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatManager = chatManager;
            UnitManager = unitManager;
            SharkyOptions = sharkyOptions;
            DebugManager = debugManager;
        }

        protected override bool Detect(int frame)
        {
            if (UnitManager.EnemyCount(UnitTypes.PROTOSS_ADEPT) >= 4 && frame < SharkyOptions.FramesPerSecond * 5 * 60)
            {
                return true;
            }

            return false;
        }
    }
}
