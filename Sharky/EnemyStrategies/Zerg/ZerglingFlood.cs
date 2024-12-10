namespace Sharky.EnemyStrategies.Zerg
{
    public class ZerglingFlood : EnemyStrategy
    {
        public ZerglingFlood(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            var lingCount = UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING);
            var elapsedTime = FrameToTimeConverter.GetTime(frame);

            if (elapsedTime.TotalMinutes > 6)
            {
                return false;
            }

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_HATCHERY) > 2)
            {
                return false;
            }

            bool enemytech = UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_BANELINGNEST) > 0 || UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_LAIR) > 0 || UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_ROACHWARREN) > 0;
            if (enemytech && lingCount < 8)
            {
                return false;
            }

            return lingCount > 6;
        }
    }
}
