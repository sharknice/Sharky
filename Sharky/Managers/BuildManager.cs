using SC2APIProtocol;
using Sharky.Builds;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers
{
    public class BuildManager : SharkyManager
    {
        DebugManager DebugManager;
        MacroManager Macro;
        BuildChoices BuildChoices;

        IMacroBalancer MacroBalancer;
        ISharkyBuild CurrentBuild;
        List<string> BuildSequence;

        Dictionary<int, string> BuildHistory { get; set; }

        Race ActualRace;

        public BuildManager(MacroManager macro, BuildChoices buildChoices, DebugManager debugManager, IMacroBalancer macroBalancer)
        {
            Macro = macro;
            BuildChoices = buildChoices;
            DebugManager = debugManager;
            MacroBalancer = macroBalancer;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var playerInfo in gameInfo.PlayerInfo)
            {
                if (playerInfo.PlayerId == playerId)
                {
                    ActualRace = playerInfo.RaceActual;
                }
            }

            var enemyRace = Race.Zerg;
            var enemyName = "test";
            var buildSequences = BuildChoices.BuildSequences[enemyRace.ToString()];
            if (!string.IsNullOrWhiteSpace(enemyName) && BuildChoices.BuildSequences.ContainsKey(enemyName))
            {
                buildSequences = BuildChoices.BuildSequences[enemyName];
            }

            BuildSequence = buildSequences.First(); //BuildDecisionManager.GetBestBuild(enemyBot, buildSequences, gameInfo.MapName, EnemyBotManager.Enemies, EnemyRace, ChatManager);

            BuildHistory = new Dictionary<int, string>();
            SwitchBuild(BuildSequence.First(), 0);
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            DebugManager.DrawText("Build: " + CurrentBuild.Name());
            DebugManager.DrawText("Sequence: " + string.Join(", ", BuildSequence));

            var counterTransition = CurrentBuild.CounterTransition();
            if (counterTransition != null && counterTransition.Count() > 0)
            {
                BuildSequence = counterTransition;
                SwitchBuild(BuildSequence[0], (int)observation.Observation.GameLoop);
            }
            else if (CurrentBuild.Transition())
            {
                var buildSequenceIndex = BuildSequence.FindIndex(b => b == CurrentBuild.Name());
                if (buildSequenceIndex != -1 && BuildSequence.Count() > buildSequenceIndex + 1)
                {
                    SwitchBuild(BuildSequence[buildSequenceIndex + 1], (int)observation.Observation.GameLoop);
                }
                else
                {
                    TransitionBuild((int)observation.Observation.GameLoop);
                }
            }

            CurrentBuild.OnFrame(observation);

            MacroBalancer.BalanceSupply();
            MacroBalancer.BalanceGases();
            MacroBalancer.BalanceTech();
            MacroBalancer.BalanceProduction();
            MacroBalancer.BalanceProductionBuildings();
            MacroBalancer.BalanceGasWorkers();

            return new List<SC2APIProtocol.Action>();
        }

        void SwitchBuild(string buildName, int frame)
        {
            BuildHistory[frame] = buildName;
            CurrentBuild = BuildChoices.Builds[buildName];
            CurrentBuild.StartBuild(frame);
        }

        void TransitionBuild(int frame)
        {
            BuildSequence = BuildChoices.BuildSequences["Transition"][new Random().Next(BuildChoices.BuildSequences["Transition"].Count)];
            SwitchBuild(BuildSequence[0], frame);
        }
    }
}
