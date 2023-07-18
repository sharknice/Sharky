using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies.Protoss
{
    public class FourGate : EnemyStrategy
    {
        public FourGate(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (UnitCountService.Count(UnitTypes.PROTOSS_ROBOTICSFACILITY) + UnitCountService.Count(UnitTypes.PROTOSS_TWILIGHTCOUNCIL) + UnitCountService.Count(UnitTypes.PROTOSS_DARKSHRINE) > 0) { return false; }

            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_NEXUS) > 1) { return false; }

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.PROTOSS_GATEWAY) >= 4 && frame < SharkyOptions.FramesPerSecond * 4 * 60)
            {
                return true;
            }

            return false;
        }
    }
}
