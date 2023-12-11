namespace Sharky.EnemyStrategies.Protoss
{
    public class SuspectedFourGate : EnemyStrategy
    {
        BaseData BaseData;

        public SuspectedFourGate(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) 
        {
            BaseData = defaultSharkyBot.BaseData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss || !BaseData.EnemyBaseLocations.Any()) { return false; }

            if (frame > SharkyOptions.FramesPerSecond * 4 * 60) { return false; }

            if (UnitCountService.EnemyHas(new List<UnitTypes> { UnitTypes.PROTOSS_ROBOTICSFACILITY, UnitTypes.PROTOSS_TWILIGHTCOUNCIL, UnitTypes.PROTOSS_DARKSHRINE })) { return false; }

            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_NEXUS) > 1) { return false; }

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.PROTOSS_GATEWAY) >= 3 && UnitCountService.EnemyHas(new List<UnitTypes> { UnitTypes.PROTOSS_CYBERNETICSCORE }))
            {
                return true;
            }          

            return false;
        }
    }
}
