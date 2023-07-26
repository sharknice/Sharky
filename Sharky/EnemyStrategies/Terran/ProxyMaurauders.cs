namespace Sharky.EnemyStrategies.Terran
{
    public class ProxyMaurauders : EnemyStrategy
    {
        public ProxyMaurauders(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (frame > SharkyOptions.FramesPerSecond * 2 * 60)
            {
                return false;
            }

            if (UnitCountService.EnemyCount(UnitTypes.TERRAN_BARRACKS) < 3 && ActiveUnitData.EnemyUnits.Values.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKSTECHLAB && e.Unit.IsActive))
            {
                return true;
            }

            return false;
        }
    }
}
