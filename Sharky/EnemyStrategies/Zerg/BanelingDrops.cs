using Sharky.DefaultBot;
using Sharky.Extensions;
using System.Linq;

namespace Sharky.EnemyStrategies.Zerg
{
    public class BanelingDrops : EnemyStrategy
    {
        public BanelingDrops(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            foreach (var dropaLord in ActiveUnitData.EnemyUnits.Values.Where(u => u.Unit.UnitType == (int)UnitTypes.ZERG_OVERLORDTRANSPORT))
            {
                return dropaLord.NearbyAllies.Any(b => b.Unit.UnitType == (int)UnitTypes.ZERG_BANELING && dropaLord.Unit.Pos.DistanceSquared(b.Unit.Pos) <= 1);
            }

            return false;
        }
    }
}
