using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Protoss
{
    public class FleetBeaconTech : EnemyStrategy
    {
        public FleetBeaconTech(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_FLEETBEACON) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
