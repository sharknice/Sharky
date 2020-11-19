using SC2APIProtocol;
using Sharky.Builds;
using Sharky.Builds.BuildChoosing;
using Sharky.Chat;
using Sharky.EnemyPlayer;
using Sharky.EnemyStrategies;
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
        IBuildDecisionService BuildDecisionService;
        IEnemyPlayerService EnemyPlayerService;

        IMacroBalancer MacroBalancer;
        ISharkyBuild CurrentBuild;
        List<string> BuildSequence;

        Dictionary<int, string> BuildHistory { get; set; }

        Race ActualRace;
        Race EnemyRace;
        string MapName;
        EnemyPlayer.EnemyPlayer EnemyPlayer;
        ChatHistory ChatHistory;
        EnemyStrategyHistory EnemyStrategyHistory;

        public BuildManager(MacroManager macro, BuildChoices buildChoices, DebugManager debugManager, IMacroBalancer macroBalancer, IBuildDecisionService buildDecisionService, IEnemyPlayerService enemyPlayerService, ChatHistory chatHistory, EnemyStrategyHistory enemyStrategyHistory)
        {
            Macro = macro;
            BuildChoices = buildChoices;
            DebugManager = debugManager;
            MacroBalancer = macroBalancer;
            BuildDecisionService = buildDecisionService;
            EnemyPlayerService = enemyPlayerService;
            ChatHistory = chatHistory;
            EnemyStrategyHistory = enemyStrategyHistory;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var playerInfo in gameInfo.PlayerInfo)
            {
                if (playerInfo.PlayerId == playerId)
                {
                    ActualRace = playerInfo.RaceActual;
                }
                else
                {
                    EnemyRace = playerInfo.RaceRequested;
                }
            }

            EnemyPlayer = EnemyPlayerService.Enemies.FirstOrDefault(e => e.Id == opponentId);
            if (opponentId == "test")
            {
                EnemyPlayer = new EnemyPlayer.EnemyPlayer { ChatMatches = new List<string>(), Games = new List<Game>(), Id = opponentId, Name = "test" };
            }
            if (EnemyPlayer == null)
            {
                EnemyPlayer = new EnemyPlayer.EnemyPlayer { ChatMatches = new List<string>(), Games = new List<Game>(), Id = opponentId, Name = string.Empty };
            }


            var buildSequences = BuildChoices.BuildSequences[EnemyRace.ToString()];
            if (!string.IsNullOrWhiteSpace(EnemyPlayer.Name) && BuildChoices.BuildSequences.ContainsKey(EnemyPlayer.Name))
            {
                buildSequences = BuildChoices.BuildSequences[EnemyPlayer.Name];
            }

            MapName = gameInfo.MapName;
            BuildSequence = BuildDecisionService.GetBestBuild(EnemyPlayer, buildSequences, MapName, EnemyPlayerService.Enemies, EnemyRace);

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

        public override void OnEnd(ResponseObservation observation, Result result)
        {
            var game = new Game { DateTime = DateTime.Now, EnemyRace = EnemyRace, Length = (int)observation.Observation.GameLoop, MapName = MapName, Result = (int)result, EnemyId = EnemyPlayer.Id, Builds = BuildHistory, EnemyStrategies = EnemyStrategyHistory.History, EnemyChat = ChatHistory.EnemyChatHistory, MyChat = ChatHistory.MyChatHistory };
            EnemyPlayerService.SaveGame(game);
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
