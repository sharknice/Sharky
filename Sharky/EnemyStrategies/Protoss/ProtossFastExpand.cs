using Sharky.Chat;
using System.Linq;

namespace Sharky.EnemyStrategies.Protoss
{
    public class ProtossFastExpand : EnemyStrategy
    {
        TargetingData TargetingData;

        public ProtossFastExpand(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, DebugService debugService, UnitCountService unitCountService, TargetingData targetingData)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatService = chatService;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            DebugService = debugService;
            UnitCountService = unitCountService;

            TargetingData = targetingData;
        }

        protected override bool Detect(int frame)
        {
            if (frame < SharkyOptions.FramesPerSecond * 3 * 60)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && e.Unit.Pos.X != TargetingData.EnemyMainBasePoint.X && e.Unit.Pos.Y != TargetingData.EnemyMainBasePoint.Y))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
