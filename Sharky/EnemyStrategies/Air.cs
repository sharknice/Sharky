using Sharky.DefaultBot;

namespace Sharky.EnemyStrategies
{
    public class Air : EnemyStrategy
    {
        public Air(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace == SC2APIProtocol.Race.Zerg)
            {
                return UnitCountService.Count(UnitTypes.ZERG_MUTALISK) > 0
                    || UnitCountService.Count(UnitTypes.ZERG_CORRUPTOR) > 0
                    || UnitCountService.Count(UnitTypes.ZERG_BROODLORD) > 0;
            }
            else if (EnemyData.EnemyRace == SC2APIProtocol.Race.Protoss)
            {
                return UnitCountService.Count(UnitTypes.PROTOSS_VOIDRAY) > 0
                    || UnitCountService.Count(UnitTypes.PROTOSS_CARRIER) > 0
                    || UnitCountService.Count(UnitTypes.PROTOSS_TEMPEST) > 0
                    || UnitCountService.Count(UnitTypes.PROTOSS_ORACLE) > 0;
            }
            else if (EnemyData.EnemyRace == SC2APIProtocol.Race.Terran)
            {
                return UnitCountService.Count(UnitTypes.TERRAN_BATTLECRUISER) > 0
                    || UnitCountService.Count(UnitTypes.TERRAN_VIKINGFIGHTER) > 0
                    || UnitCountService.Count(UnitTypes.TERRAN_BANSHEE) > 0;
            }

            return false;
        }
    }
}
