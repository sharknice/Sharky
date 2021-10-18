using Sharky.Chat;

namespace Sharky.EnemyStrategies.Protoss
{
    public class FourGate : EnemyStrategy
    {
        public FourGate(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, DebugService debugService, UnitCountService unitCountService, FrameToTimeConverter frameToTimeConverter)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatService = chatService;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            DebugService = debugService;
            UnitCountService = unitCountService;
            FrameToTimeConverter = frameToTimeConverter;
        }

        protected override bool Detect(int frame)
        {
            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.PROTOSS_GATEWAY) >= 4 && frame < SharkyOptions.FramesPerSecond * 4 * 60)
            {
                return true;
            }

            return false;
        }
    }
}
