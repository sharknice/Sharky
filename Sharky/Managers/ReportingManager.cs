using SC2APIProtocol;
using Sharky.DefaultBot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sharky.Managers
{
    /// <summary>
    /// Reporting manager allows detailed frame reporting to follow what happened in the game from logs.
    /// </summary>
    public class ReportingManager : SharkyManager
    {
        private readonly DefaultSharkyBot DefaultSharkyBot;
        
        /// <summary>
        /// Which Nth frame should be logged
        /// </summary>
        private readonly int logFrameInterval;

        /// <summary>
        /// Creates instance of reporting manager.
        /// </summary>
        /// <param name="defaultSharkyBot">Sharky bot to read the data from.</param>
        /// <param name="logInterval">Log interval in seconds</param>
        public ReportingManager(DefaultSharkyBot defaultSharkyBot, float logInterval = 10.0f)
        {
            DefaultSharkyBot = defaultSharkyBot;
            logFrameInterval = (int)(logInterval * 22.4f);
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if ((observation.Observation.GameLoop) > 10 && (observation.Observation.GameLoop % logFrameInterval == 0))
                DetailedFrame((int)observation.Observation.GameLoop);

            return actions;
        }

        /// <summary>
        /// Prints detailed frame info
        /// </summary>
        /// <param name="frame"></param>
        private void DetailedFrame(int frame)
        {
            var elapsedTime = DefaultSharkyBot.FrameToTimeConverter.GetTime(frame);
            Console.WriteLine(new String('=', 20));
            Console.WriteLine($"Frame {frame} report, elapsed time: {elapsedTime}, {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MiB memory used");
            Console.WriteLine($"  Minerals: {DefaultSharkyBot.MacroData.Minerals} Gas: {DefaultSharkyBot.MacroData.VespeneGas} Supply: {DefaultSharkyBot.MacroData.FoodUsed}/{DefaultSharkyBot.MacroData.FoodLeft + DefaultSharkyBot.MacroData.FoodUsed} ({DefaultSharkyBot.MacroData.FoodArmy} army) Larvae: {DefaultSharkyBot.UnitCountService.Count(UnitTypes.ZERG_LARVA)}");
            Console.WriteLine($"  Workers: {DefaultSharkyBot.UnitCountService.UnitsDoneAndInProgressCount(UnitTypes.ZERG_DRONE)} from wanted {DefaultSharkyBot.MacroData.DesiredUnitCounts[UnitTypes.ZERG_DRONE]} (strict: {DefaultSharkyBot.BuildOptions.StrictWorkerCount}), per extractor {DefaultSharkyBot.BuildOptions.StrictWorkersPerGasCount} (strict: {DefaultSharkyBot.BuildOptions.StrictWorkersPerGas}), extractors: {DefaultSharkyBot.UnitCountService.EquivalentTypeCount(UnitTypes.ZERG_EXTRACTOR)} from {DefaultSharkyBot.MacroData.DesiredGases}");
            Console.WriteLine($"  Desired units:");
            foreach (var entry in DefaultSharkyBot.MacroData.DesiredUnitCounts)
            {
                int amountHave = DefaultSharkyBot.UnitCountService.Completed(entry.Key);
                int amountHaveInProgress = DefaultSharkyBot.UnitCountService.UnitsInProgressCount(entry.Key);
                if (entry.Value > 0 || amountHave > 0 || amountHaveInProgress > 0)
                    Console.WriteLine($"    [{entry.Key}]={entry.Value} ({amountHave} have, {amountHaveInProgress} in progress)");
            }
            Console.WriteLine("  Desired production:");
            foreach (var entry in DefaultSharkyBot.MacroData.DesiredProductionCounts)
            {
                int amountHave = DefaultSharkyBot.UnitCountService.Completed(entry.Key);
                int amountHaveInProgress = DefaultSharkyBot.UnitCountService.BuildingsInProgressCount(entry.Key);
                if (entry.Value > 0 || amountHave > 0 || amountHaveInProgress > 0)
                    Console.WriteLine($"    [{entry.Key}]={entry.Value} ({amountHave} have, {amountHaveInProgress} in progress)");
            }
            Console.WriteLine("  Desired techs:");
            foreach (var entry in DefaultSharkyBot.MacroData.DesiredTechCounts)
            {
                int amountHave = DefaultSharkyBot.UnitCountService.Completed(entry.Key);
                int amountHaveInProgress = DefaultSharkyBot.UnitCountService.BuildingsInProgressCount(entry.Key);
                if (entry.Value > 0 || amountHave > 0 || amountHaveInProgress > 0)
                    Console.WriteLine($"    [{entry.Key}]={entry.Value} ({amountHave} have, {amountHaveInProgress} in progress)");
            }
            Console.WriteLine("  Desired upgrades:");
            foreach (var entry in DefaultSharkyBot.MacroData.DesiredUpgrades)
            {
                if (entry.Value)
                    Console.WriteLine($"    [{entry.Key}]");
            }
            Console.WriteLine("  Researched upgrades:");
            foreach (var entry in DefaultSharkyBot.SharkyUnitData.ResearchedUpgrades)
            {
                var upgrade = (Upgrades)entry;
                Console.WriteLine($"    [{upgrade}]");
            }
            Console.WriteLine("Enemy strategies:");
            foreach (var entry in DefaultSharkyBot.EnemyData.EnemyStrategies)
            {
                if (entry.Value.Detected)
                    Console.WriteLine($"    [{entry.Key}] is { (entry.Value.Active ? "active" : "inactive") }");
            }
            CheckCommanders();
            Console.WriteLine(new String('=', 20));
        }

        private void CheckCommanders()
        {
            foreach (var m1 in DefaultSharkyBot.MicroTaskData.MicroTasks)
            {
                foreach (var m2 in DefaultSharkyBot.MicroTaskData.MicroTasks)
                {
                    if (m1.Key == m2.Key)
                        continue;

                    var multipleCommanders = m1.Value.UnitCommanders.Intersect(m2.Value.UnitCommanders);

                    foreach (var mul in multipleCommanders)
                    {
                        Console.WriteLine($"!!! Unit {(UnitTypes)mul.UnitCalculation.Unit.UnitType} with role {mul.UnitRole} has multiple commanders: {m1.Key} and {m2.Key}");
                    }
                }
            }
        }
    }
}
