namespace Sharky.EnemyStrategies.Terran
{
    public class MassRavens : EnemyStrategy
    {
        public MassRavens(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_RAVEN) >= 6)
            {
                return true;
            }

            return false;
        }
    }
}
