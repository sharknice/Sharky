using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SC2APIProtocol;

namespace Sharky
{
    public class GameConnection
    {
        ProtobufProxy Proxy = new ProtobufProxy();
        string address = "127.0.0.1";

        string starcraftExe;
        string starcraftDir;

        // bool frameMismatchDetected = false;

        public GameConnection() { }

        public void StartSC2Instance(int port)
        {
            var processStartInfo = new ProcessStartInfo(starcraftExe);
            processStartInfo.Arguments = String.Format("-listen {0} -port {1} -displayMode 0", address, port);
            processStartInfo.WorkingDirectory = Path.Combine(starcraftDir, "Support64");
            Process.Start(processStartInfo);
        }

        public async Task Connect(int port)
        {
            for (int i = 0; i < 40; i++)
            {
                try
                {
                    await Proxy.Connect(address, port);
                    return;
                }
                catch (WebSocketException) { }
                Thread.Sleep(2000);
            }
            throw new Exception("Unable to make a connection.");
        }

        public async Task CreateGame(String mapName, Race opponentRace, Difficulty opponentDifficulty, AIBuild aIBuild, int randomSeed = -1)
        {
            var createGame = new RequestCreateGame();
            createGame.Realtime = false;

            if (randomSeed >= 0)
            {
                createGame.RandomSeed = (uint)randomSeed;
            }

            string mapPath = Path.Combine(starcraftDir, "Maps", mapName);
            if (!File.Exists(mapPath))
            {
                throw new Exception("Could not find map at " + mapPath);
            }
            createGame.LocalMap = new LocalMap();
            createGame.LocalMap.MapPath = mapPath;

            var player1 = new PlayerSetup();
            createGame.PlayerSetup.Add(player1);
            player1.Type = PlayerType.Participant;

            var player2 = new PlayerSetup();
            createGame.PlayerSetup.Add(player2);
            player2.Race = opponentRace;
            player2.Type = PlayerType.Computer;
            player2.Difficulty = opponentDifficulty;
            player2.AiBuild = aIBuild;

            var request = new Request();
            request.CreateGame = createGame;
            var response = await Proxy.SendRequest(request);
        }

