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
            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.Unit.UnitType != (uint)UnitTypes.ZERG_ZERGLING))
            {
                return false;
            }

            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_SPAWNINGPOOL && e.Unit.BuildProgress > .5) && frame < SharkyOptions.FramesPerSecond * 1 * 60)
            {
                return true;
            }

            if (UnitCountService.EnemyCompleted(UnitTypes.ZERG_SPAWNINGPOOL) > 0 && frame < SharkyOptions.FramesPerSecond * 1.25 * 60)
            {
                return true;
            }

            if (UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING) >= 4 && frame < SharkyOptions.FramesPerSecond * 3 * 60)
            {
                return true;
            }

            if (frame < SharkyOptions.FramesPerSecond * 4 * 60 && UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING) >= 6 && UnitCountService.EnemyCount(UnitTypes.ZERG_EXTRACTOR) <= 1 && UnitCountService.EnemyCount(UnitTypes.ZERG_ROACHWARREN) == 0)
            {
                return true;
            }

            return false;
        }
    }
}
