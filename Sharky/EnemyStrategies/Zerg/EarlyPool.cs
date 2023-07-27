namespace Sharky.EnemyStrategies.Zerg
{
    public class EarlyPool : EnemyStrategy
    {
        public EarlyPool(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        private bool earlyPool = false;

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            var elapsedTime = FrameToTimeConverter.GetTime(frame);

            if (elapsedTime.TotalMinutes >= 3.5f)
            {
                return false;
            }

            if (!earlyPool)
            {
                var enemyPool = ActiveUnitData.EnemyUnits.Where(x => x.Value.Unit.UnitType == (int)UnitTypes.ZERG_SPAWNINGPOOL).Select(x => x.Value).FirstOrDefault();
                var lingCount = UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING);

                earlyPool = (lingCount > 0 && elapsedTime < TimeSpan.FromMinutes(2.08f))
                    || (enemyPool is not null && UnitCountService.BuildingStarted(enemyPool).TotalMinutes < 1.0f);
            }

            return earlyPool;
        }
    }
}
