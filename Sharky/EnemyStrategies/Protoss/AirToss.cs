using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies
{
    public class AirToss : EnemyStrategy
    {
        public AirToss(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            return UnitCountService.EnemyCount(UnitTypes.PROTOSS_ORACLE) > 2
                || UnitCountService.EnemyCount(UnitTypes.PROTOSS_VOIDRAY) > 2
                || UnitCountService.EnemyCount(UnitTypes.PROTOSS_CARRIER) > 0
                || UnitCountService.EnemyCount(UnitTypes.PROTOSS_TEMPEST) > 0
                || UnitCountService.EnemyCount(UnitTypes.PROTOSS_STARGATE) > 1
                || UnitCountService.EnemyCount(UnitTypes.PROTOSS_FLEETBEACON) > 0;
        }
    }
}
