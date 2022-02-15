using Sharky;
using Sharky.DefaultBot;

namespace SharkyTerranExampleBot.Builds.BuildServices
{
    public class ExpandForever
    {
        UnitCountService UnitCountService;
        MacroData MacroData;

        public ExpandForever(DefaultSharkyBot defaultSharkyBot)
        {
            UnitCountService = defaultSharkyBot.UnitCountService;
            MacroData = defaultSharkyBot.MacroData;
        }

        public void OnFrame()
        {
            if (MacroData.Minerals > 650)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] <= UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER))
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER]++;
                }
            }

            MorphCommandCenters();
        }

        void MorphCommandCenters()
        {
            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_COMMANDCENTER) >= 2)
            {
                if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_BARRACKS) > 0)
                {
                    if (MacroData.DesiredMorphCounts[UnitTypes.TERRAN_ORBITALCOMMAND] < 2)
                    {
                        MacroData.DesiredMorphCounts[UnitTypes.TERRAN_ORBITALCOMMAND] = 2;
                    }
                }
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_COMMANDCENTER) > 3)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.TERRAN_ENGINEERINGBAY] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.TERRAN_ENGINEERINGBAY] = 1;
                }
                if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_ENGINEERINGBAY) > 0)
                {
                    if (MacroData.DesiredMorphCounts[UnitTypes.TERRAN_PLANETARYFORTRESS] < UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_COMMANDCENTER) - 3)
                    {
                        MacroData.DesiredMorphCounts[UnitTypes.TERRAN_PLANETARYFORTRESS] = UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_COMMANDCENTER) - 3;
                    }
                }
            }
        }
    }
}
