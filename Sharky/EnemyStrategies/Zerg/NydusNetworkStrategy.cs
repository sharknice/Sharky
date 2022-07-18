using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Zerg
{
    public class NydusNetworkStrategy : EnemyStrategy
    {
        public NydusNetworkStrategy(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            return UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_NYDUSCANAL) > 0;
        }
    }
}
