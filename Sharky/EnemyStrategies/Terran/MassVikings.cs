namespace Sharky.EnemyStrategies.Terran
{
    public class MassVikings : EnemyStrategy
    {
        public MassVikings(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_VIKINGFIGHTER) >= 8)
            {
                return true;
            }

            return false;
        }
    }
}
