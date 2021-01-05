using Sharky.Chat;
using System.Linq;

namespace Sharky.EnemyStrategies.Zerg
{
    public class ZerglingRush : EnemyStrategy
    {
        public ZerglingRush(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, DebugService debugService, UnitCountService unitCountService)
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
            if (UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING) >= 4 && frame < SharkyOptions.FramesPerSecond * 4 * 60 && !ActiveUnitData.EnemyUnits.Any(e => e.Value.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.Value.Unit.UnitType != (uint)UnitTypes.ZERG_ZERGLING))
            {
                return true;
            }

            if (frame < SharkyOptions.FramesPerSecond * 5 * 60 && UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING) >= 6 && !ActiveUnitData.EnemyUnits.Any(e => e.Value.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.Value.Unit.UnitType != (uint)UnitTypes.ZERG_ZERGLING) && UnitCountService.EnemyCount(UnitTypes.ZERG_EXTRACTOR) <= 1 && UnitCountService.EnemyCount(UnitTypes.ZERG_ROACHWARREN) == 0)
            {
                return true;
            }

            return false;
        }
    }
}
