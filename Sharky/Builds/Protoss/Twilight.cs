namespace Sharky.Builds.Protoss
{
    public class Twilight : ProtossSharkyBuild
    {
        public Twilight(DefaultSharkyBot defaultSharkyBot, ICounterTransitioner counterTransitioner)
            : base(defaultSharkyBot, counterTransitioner)
        {
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
            }
            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_TWILIGHTCOUNCIL] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_TWILIGHTCOUNCIL] = 1;
                }
            }
        }

        public override bool Transition(int frame)
        {
            return UnitCountService.Completed(UnitTypes.PROTOSS_TWILIGHTCOUNCIL) > 0;
        }
    }
}