        private void readSettings()
        {
            var myDocuments = Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var executeInfo = Path.Combine(myDocuments, "Starcraft II", "ExecuteInfo.txt");
            if (File.Exists(executeInfo))
            {
                var lines = File.ReadAllLines(executeInfo);
                foreach (string line in lines)
                {
                    var argument = line.Substring(line.IndexOf('=') + 1).Trim();
                    if (line.Trim().StartsWith("executable"))
                    {
                        starcraftExe = argument;
                        starcraftDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(starcraftExe)));
                    }
                }
            }
            else
            {
                throw new Exception("Unable to find ExecuteInfo.txt at " + executeInfo);
            }
        }
        
        public async Task<uint> JoinGame(Race race)
        {
            var joinGame = new RequestJoinGame();
            joinGame.Race = race;

            joinGame.Options = new InterfaceOptions();
            joinGame.Options.FeatureLayer = new SpatialCameraSetup { CropToPlayableArea = true, AllowCheatingLayers = false, MinimapResolution = new Size2DI { X = 16, Y = 16 }, Resolution = new Size2DI { X = 128, Y = 128 }, Width = 10 };
            joinGame.Options.Raw = true;
            joinGame.Options.Score = true;
            joinGame.Options.ShowCloaked = true;
            joinGame.Options.ShowBurrowedShadows = true;
            joinGame.Options.RawCropToPlayableArea = true;
            joinGame.Options.RawAffectsSelection = true;

            var request = new Request();
            request.JoinGame = joinGame;
            var response = await Proxy.SendRequest(request);
            return response.JoinGame.PlayerId;
        }

        public async Task<uint> JoinGameLadder(Race race, int startPort)
        {
            var joinGame = new RequestJoinGame();
            joinGame.Race = race;
            
            joinGame.SharedPort = startPort + 1;
            joinGame.ServerPorts = new PortSet();
            joinGame.ServerPorts.GamePort = startPort + 2;
            joinGame.ServerPorts.BasePort = startPort + 3;

            joinGame.ClientPorts.Add(new PortSet());
            joinGame.ClientPorts[0].GamePort = startPort + 4;
            joinGame.ClientPorts[0].BasePort = startPort + 5;

            joinGame.Options = new InterfaceOptions();
            joinGame.Options.FeatureLayer = new SpatialCameraSetup { CropToPlayableArea = true, AllowCheatingLayers = false, MinimapResolution = new Size2DI { X = 16, Y = 16 }, Resolution = new Size2DI { X = 128, Y = 128 }, Width = 10 };
            joinGame.Options.Raw = true;
            joinGame.Options.Score = true;
            joinGame.Options.ShowCloaked = true;
            joinGame.Options.ShowBurrowedShadows = true;
            joinGame.Options.RawCropToPlayableArea = true;
            joinGame.Options.RawAffectsSelection = true;

            var request = new Request();
            request.JoinGame = joinGame;

            var response = await Proxy.SendRequest(request);
            return response.JoinGame.PlayerId;
        }

        public async Task<ResponsePing> Ping()
        {
            var request = new Request();
            request.Ping = new RequestPing();
            var response = await Proxy.SendRequest(request);
            return response.Ping;
        }

        public async Task RequestLeaveGame()
        {
            await Proxy.SendRequest(new Request { LeaveGame = new RequestLeaveGame() });
        }

        public async Task SendRequest(Request request)
        {
            await Proxy.SendRequest(request);
        }

        public async Task<ResponseQuery> SendQuery(RequestQuery query)
        {
            var response = await Proxy.SendRequest(new Request { Query = query });
            return response.Query;
        }

        public async Task Run(ISharkyBot bot, uint playerId, string opponentID)
        {
            var gameInfoReq = new Request();
            gameInfoReq.GameInfo = new RequestGameInfo();

            var gameInfoResponse = await Proxy.SendRequest(gameInfoReq);

            var gameDataRequest = new Request();
            gameDataRequest.Data = new RequestData();
            gameDataRequest.Data.UnitTypeId = true;
            gameDataRequest.Data.AbilityId = true;
            gameDataRequest.Data.BuffId = true;
            gameDataRequest.Data.EffectId = true;
            gameDataRequest.Data.UpgradeId = true;

            var dataResponse = await Proxy.SendRequest(gameDataRequest);

            var pingResponse = await Ping();

            var start = true;

            var observationRequest = new Request
            {
                Observation = new RequestObservation()
            };

            var stepRequest = new Request
            {
                Step = new RequestStep { Count = 1 }
            };

            double totalTime = 0;
            int frames = 0;

            double specificTime = 0;
            int actionCount = 0;

            while (true)
            {
                var beginTotal = DateTime.UtcNow;

                if (!start)
                {
                    await Proxy.SendRequest(stepRequest);
                }
                var begin = DateTime.UtcNow;
                var response = await Proxy.SendRequest(observationRequest);

                specificTime += (DateTime.UtcNow - begin).TotalMilliseconds;

                var observation = response.Observation;

                if (observation == null)
                {
                    bot.OnEnd(observation, Result.Undecided);
                    break;
                }
                if (response.Status == Status.Ended || response.Status == Status.Quit)
                {
                    bot.OnEnd(observation, observation.PlayerResult[(int)playerId - 1].Result);
                    break;
                }

                if (start)
                {
                    start = false;
                    bot.OnStart(gameInfoResponse.GameInfo, dataResponse.Data, pingResponse, observation, playerId, opponentID);
                }

                var actions = bot.OnFrame(observation);

                var generatedActions = actions.Count();
                actions = actions.Where(action => action?.ActionRaw?.UnitCommand?.UnitTags == null ||
                    (action?.ActionRaw?.UnitCommand?.UnitTags != null && 
                    !action.ActionRaw.UnitCommand.UnitTags.Any(tag => !observation.Observation.RawData.Units.Any(u => u.Tag == tag))));
                var removedActions = generatedActions - actions.Count();
                if (removedActions > 0)
                {
                    // Console.WriteLine($"Removed {removedActions} actions for units that are not controllable");
                }

                var filteredActions = new List<SC2APIProtocol.Action>();
                var tags = new List<ulong>();
                foreach (var action in actions)
                {
                    if (action?.ActionRaw?.UnitCommand?.UnitTags != null && !action.ActionRaw.UnitCommand.QueueCommand)
                    {
                        if (!tags.Any(tag => action.ActionRaw.UnitCommand.UnitTags.Any(t => t == tag)))
                        {
                            filteredActions.Add(action);
                            tags.AddRange(action.ActionRaw.UnitCommand.UnitTags);
                        }
                        else
                        {
                            // Console.WriteLine($"{observation.Observation.GameLoop} Removed conflicting order {action.ActionRaw.UnitCommand.AbilityId} for tags {string.Join(" ", action.ActionRaw.UnitCommand.UnitTags)}");
                        }
                    }
                    else
                    {
                        filteredActions.Add(action);
                    }
                }

                var actionRequest = new Request();
                actionRequest.Action = new RequestAction();
                actionRequest.Action.Actions.AddRange(filteredActions);

                if (actionRequest.Action.Actions.Count > 0)
                {
                    await Proxy.SendRequest(actionRequest);
                    actionCount += actionRequest.Action.Actions.Count;
                }

                var frameTotal = (DateTime.UtcNow - beginTotal).TotalMilliseconds;
                totalTime += frameTotal;
                frames++;
            }
        }
        
        public async Task RunSinglePlayer(ISharkyBot bot, string map, Race myRace, Race opponentRace, Difficulty opponentDifficulty, AIBuild aIBuild, int randomSeed = -1, string opponentID = "test")
        {
            readSettings();
            StartSC2Instance(5678);
            await Connect(5678);
            await CreateGame(map, opponentRace, opponentDifficulty, aIBuild, randomSeed);
            var playerId = await JoinGame(myRace);
            await Run(bot, playerId, opponentID);
        }

        public async Task RunLadder(ISharkyBot bot, Race myRace, int gamePort, int startPort, String opponentID)
        {
            await Connect(gamePort);
            var playerId = await JoinGameLadder(myRace, startPort);
            await Run(bot, playerId, opponentID);
        }

        public async Task RunLadder(ISharkyBot bot, Race myRace, string[] args)
        {
            var clargs = new CLArgs(args);
            await RunLadder(bot, myRace, clargs.GamePort, clargs.StartPort, clargs.OpponentID);
        }
    }
}
