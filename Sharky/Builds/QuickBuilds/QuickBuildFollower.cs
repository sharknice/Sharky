using SC2APIProtocol;
using Sharky.DefaultBot;
using System;

namespace Sharky.Builds.QuickBuilds
{
    /// <summary>
    /// Simple class that follows quickbuild and builds what is needed accordingly.
    /// </summary>
    public class QuickBuildFollower
    {
        MacroData MacroData;
        BuildOptions BuildOptions;
        UnitCountService UnitCountService;
        EnemyData EnemyData;
        UnitTypeBuildClassifications UnitTypeBuildClassifications;

        public QuickBuildFollower(DefaultSharkyBot defaultSharkyBot)
        {
            MacroData = defaultSharkyBot.MacroData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            UnitCountService = defaultSharkyBot.UnitCountService;
            EnemyData = defaultSharkyBot.EnemyData;
            UnitTypeBuildClassifications = defaultSharkyBot.UnitTypeBuildClassifications;
        }

        public void Follow(QuickBuild build)
        {
            var step = build.CurrentStep;

            if (step == null)
                return;

            var workerType = UnitTypes.ZERG_DRONE;
            if (EnemyData.SelfRace == Race.Protoss)
            {
                workerType = UnitTypes.PROTOSS_PROBE;
            }
            else if (EnemyData.SelfRace == Race.Terran)
            {
                workerType = UnitTypes.TERRAN_SCV;
            }

            var workerCountActual = UnitCountService.UnitsDoneAndInProgressCount(workerType);
            var workerCountWanted = step.Value.Item1 ?? 0;

            if (workerCountActual >= workerCountWanted)
            {
                if (step.Value.Item2 is UnitTypes unit)
                {
                    var unitCount = step.Value.Item3 ?? 0;
                    if (UnitTypeBuildClassifications.ProducedUnits.Contains(step.Value.Item2))
                    {
                        MacroData.DesiredUnitCounts[unit] += unitCount;
                    }
                    else if (UnitTypeBuildClassifications.TechUnits.Contains(step.Value.Item2))
                    {
                        MacroData.DesiredTechCounts[unit] += unitCount;
                    }
                    else if (UnitTypeBuildClassifications.ProductionUnits.Contains(step.Value.Item2))
                    {
                        MacroData.DesiredProductionCounts[unit] += unitCount;
                    }
                    else if (UnitTypeBuildClassifications.MorphUnits.Contains(step.Value.Item2))
                    {
                        MacroData.DesiredMorphCounts[unit] += unitCount;
                    }
                }
                else if (step.Value.Item2 is Upgrades upgrade)
                {
                    MacroData.DesiredUpgrades[upgrade] = true;
                }
                build.CurrentStepIndex++;
            }
        }
    }
}
