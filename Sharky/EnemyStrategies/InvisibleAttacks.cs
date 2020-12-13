using Sharky.Managers;
using System.Linq;

namespace Sharky.EnemyStrategies
{
    class InvisibleAttacks : EnemyStrategy
    {
        public InvisibleAttacks(EnemyStrategyHistory enemyStrategyHistory, IChatManager chatManager, IUnitManager unitManager, SharkyOptions sharkyOptions)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatManager = chatManager;
            UnitManager = unitManager;
            SharkyOptions = sharkyOptions;
        }

        protected override bool Detect(int frame)
        {
            if (UnitManager.EnemyUnits.Any(e => e.Value.Unit.DisplayType == SC2APIProtocol.DisplayType.Hidden))
            {
                return true;
            }

            if (UnitManager.EnemyUnits.Any(e => e.Value.UnitClassifications.Contains(UnitClassification.Cloakable)))
            {
                return true;
            }

            if (UnitManager.EnemyCount(UnitTypes.PROTOSS_DARKSHRINE) > 0 || UnitManager.EnemyCount(UnitTypes.TERRAN_STARPORTTECHLAB) > 0 || UnitManager.EnemyCount(UnitTypes.ZERG_LURKERDENMP) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
