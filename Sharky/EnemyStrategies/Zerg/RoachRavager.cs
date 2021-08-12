using Sharky.Chat;
using System.Linq;

namespace Sharky.EnemyStrategies.Zerg
{
    public class RoachRavager : EnemyStrategy
    {
        public RoachRavager(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, DebugService debugService, UnitCountService unitCountService, FrameToTimeConverter frameToTimeConverter)
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
            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.Unit.UnitType != (uint)UnitTypes.ZERG_ROACH && e.Unit.UnitType != (uint)UnitTypes.ZERG_RAVAGER && e.Unit.UnitType != (uint)UnitTypes.ZERG_RAVAGERCOCOON))
            {
                return false;
            }

            if (UnitCountService.EnemyCount(UnitTypes.ZERG_ROACH) >= 2 && UnitCountService.EnemyCount(UnitTypes.ZERG_RAVAGER) + UnitCountService.EnemyCount(UnitTypes.ZERG_RAVAGERCOCOON) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
