using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SC2APIProtocol;
using Sharky;

namespace Sharky
{
    public class GameConnectionAsync
    {
        ProtobufProxy proxy = new ProtobufProxy();
        string address = "127.0.0.1";

        string starcraftExe;
        string starcraftDir;

        public GameConnectionAsync()
        { }

        public void StartSC2Instance(int port)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(starcraftExe);
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
                    await proxy.Connect(address, port);
                    return;
                }
                catch (WebSocketException) { }
                Thread.Sleep(2000);
            }
            throw new Exception("Unable to make a connection.");
        }

        public async Task CreateGame(String mapName, Race opponentRace, Difficulty opponentDifficulty)
        {
            RequestCreateGame createGame = new RequestCreateGame();
            createGame.Realtime = false;

            string mapPath = Path.Combine(starcraftDir, "Maps", mapName);
            if (!File.Exists(mapPath))
                throw new Exception("Could not find map at " + mapPath);
            createGame.LocalMap = new LocalMap();
            createGame.LocalMap.MapPath = mapPath;

            PlayerSetup player1 = new PlayerSetup();
            createGame.PlayerSetup.Add(player1);
            player1.Type = PlayerType.Participant;

            PlayerSetup player2 = new PlayerSetup();
            createGame.PlayerSetup.Add(player2);
            player2.Race = opponentRace;
            player2.Type = PlayerType.Computer;
            player2.Difficulty = opponentDifficulty;

            Request request = new Request();
            request.CreateGame = createGame;
            Response response = await proxy.SendRequest(request);
        }

        private void readSettings()
        {
            string myDocuments = Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string executeInfo = Path.Combine(myDocuments, "Starcraft II", "ExecuteInfo.txt");
            if (File.Exists(executeInfo))
            {
                string[] lines = File.ReadAllLines(executeInfo);
                foreach (string line in lines)
                {
                    string argument = line.Substring(line.IndexOf('=') + 1).Trim();
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
            RequestJoinGame joinGame = new RequestJoinGame();
            joinGame.Race = race;

            joinGame.Options = new InterfaceOptions();
            joinGame.Options.Raw = true;
            joinGame.Options.Score = true;
            joinGame.Options.ShowCloaked = true;
            joinGame.Options.RawCropToPlayableArea = true;

            Request request = new Request();
            request.JoinGame = joinGame;
            Response response = await proxy.SendRequest(request);
            return response.JoinGame.PlayerId;
        }

        public async Task<uint> JoinGameLadder(Race race, int startPort)
        {
            RequestJoinGame joinGame = new RequestJoinGame();
            joinGame.Race = race;

            joinGame.SharedPort = startPort + 1;
            joinGame.ServerPorts = new PortSet();
            joinGame.ServerPorts.GamePort = startPort + 2;
            joinGame.ServerPorts.BasePort = startPort + 3;

            joinGame.ClientPorts.Add(new PortSet());
            joinGame.ClientPorts[0].GamePort = startPort + 4;
            joinGame.ClientPorts[0].BasePort = startPort + 5;

            joinGame.Options = new InterfaceOptions();
            joinGame.Options.Raw = true;
            joinGame.Options.Score = true;
            joinGame.Options.ShowCloaked = true;
            joinGame.Options.RawCropToPlayableArea = true;

            Request request = new Request();
            request.JoinGame = joinGame;

            Response response = await proxy.SendRequest(request);
            return response.JoinGame.PlayerId;
        }

        public async Task<ResponsePing> Ping()
        {
            Request request = new Request();
            request.Ping = new RequestPing();
            Response response = await proxy.SendRequest(request);
            return response.Ping;
        }

        public async Task RequestLeaveGame()
        {
            Request requestLeaveGame = new Request();
            requestLeaveGame.LeaveGame = new RequestLeaveGame();
            await proxy.SendRequest(requestLeaveGame);
        }

        public async Task SendRequest(Request request)
        {
            await proxy.SendRequest(request);
        }

        public async Task<ResponseQuery> SendQuery(RequestQuery query)
        {
            Request request = new Request();
            request.Query = query;
            Response response = await proxy.SendRequest(request);
            return response.Query;
        }

        public async Task Run(ISharkyBotAsync bot, uint playerId, string opponentID)
        {
            Request gameInfoReq = new Request();
            gameInfoReq.GameInfo = new RequestGameInfo();

            Response gameInfoResponse = await proxy.SendRequest(gameInfoReq);

            Request gameDataRequest = new Request();
            gameDataRequest.Data = new RequestData();
            gameDataRequest.Data.UnitTypeId = true;
            gameDataRequest.Data.AbilityId = true;
            gameDataRequest.Data.BuffId = true;
            gameDataRequest.Data.EffectId = true;
            gameDataRequest.Data.UpgradeId = true;

            Response dataResponse = await proxy.SendRequest(gameDataRequest);

            ResponsePing pingResponse = await Ping();

            bool start = true;

            IEnumerable<SC2APIProtocol.Action> Actions;


            while (true)
            {
                Request observationRequest = new Request();
                observationRequest.Observation = new RequestObservation();
                Response response = await proxy.SendRequest(observationRequest);

                ResponseObservation observation = response.Observation;

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

                var task = bot.OnFrame(observation); 
                while (!task.IsCompleted)
                {
                    // TODO: if this takes longer than real time frame send blank action request, add the actions when they come in
                    // TODO: keep a list of all the tasks, if there is still a task not processed by the time the next frame, queue that frame to be processed with a flag that says behind, and it will only remove dead untis and not issue new orders
                }

                Request actionRequest = new Request();
                actionRequest.Action = new RequestAction();
                //actionRequest.Action.Actions.AddRange(actions);
                if (actionRequest.Action.Actions.Count > 0)
                {
                    await proxy.SendRequest(actionRequest);
                }

                Request stepRequest = new Request();
                stepRequest.Step = new RequestStep();
                stepRequest.Step.Count = 1;
                await proxy.SendRequest(stepRequest);
            }
        }

        public async Task RunSinglePlayer(ISharkyBotAsync bot, string map, Race myRace, Race opponentRace, Difficulty opponentDifficulty)
        {
            readSettings();
            StartSC2Instance(5678);
            await Connect(5678);
            await CreateGame(map, opponentRace, opponentDifficulty);
            uint playerId = await JoinGame(myRace);
            await Run(bot, playerId, "test");
        }

        public async Task RunLadder(ISharkyBotAsync bot, Race myRace, int gamePort, int startPort, String opponentID)
        {
            await Connect(gamePort);
            uint playerId = await JoinGameLadder(myRace, startPort);
            await Run(bot, playerId, opponentID);
        }

        public async Task RunLadder(ISharkyBotAsync bot, Race myRace, string[] args)
        {
            CLArgs clargs = new CLArgs(args);
            await RunLadder(bot, myRace, clargs.GamePort, clargs.StartPort, clargs.OpponentID);
        }
    }
}
