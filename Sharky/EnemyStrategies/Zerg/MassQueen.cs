using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Zerg
{
    public class MassQueen : EnemyStrategy
    {
        public MassQueen(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            var queens = UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_QUEEN);

            if (queens > 4 && queens > UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING) + UnitCountService.EnemyCount(UnitTypes.ZERG_ROACH))
            {
                return true;
            }

            return queens >= 10 && (queens * 2 > UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_HATCHERY));
        }
    }
}
