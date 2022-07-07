using Sharky.DefaultBot;
using System.Linq;

namespace Sharky.EnemyStrategies
{
    public class BurrowStrategy : EnemyStrategy
    {
        public BurrowStrategy(DefaultSharkyBot defaultSharkyBot) : base (defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            if (ActiveUnitData.EnemyUnits.Any(e => (e.Value.Unit.IsBurrowed && e.Value.Unit.UnitType != (uint)UnitTypes.ZERG_CREEPTUMORBURROWED) || e.Value.Unit.UnitType == (int)UnitTypes.ZERG_QUEENBURROWED || e.Value.Unit.UnitType == (int)UnitTypes.ZERG_ROACHBURROWED || e.Value.Unit.UnitType == (int)UnitTypes.ZERG_ZERGLINGBURROWED))
            {
                return true;
            }

            return false;
        }
    }
}
