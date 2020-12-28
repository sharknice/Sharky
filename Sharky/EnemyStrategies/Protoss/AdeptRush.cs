using Sharky.Chat;
using Sharky.Managers;

namespace Sharky.EnemyStrategies.Protoss
{
    public class AdeptRush : EnemyStrategy
    {
        public AdeptRush(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, DebugManager debugManager, UnitCountService unitCountService)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatService = chatService;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            DebugManager = debugManager;
            UnitCountService = unitCountService;
        }

        protected override bool Detect(int frame)
        {
            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_ADEPT) >= 4 && frame < SharkyOptions.FramesPerSecond * 5 * 60)
            {
                return true;
            }

            return false;
        }
    }
}
