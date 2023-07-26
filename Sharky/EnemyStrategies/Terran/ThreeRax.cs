namespace Sharky.EnemyStrategies.Terran
{
    public class ThreeRax : EnemyStrategy
    {
        public ThreeRax(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (frame > SharkyOptions.FramesPerSecond * 2 * 60 || UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_FACTORY) > 0 || UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_STARPORT) > 0)
            {
                return false;
            }

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_BARRACKS) >= 3)
            {
                return true;
            }

            return false;
        }
    }
}
