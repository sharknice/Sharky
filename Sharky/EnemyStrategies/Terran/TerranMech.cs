namespace Sharky.EnemyStrategies.Terran
{
    public class TerranMech : EnemyStrategy
    {
        public TerranMech(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_FACTORY) > 1)
            {
                return true;
            }

            return false;
        }
    }
}
