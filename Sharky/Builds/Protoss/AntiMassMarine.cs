namespace Sharky.Builds.Protoss
{
    public class AntiMassMarine : ProtossSharkyBuild
    {
        public AntiMassMarine(DefaultSharkyBot defaultSharkyBot, ICounterTransitioner counterTransitioner)
            : base(defaultSharkyBot, counterTransitioner)
        {

        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;

            ChronoData.ChronodUpgrades = new HashSet<Upgrades>
            {
                Upgrades.WARPGATERESEARCH
            };

            ChronoData.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_PROBE,
                UnitTypes.PROTOSS_STALKER
            };
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroData.FoodUsed >= 15)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
                }
            }
            if (MacroData.FoodUsed >= 17 && UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredGases < 1)
                {
                    MacroData.DesiredGases = 1;
                }
            }
            if (MacroData.FoodUsed >= 18 && UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredGases < 2)
                {
                    MacroData.DesiredGases = 2;
                }
            }
            if (UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
                ChronoData.ChronodUnits.Remove(UnitTypes.PROTOSS_PROBE);
            }

            if (UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) > 0 && UnitCountService.Count(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 3)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 3;
                }
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 20)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 20;
                }
            }

            if (UnitCountService.Count(UnitTypes.PROTOSS_STALKER) > 0)
            {
                MacroData.ProtossMacroData.DesiredPylonsAtDefensivePoint = 1;
                MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.PROTOSS_SHIELDBATTERY] = 1;
            }
            if (UnitCountService.Count(UnitTypes.PROTOSS_STALKER) >= 2)
            {
                MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.PROTOSS_SHIELDBATTERY] = 2;
            }
            if (UnitCountService.Count(UnitTypes.PROTOSS_STALKER) >= 3)
            {
                MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.PROTOSS_SHIELDBATTERY] = 3;
            }
            if (UnitCountService.Count(UnitTypes.PROTOSS_STALKER) >= 4)
            {
                MacroData.ProtossMacroData.DesiredPylonsAtDefensivePoint = 2;
                MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.PROTOSS_SHIELDBATTERY] = 4;
            }

            if (MacroData.FoodUsed >= 50)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 2;
                }
            }
        }

        public override bool Transition(int frame)
        {
            if (ActiveUnitData.EnemyUnits.Any(e => e.Value.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && e.Value.Unit.UnitType != (uint)UnitTypes.TERRAN_MARINE))
            {
                return true;
            }
            return MacroData.FoodUsed > 50 && UnitCountService.Count(UnitTypes.PROTOSS_NEXUS) > 1;
        }

        public override List<string> CounterTransition(int frame)
        {
            return new List<string>();
        }
    }
}
