using SC2APIProtocol;
using Sharky.DefaultBot;
using System;

namespace Sharky.Builds
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

        public QuickBuildFollower(DefaultSharkyBot defaultSharkyBot)
        {
            MacroData = defaultSharkyBot.MacroData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            UnitCountService = defaultSharkyBot.UnitCountService;
            EnemyData = defaultSharkyBot.EnemyData;
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
                    MacroData.DesiredUnitCounts[unit] += unitCount;
                    MacroData.DesiredTechCounts[unit] += unitCount;
                    MacroData.DesiredProductionCounts[unit] += unitCount;
                    MacroData.DesiredMorphCounts[unit] += unitCount;
                    throw new NotImplementedException("todo: this needs to be updated to properly produce unit/techbuilding/productionbuilding/morph");
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
