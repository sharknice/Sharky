using Sharky.DefaultBot;
using System.Linq;

namespace Sharky.EnemyStrategies.Zerg
{
    public class MassQueen : EnemyStrategy
    {
        public MassQueen(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        bool MadeRegularArmy { get; set; }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg || MadeRegularArmy) { return false; }

            if (ActiveUnitData.EnemyUnits.Values.Count(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.Unit.UnitType != (uint)UnitTypes.ZERG_QUEEN && e.Unit.UnitType != (uint)UnitTypes.ZERG_QUEENBURROWED) > 10)
            {
                MadeRegularArmy = true;
            }

            var queens = UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_QUEEN);

            if (queens > 4 && queens > UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING) + UnitCountService.EnemyCount(UnitTypes.ZERG_ROACH))
            {
                return true;
            }

            return queens >= 10 && (queens * 2 > UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_HATCHERY));
        }
    }
}
