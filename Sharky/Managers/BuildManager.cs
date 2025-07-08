namespace Sharky.Managers
{
    public class BuildManager : SharkyManager
    {
        protected DebugService DebugService;
        protected Dictionary<Race, BuildChoices> BuildChoices;
        public IBuildDecisionService BuildDecisionService { get; set; }
        protected IEnemyPlayerService EnemyPlayerService;
        protected FrameToTimeConverter FrameToTimeConverter;
        protected SharkyOptions SharkyOptions;
        protected ChatService ChatService;
        protected TagService TagService;

        protected IMacroBalancer MacroBalancer;
        protected ISharkyBuild CurrentBuild;
        public List<string> BuildSequence { get; protected set; }
        protected List<string> PlannedBuildSequence;

        protected SimCityService SimCityService;

        public Dictionary<int, string> BuildHistory { get; protected set; }

        protected Race SelectedRace;
        protected Race ActualRace;
        protected Race EnemySelectedRace;
        protected Race EnemyRace;
        protected string MapName;
        protected EnemyPlayer.EnemyPlayer EnemyPlayer;
        protected ChatHistory ChatHistory;
        protected EnemyStrategyHistory EnemyStrategyHistory;

        EnemyData EnemyData;
        BaseData BaseData;

        public bool ShowBuildText { get; set; } = true;

        public BuildManager(DefaultSharkyBot defaultSharkyBot)
        {
            BuildChoices = defaultSharkyBot.BuildChoices;
            DebugService = defaultSharkyBot.DebugService;
            MacroBalancer = defaultSharkyBot.MacroBalancer;
            BuildDecisionService = defaultSharkyBot.BuildDecisionService;
            EnemyPlayerService = defaultSharkyBot.EnemyPlayerService;
            ChatHistory = defaultSharkyBot.ChatHistory;
            EnemyStrategyHistory = defaultSharkyBot.EnemyStrategyHistory;
            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            ChatService = defaultSharkyBot.ChatService;
            TagService = defaultSharkyBot.TagService;
            SimCityService = defaultSharkyBot.SimCityService;
            EnemyData = defaultSharkyBot.EnemyData;
            BaseData = defaultSharkyBot.BaseData;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            GetPlayerInfo(gameInfo, playerId, opponentId);

            if (EnemyPlayerService.Tournament.Enabled)
            {
                foreach (var buildSequence in EnemyPlayerService.Tournament.BuildSequences)
                {
                    foreach (var sequence in buildSequence.Value)
                    {
                        BuildChoices[(Race)Enum.Parse(typeof(Race), buildSequence.Key)].BuildSequences[sequence.Key] = sequence.Value;
                    }
                }
            }

            var buildSequences = BuildChoices[ActualRace].BuildSequences[EnemyRace.ToString()];
            if (!string.IsNullOrWhiteSpace(EnemyPlayer.Name) && BuildChoices[ActualRace].BuildSequences.ContainsKey(EnemyPlayer.Name))
            {
                buildSequences = BuildChoices[ActualRace].BuildSequences[EnemyPlayer.Name];
            }

            MapName = gameInfo.MapName;
            BuildSequence = BuildDecisionService.GetBestBuild(EnemyPlayer, buildSequences, MapName, EnemyPlayerService.Enemies, EnemyRace, ActualRace);
            PlannedBuildSequence = BuildSequence.ToList();

            BuildHistory = new Dictionary<int, string>();
            SwitchBuild(BuildSequence.First(), 0);
        }

        protected void GetPlayerInfo(ResponseGameInfo gameInfo, uint playerId, string opponentId)
        {
            string enemyName = string.Empty;

            foreach (var playerInfo in gameInfo.PlayerInfo)
            {
                if (playerInfo.PlayerId == playerId)
                {
                    ActualRace = playerInfo.RaceActual;
                    SelectedRace = playerInfo.RaceRequested;
                    if (playerInfo.RaceRequested == Race.Random)
                    {
                        TagService.Tag($"SelfRandomRace_{playerInfo.RaceActual}");
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(playerInfo.PlayerName))
                    {
                        enemyName = playerInfo.PlayerName;
                        if (enemyName == "HUMAN")
                        {
                            enemyName = Environment.UserName;
                        }
                    }
                    EnemyRace = playerInfo.RaceRequested;
                    EnemySelectedRace = playerInfo.RaceRequested;
                }
            }

            EnemyPlayer = EnemyPlayerService.Enemies.FirstOrDefault(e => e.Id == opponentId);
            if (opponentId == "test" && EnemyPlayer == null)
            {
                EnemyPlayer = new EnemyPlayer.EnemyPlayer { ChatMatches = new List<string>(), Games = new List<Game>(), Id = opponentId, Name = "test" };
            }
            if (opponentId == "HUMAN" && EnemyPlayer == null)
            {
                EnemyPlayer = new EnemyPlayer.EnemyPlayer { ChatMatches = new List<string>(), Games = new List<Game>(), Id = opponentId, Name = enemyName };
            }
            if (EnemyPlayer == null)
            {
                EnemyPlayer = new EnemyPlayer.EnemyPlayer { ChatMatches = new List<string>(), Games = new List<Game>(), Id = opponentId, Name = enemyName };
            }
            EnemyData.EnemyPlayer = EnemyPlayer;
        }

        public override IEnumerable<SC2Action> OnFrame(ResponseObservation observation)
        {
            if (ShowBuildText)
            {
                DebugService.DrawText("Build: " + CurrentBuild.Name());
                DebugService.DrawText("Sequence: " + string.Join(", ", BuildSequence));
            }

            var frame = (int)observation.Observation.GameLoop;

            var counterTransition = CurrentBuild.CounterTransition(frame);
            if (counterTransition != null && counterTransition.Any())
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
            CurrentBuild.OnAfterFrame();

            var actions = SimCityService.OnFrame();
            MacroBalance();

            return actions;
        }

        protected void MacroBalance()
        {
            if (BaseData.SelfBases == null) { return; }

            MacroBalancer.BalanceSupply();
            MacroBalancer.BalanceGases();
            MacroBalancer.BalanceTech();
            MacroBalancer.BalanceAddOns();
            MacroBalancer.BalanceDefensiveBuildings();
            MacroBalancer.BalanceProduction();
            MacroBalancer.BalanceProductionBuildings();
            MacroBalancer.BalanceMorphs();
            MacroBalancer.BalanceGasWorkers();
        }

        public override void OnEnd(ResponseObservation observation, Result result)
        {
            Console.WriteLine($"Build Sequence: {string.Join(" ", BuildHistory.Select(b => b.Value.ToString()))}");
            Console.WriteLine($"{result}");

            var game = GetGame(observation, result);
            EnemyPlayerService.SaveGame(game);
        }

        protected Game GetGame(ResponseObservation observation, Result result)
        {
            var length = 0;
            if (observation != null)
            {
                length = (int)observation.Observation.GameLoop;
            }
            return new Game { DateTime = DateTime.Now, EnemySelectedRace = EnemySelectedRace, MySelectedRace = SelectedRace, MyRace = ActualRace, EnemyRace = EnemyRace, Length = length, MapName = MapName, Result = (int)result, EnemyId = EnemyPlayer.Id, Builds = BuildHistory, EnemyStrategies = EnemyStrategyHistory.History, EnemyChat = ChatHistory.EnemyChatHistory, MyChat = ChatHistory.MyChatHistory, PlannedBuildSequence = PlannedBuildSequence };
        }

        protected void SwitchBuild(string buildName, int frame)
        {
            BuildHistory[frame] = buildName;
            if (CurrentBuild != null)
            {
                CurrentBuild.EndBuild(frame);
            }
            CurrentBuild = BuildChoices[ActualRace].Builds[buildName];
            CurrentBuild.StartBuild(frame);
        }

        protected void TransitionBuild(int frame)
        {
            var key = $"{EnemyRace}-Transition";
            if (!BuildChoices[ActualRace].BuildSequences.ContainsKey(key))
            {
                key = "Transition";
            }
            BuildSequence = BuildChoices[ActualRace].BuildSequences[key][new Random().Next(BuildChoices[ActualRace].BuildSequences[key].Count)];
            SwitchBuild(BuildSequence[0], frame);
        }
    }
}
