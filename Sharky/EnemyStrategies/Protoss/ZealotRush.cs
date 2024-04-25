namespace Sharky.EnemyStrategies.Protoss
{
    public class ZealotRush : EnemyStrategy
    {
        bool Expired = false;
        public ZealotRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (Expired) { return false; }

            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && e.Unit.UnitType != (uint)UnitTypes.PROTOSS_ZEALOT))
            {
                Expired = true;
                return false;
            }

            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                return false;
            }

            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_ZEALOT) >= 3 && frame < SharkyOptions.FramesPerSecond * 3 * 60)
            {
                return true;
            }

            return false;
        }
    }
}
