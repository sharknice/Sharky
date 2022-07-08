using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies
{
    public class AirToss : EnemyStrategy
    {
        public AirToss(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            return UnitCountService.Count(UnitTypes.PROTOSS_VOIDRAY) > 2
                || UnitCountService.Count(UnitTypes.PROTOSS_CARRIER) > 0
                || UnitCountService.Count(UnitTypes.PROTOSS_ORACLE) > 2
                || UnitCountService.Count(UnitTypes.PROTOSS_STARGATE) > 1
                || UnitCountService.Count(UnitTypes.PROTOSS_TEMPEST) > 0;
        }
    }
}
