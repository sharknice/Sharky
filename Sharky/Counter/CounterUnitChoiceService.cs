using Sharky.DefaultBot;
using System.Collections.Generic;

namespace Sharky.Counter
{
    public class CounterUnitChoiceService
    {
        UnitCountService UnitCountService;

        public CounterUnitChoiceService(DefaultSharkyBot defaultSharkyBot)
        {
            UnitCountService = defaultSharkyBot.UnitCountService;
        }

        public List<UnitTypes> GetAvailableCounterChoices()
        {
            // TODO: zerg, terran, protoss specific

            var unitTypes = new List<UnitTypes>();

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_GREATERSPIRE) > 0)
            {
                unitTypes.Add(UnitTypes.ZERG_BROODLORD);
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_HIVE) > 0)
            {
                unitTypes.Add(UnitTypes.ZERG_VIPER);
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_SPIRE) > 0)
            {
                unitTypes.Add(UnitTypes.ZERG_CORRUPTOR);
                unitTypes.Add(UnitTypes.ZERG_MUTALISK);
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_ULTRALISKCAVERN) > 0)
            {
                unitTypes.Add(UnitTypes.ZERG_ULTRALISK);
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_INFESTATIONPIT) > 0)
            {
                unitTypes.Add(UnitTypes.ZERG_SWARMHOSTMP);
                unitTypes.Add(UnitTypes.ZERG_INFESTOR);
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_LURKERDENMP) > 0)
            {
                unitTypes.Add(UnitTypes.ZERG_LURKERMP);
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_HYDRALISKDEN) > 0)
            {
                unitTypes.Add(UnitTypes.ZERG_HYDRALISK);
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_ROACHWARREN) > 0)
            {
                unitTypes.Add(UnitTypes.ZERG_RAVAGER);
                unitTypes.Add(UnitTypes.ZERG_ROACH);
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_BANELINGNEST) > 0)
            {
                unitTypes.Add(UnitTypes.ZERG_BANELING);
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_SPAWNINGPOOL) > 0)
            {
                unitTypes.Add(UnitTypes.ZERG_QUEEN);
                unitTypes.Add(UnitTypes.ZERG_ZERGLING);
            }

            return unitTypes;
        }
    }
}
