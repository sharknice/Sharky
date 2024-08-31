namespace Sharky.Managers
{
    /// <summary>
    /// Reporting manager allows detailed frame reporting to follow what happened in the game from logs.
    /// </summary>
    public class ReportingManager : SharkyManager
    {
        DateTime StartTime;
        DateTime LastLogTime;

        private readonly DefaultSharkyBot DefaultSharkyBot;

        /// <summary>
        /// Which Nth frame should be logged
        /// </summary>
        private readonly int logFrameInterval;

        private readonly float LogInterval;

        /// <summary>
        /// Creates instance of reporting manager.
        /// </summary>
        /// <param name="defaultSharkyBot">Sharky bot to read the data from.</param>
        /// <param name="logInterval">Log interval in seconds</param>
        public ReportingManager(DefaultSharkyBot defaultSharkyBot, float logInterval = 10.0f)
        {
            DefaultSharkyBot = defaultSharkyBot;
            LogInterval = logInterval;
            logFrameInterval = (int)(logInterval * defaultSharkyBot.SharkyOptions.FramesPerSecond);
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            StartTime = DateTime.Now;
            LastLogTime = DateTime.Now;
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (DefaultSharkyBot.SharkyOptions.GameStatusReportingEnabled)
            {
                if (observation.Observation.GameLoop > 10 && (observation.Observation.GameLoop % logFrameInterval == 0))
                    DetailedFrame((int)observation.Observation.GameLoop);
            }

            return actions;
        }

        /// <summary>
        /// Prints detailed frame info
        /// </summary>
        /// <param name="frame"></param>
        private void DetailedFrame(int frame)
        {
            var elapsedTime = DefaultSharkyBot.FrameToTimeConverter.GetTime(frame);
            var elapsedRealTime = DateTime.Now - StartTime;
            Console.WriteLine(new String('=', 20));
            Console.WriteLine($"Frame {frame} report, elapsed game time: {elapsedTime}, real time: {elapsedRealTime.ToString(@"hh\:mm\:ss")}, {Math.Round(elapsedTime.TotalSeconds / (double)elapsedRealTime.TotalSeconds, 2):f2}X speed, {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MiB memory used");
            Console.WriteLine($"Average Frames, calculation: {Math.Round(DefaultSharkyBot.PerformanceData.TotalFrameCalculationTime / frame)} ms, game: {Math.Round(elapsedRealTime.TotalMilliseconds / frame)} ms ({Math.Round(frame / (double)elapsedRealTime.TotalSeconds)} fps)");
            LogIntervalPerformance(frame);

            var larva = "";
            if (DefaultSharkyBot.EnemyData.SelfRace == Race.Zerg)
            {
                larva = $"Larvae: {DefaultSharkyBot.UnitCountService.Count(UnitTypes.ZERG_LARVA)}";
            }
            Console.WriteLine($"  Minerals: {DefaultSharkyBot.MacroData.Minerals} Gas: {DefaultSharkyBot.MacroData.VespeneGas} Supply: {DefaultSharkyBot.MacroData.FoodUsed}/{DefaultSharkyBot.MacroData.FoodLeft + DefaultSharkyBot.MacroData.FoodUsed} ({DefaultSharkyBot.MacroData.FoodArmy} army) {larva}");

            var workerType = UnitTypes.ZERG_DRONE;
            var gasType = UnitTypes.ZERG_EXTRACTOR;
            if (DefaultSharkyBot.EnemyData.SelfRace == Race.Protoss)
            {
                workerType = UnitTypes.PROTOSS_PROBE;
                gasType = UnitTypes.PROTOSS_ASSIMILATOR;
            }
            else if (DefaultSharkyBot.EnemyData.SelfRace == Race.Terran)
            {
                workerType = UnitTypes.TERRAN_SCV;
                gasType = UnitTypes.TERRAN_REFINERY;
            }
            Console.WriteLine($"  Workers: {DefaultSharkyBot.UnitCountService.UnitsDoneAndInProgressCount(workerType)} from wanted {DefaultSharkyBot.MacroData.DesiredUnitCounts[workerType]} (strict: {DefaultSharkyBot.BuildOptions.StrictWorkerCount}), per gas {DefaultSharkyBot.BuildOptions.StrictWorkersPerGasCount} (strict: {DefaultSharkyBot.BuildOptions.StrictWorkersPerGas}), gas: {DefaultSharkyBot.UnitCountService.EquivalentTypeCount(gasType)} from {DefaultSharkyBot.MacroData.DesiredGases}");
            Console.WriteLine($"  Desired units:");
            foreach (var entry in DefaultSharkyBot.MacroData.DesiredUnitCounts.OrderBy(x => Enum.GetName(typeof(UnitTypes), x.Key)))
            {
                int amountHave = DefaultSharkyBot.UnitCountService.EquivalentTypeCompleted(entry.Key);
                int amountHaveInProgress = DefaultSharkyBot.UnitCountService.UnitsInProgressCount(entry.Key);
                if (entry.Value > 0 || amountHave > 0 || amountHaveInProgress > 0)
                    Console.WriteLine($"    [{entry.Key}]={entry.Value} ({amountHave} have, {amountHaveInProgress} in progress)");
            }
            Console.WriteLine("  Desired production:");
            foreach (var entry in DefaultSharkyBot.MacroData.DesiredProductionCounts.OrderBy(x => Enum.GetName(typeof(UnitTypes), x.Key)))
            {
                int amountHave = DefaultSharkyBot.UnitCountService.EquivalentTypeCompleted(entry.Key);
                int amountHaveInProgress = DefaultSharkyBot.UnitCountService.BuildingsInProgressCount(entry.Key);
                if (entry.Value > 0 || amountHave > 0 || amountHaveInProgress > 0)
                    Console.WriteLine($"    [{entry.Key}]={entry.Value} ({amountHave} have, {amountHaveInProgress} in progress)");
            }
            Console.WriteLine("  Desired techs:");
            foreach (var entry in DefaultSharkyBot.MacroData.DesiredTechCounts.OrderBy(x => Enum.GetName(typeof(UnitTypes), x.Key)))
            {
                int amountHave = DefaultSharkyBot.UnitCountService.EquivalentTypeCompleted(entry.Key);
                int amountHaveInProgress = DefaultSharkyBot.UnitCountService.BuildingsInProgressCount(entry.Key);
                if (entry.Value > 0 || amountHave > 0 || amountHaveInProgress > 0)
                    Console.WriteLine($"    [{entry.Key}]={entry.Value} ({amountHave} have, {amountHaveInProgress} in progress)");
            }
            Console.WriteLine("  Desired defense at defensive point:");
            foreach (var entry in DefaultSharkyBot.MacroData.DesiredDefensiveBuildingsAtDefensivePoint.OrderBy(x => Enum.GetName(typeof(UnitTypes), x.Key)))
            {
                int amountHave = DefaultSharkyBot.UnitCountService.EquivalentTypeCompleted(entry.Key);
                int amountHaveInProgress = DefaultSharkyBot.UnitCountService.BuildingsInProgressCount(entry.Key);
                if (entry.Value > 0 || amountHave > 0 || amountHaveInProgress > 0)
                    Console.WriteLine($"    [{entry.Key}]={entry.Value} ({amountHave} have, {amountHaveInProgress} in progress)");
            }
            Console.WriteLine("  Desired defense at defensive at every base:");
            foreach (var entry in DefaultSharkyBot.MacroData.DesiredDefensiveBuildingsAtEveryBase.OrderBy(x => Enum.GetName(typeof(UnitTypes), x.Key)))
            {
                int amountHave = DefaultSharkyBot.UnitCountService.EquivalentTypeCompleted(entry.Key);
                int amountHaveInProgress = DefaultSharkyBot.UnitCountService.BuildingsInProgressCount(entry.Key);
                if (entry.Value > 0 || amountHave > 0 || amountHaveInProgress > 0)
                    Console.WriteLine($"    [{entry.Key}]={entry.Value} ({amountHave} have, {amountHaveInProgress} in progress)");
            }
            Console.WriteLine("  Desired defense at defensive at every mineral line:");
            foreach (var entry in DefaultSharkyBot.MacroData.DesiredDefensiveBuildingsAtEveryMineralLine.OrderBy(x => Enum.GetName(typeof(UnitTypes), x.Key)))
            {
                int amountHave = DefaultSharkyBot.UnitCountService.EquivalentTypeCompleted(entry.Key);
                int amountHaveInProgress = DefaultSharkyBot.UnitCountService.BuildingsInProgressCount(entry.Key);
                if (entry.Value > 0 || amountHave > 0 || amountHaveInProgress > 0)
                    Console.WriteLine($"    [{entry.Key}]={entry.Value} ({amountHave} have, {amountHaveInProgress} in progress)");
            }
            if (DefaultSharkyBot.EnemyData.SelfRace == Race.Protoss)
            {
                Console.WriteLine("  Protosss Macro Data:");
                Console.WriteLine($"    [DesiredExtraBaseSimCityBatteries]={DefaultSharkyBot.MacroData.ProtossMacroData.DesiredExtraBaseSimCityBatteries}");
                Console.WriteLine($"    [DesiredExtraBaseSimCityCannons]={DefaultSharkyBot.MacroData.ProtossMacroData.DesiredExtraBaseSimCityCannons}");
                Console.WriteLine($"    [DesiredExtraBaseSimCityPylons]={DefaultSharkyBot.MacroData.ProtossMacroData.DesiredExtraBaseSimCityPylons}");
                Console.WriteLine($"    [DesiredPylons]={DefaultSharkyBot.MacroData.ProtossMacroData.DesiredPylons}");
                Console.WriteLine($"    [DesiredPylonsAtDefensivePoint]={DefaultSharkyBot.MacroData.ProtossMacroData.DesiredPylonsAtDefensivePoint}");
                Console.WriteLine($"    [DesiredPylonsAtEveryBase]={DefaultSharkyBot.MacroData.ProtossMacroData.DesiredPylonsAtEveryBase}");
                Console.WriteLine($"    [ExtraBasesGatewayCannonSimCity]={DefaultSharkyBot.MacroData.ProtossMacroData.ExtraBasesGatewayCannonSimCity}");
                Console.WriteLine($"    [DesiredPylonsAtNextBase]={DefaultSharkyBot.MacroData.ProtossMacroData.DesiredPylonsAtNextBase}");
            }
            Console.WriteLine("  Desired upgrades:");
            foreach (var entry in DefaultSharkyBot.MacroData.DesiredUpgrades.OrderBy(x => Enum.GetName(typeof(Upgrades), x.Key)))
            {
                if (entry.Value)
                    Console.WriteLine($"    [{entry.Key}]");
            }
            Console.WriteLine("  Researched upgrades:");
            foreach (var entry in DefaultSharkyBot.SharkyUnitData.ResearchedUpgrades.OrderBy(x => Enum.GetName(typeof(Upgrades), (Upgrades)x)))
            {
                var upgrade = (Upgrades)entry;
                Console.WriteLine($"    [{upgrade}]");

            }
            Console.WriteLine($"Enemy aggressivity: {DefaultSharkyBot.EnemyData.EnemyAggressivityData}");
            Console.WriteLine("Enemy strategies:");
            foreach (var entry in DefaultSharkyBot.EnemyData.EnemyStrategies.OrderBy(x => x.Key))
            {
                if (entry.Value.Detected)
                    Console.WriteLine($"    [{entry.Key}] is {(entry.Value.Active ? "active" : "inactive")} ({FormatElapsedTime(entry.Value.FirstActiveFrame)} to {FormatElapsedTime(entry.Value.LastActiveFrame)})");
            }
            PrintEnemyUnits();

            Console.WriteLine($"Losses:");
            Console.WriteLine($"  Self: {DefaultSharkyBot.ActiveUnitData.SelfDeaths} units, {DefaultSharkyBot.ActiveUnitData.SelfResourcesLost} resources");
            Console.WriteLine($"  Enemy: {DefaultSharkyBot.ActiveUnitData.EnemyDeaths} units, {DefaultSharkyBot.ActiveUnitData.EnemyResourcesLost} resources");
            var targetingPriority = DefaultSharkyBot.TargetPriorityService.CalculateGeneralTargetPriority();
            Console.WriteLine($"Target Calculation Winnablility: {targetingPriority.OverallWinnability}");
            if (DefaultSharkyBot.AttackData?.TargetPriorityCalculation?.TargetPriority != null)
            {
                Console.WriteLine($"TargetPriority: {DefaultSharkyBot.AttackData.TargetPriorityCalculation.TargetPriority}");
            }
            Console.WriteLine($"Attack Point: {DefaultSharkyBot.TargetingData.AttackPoint}");
            Console.WriteLine($"Retreat Point: {DefaultSharkyBot.TargetingData.ForwardDefensePoint}");

            if (DefaultSharkyBot.AttackData.UseAttackDataManager)
            {
                Console.WriteLine($"Attacking: {DefaultSharkyBot.AttackData.Attacking}");
            }
            if (DefaultSharkyBot.TargetingData.AttackState != MicroTasks.Attack.AttackState.None)
            {
                Console.WriteLine($"Attack State: {DefaultSharkyBot.TargetingData.AttackState}");
            }

            CheckCommanders();
            PrintTasks(frame);
            Console.WriteLine(new String('=', 20));
        }

        private void LogIntervalPerformance(int frame)
        {
            var elapsedRealTime = DateTime.Now - LastLogTime;
            var frames = DefaultSharkyBot.SharkyOptions.FramesPerSecond * LogInterval;
            Console.WriteLine($"Last {LogInterval}s game time, {elapsedRealTime.ToString(@"hh\:mm\:ss")} real time, {Math.Round(LogInterval / (double)elapsedRealTime.TotalSeconds, 2):f2}X speed, {Math.Round(DefaultSharkyBot.PerformanceData.TotalFrameCalculationTime / frames)} ms, game: {Math.Round(elapsedRealTime.TotalMilliseconds / frames)} ms ({Math.Round(frames / (double)elapsedRealTime.TotalSeconds)} fps)");
            LastLogTime = DateTime.Now;
        }

        private string FormatElapsedTime(int frames)
        {
            return DefaultSharkyBot.FrameToTimeConverter.GetTime(frames).ToString(@"mm\:ss");
        }

        private void PrintEnemyUnits()
        {
            Console.WriteLine("Enemy Units:");
            var enemyUnitGroups = DefaultSharkyBot.ActiveUnitData.EnemyUnits.GroupBy(x => x.Value.Unit.UnitType);
            var army = enemyUnitGroups.Where(g => g.FirstOrDefault().Value.UnitClassifications.HasFlag(UnitClassification.ArmyUnit));
            Console.WriteLine("  Army:");
            foreach (var group in army.OrderBy(x => Enum.GetName(typeof(UnitTypes), x.Key)))
            {
                Console.WriteLine($"    [{(UnitTypes)group.Key}]={group.Count()}");
            }
            var buildings = enemyUnitGroups.Where(g => g.FirstOrDefault().Value.Attributes.Contains(SC2APIProtocol.Attribute.Structure));
            Console.WriteLine("  Structures:");
            foreach (var group in buildings.OrderBy(x => Enum.GetName(typeof(UnitTypes), x.Key)))
            {
                Console.WriteLine($"    [{(UnitTypes)group.Key}]={group.Count()}");
            }
            var workers = enemyUnitGroups.Where(g => g.FirstOrDefault().Value.UnitClassifications.HasFlag(UnitClassification.Worker));
            Console.WriteLine("  Workers:");
            foreach (var group in workers)
            {
                Console.WriteLine($"    [{(UnitTypes)group.Key}]={group.Count()}");
            }
        }

        private void PrintTasks(int frame)
        {
            Console.WriteLine("Tasks:");
            foreach (var task in DefaultSharkyBot.MicroTaskData.Where(t => t.Value.Enabled))
            {
                Console.WriteLine($"  {task.Key}:");
                var unitGroups = task.Value.UnitCommanders.GroupBy(x => x.UnitCalculation.Unit.UnitType);
                foreach (var group in unitGroups.OrderBy(x => Enum.GetName(typeof(UnitTypes), x.Key)))
                {
                    Console.WriteLine($"    [{(UnitTypes)group.Key}]={group.Count()}");
                }
                task.Value.PrintReport(frame);
            }
        }

        private void CheckCommanders()
        {
            foreach (var m1 in DefaultSharkyBot.MicroTaskData)
            {
                foreach (var m2 in DefaultSharkyBot.MicroTaskData)
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
