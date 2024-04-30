namespace Sharky.EnemyStrategies
{
    public class InvisibleAttacks : EnemyStrategy
    {
        public InvisibleAttacks(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        /// <summary>
        /// Unit is not an observer and can be invisible
        /// </summary>
        public static bool IsNonObserverCloakableUnit(UnitCalculation unitCalculation)
        {
            return unitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_OBSERVER && (unitCalculation.UnitClassifications.HasFlag(UnitClassification.Cloakable) || unitCalculation.Unit.DisplayType == DisplayType.Hidden);
        }

        protected override bool Detect(int frame)
        {
            if (ActiveUnitData.EnemyUnits.Any(e => IsNonObserverCloakableUnit(e.Value)))
            {
                return true;
            }

            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_DARKSHRINE) > 0 || UnitCountService.EnemyCount(UnitTypes.TERRAN_STARPORTTECHLAB) > 0 || UnitCountService.EnemyCount(UnitTypes.TERRAN_GHOSTACADEMY) > 0 || UnitCountService.EnemyCount(UnitTypes.ZERG_LURKERDENMP) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
