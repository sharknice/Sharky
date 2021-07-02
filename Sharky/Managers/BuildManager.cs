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
        DebugService DebugService;
        Dictionary<Race, BuildChoices> BuildChoices;
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

        public BuildManager(Dictionary<Race, BuildChoices> buildChoices, DebugService debugService, IMacroBalancer macroBalancer, IBuildDecisionService buildDecisionService, IEnemyPlayerService enemyPlayerService, ChatHistory chatHistory, EnemyStrategyHistory enemyStrategyHistory)
        {
            BuildChoices = buildChoices;
            DebugService = debugService;
            MacroBalancer = macroBalancer;
            BuildDecisionService = buildDecisionService;
            EnemyPlayerService = enemyPlayerService;
            ChatHistory = chatHistory;
            EnemyStrategyHistory = enemyStrategyHistory;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            string enemyName = string.Empty;
            
            foreach (var playerInfo in gameInfo.PlayerInfo)
            {                
                if (playerInfo.PlayerId == playerId)
                {
                    ActualRace = playerInfo.RaceActual;
                }
                else
                {
                    if (playerInfo.PlayerName != null)
                    {
                        enemyName = playerInfo.PlayerName;
                    }
                    EnemyRace = playerInfo.RaceRequested;
                }
            }

            EnemyPlayer = EnemyPlayerService.Enemies.FirstOrDefault(e => e.Id == opponentId);
            if (opponentId == "test" && EnemyPlayer == null)
            {
                EnemyPlayer = new EnemyPlayer.EnemyPlayer { ChatMatches = new List<string>(), Games = new List<Game>(), Id = opponentId, Name = "test" };
            }
            if (EnemyPlayer == null)
            {
                EnemyPlayer = new EnemyPlayer.EnemyPlayer { ChatMatches = new List<string>(), Games = new List<Game>(), Id = opponentId, Name = enemyName };
            }

            if (EnemyPlayerService.Tournament.Enabled)
            {
                foreach (var buildSequence in EnemyPlayerService.Tournament.BuildSequences)
                {
                    BuildChoices[ActualRace].BuildSequences[buildSequence.Key] = buildSequence.Value;
                }
            }

            var buildSequences = BuildChoices[ActualRace].BuildSequences[EnemyRace.ToString()];
            if (!string.IsNullOrWhiteSpace(EnemyPlayer.Name) && BuildChoices[ActualRace].BuildSequences.ContainsKey(EnemyPlayer.Name))
            {
                buildSequences = BuildChoices[ActualRace].BuildSequences[EnemyPlayer.Name];
            }

            MapName = gameInfo.MapName;
            BuildSequence = BuildDecisionService.GetBestBuild(EnemyPlayer, buildSequences, MapName, EnemyPlayerService.Enemies, EnemyRace);

            BuildHistory = new Dictionary<int, string>();
            SwitchBuild(BuildSequence.First(), 0);
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            DebugService.DrawText("Build: " + CurrentBuild.Name());
            DebugService.DrawText("Sequence: " + string.Join(", ", BuildSequence));

            var frame = (int)observation.Observation.GameLoop;

            var counterTransition = CurrentBuild.CounterTransition(frame);
            if (counterTransition != null && counterTransition.Count() > 0)
            {
                BuildSequence = counterTransition;
                SwitchBuild(BuildSequence[0], frame);
            }
            else if (CurrentBuild.Transition(frame))
            {
                var buildSequenceIndex = BuildSequence.FindIndex(b => b == CurrentBuild.Name());
                if (buildSequenceIndex != -1 && BuildSequence.Count() > buildSequenceIndex + 1)
                {
                    SwitchBuild(BuildSequence[buildSequenceIndex + 1], frame);
                }
                else
                {
                    TransitionBuild(frame);
                }
            }

            CurrentBuild.OnFrame(observation);

            MacroBalancer.BalanceSupply();
            MacroBalancer.BalanceGases();
            MacroBalancer.BalanceTech();
            MacroBalancer.BalanceAddOns();
            MacroBalancer.BalanceDefensiveBuildings();
            MacroBalancer.BalanceProduction();
            MacroBalancer.BalanceProductionBuildings();
            MacroBalancer.BalanceMorphs();
            MacroBalancer.BalanceGasWorkers();

            return null;
        }

        public override void OnEnd(ResponseObservation observation, Result result)
        {
            var game = new Game { DateTime = DateTime.Now, EnemyRace = EnemyRace, Length = (int)observation.Observation.GameLoop, MapName = MapName, Result = (int)result, EnemyId = EnemyPlayer.Id, Builds = BuildHistory, EnemyStrategies = EnemyStrategyHistory.History, EnemyChat = ChatHistory.EnemyChatHistory, MyChat = ChatHistory.MyChatHistory };
            EnemyPlayerService.SaveGame(game);
        }

        void SwitchBuild(string buildName, int frame)
        {
            BuildHistory[frame] = buildName;
            CurrentBuild = BuildChoices[ActualRace].Builds[buildName];
            CurrentBuild.StartBuild(frame);
        }

        void TransitionBuild(int frame)
        {
            BuildSequence = BuildChoices[ActualRace].BuildSequences["Transition"][new Random().Next(BuildChoices[ActualRace].BuildSequences["Transition"].Count)];
            SwitchBuild(BuildSequence[0], frame);
        }
    }
}
