using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Zerg
{
    public class MassQueen : EnemyStrategy
    {
        public MassQueen(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            return UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_QUEEN) >= 8 && (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_QUEEN) * 2 > UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_HATCHERY));
        }
    }
}
