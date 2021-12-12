using SC2APIProtocol;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sharky.Managers
{
    public class DebugManager : SharkyManager
    {
        GameConnection GameConnection;
        SharkyOptions SharkyOptions;
        DebugService DebugService;

        public DebugManager(GameConnection gameConnection, SharkyOptions sharkyOptions, DebugService debugService)
        {
            GameConnection = gameConnection;
            SharkyOptions = sharkyOptions;
            DebugService = debugService;
        }

        public override bool NeverSkip { get => true; }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            if (SharkyOptions.Debug)
            {
                ReadCommand(observation.Chat, observation.Observation.RawData.Player.Camera);
                try
                {
                    GameConnection.SendRequest(DebugService.DrawRequest).Wait();
                    GameConnection.SendRequest(DebugService.SpawnRequest).Wait();
                    if (DebugService.Surrender)
                    {
                        GameConnection.SendRequest(new Request { LeaveGame = new RequestLeaveGame() }).Wait();
                    }
                }
                catch(System.Exception e)
                {
                    System.Console.WriteLine($"{e.Message}");
                }
            }

            DebugService.ResetDrawRequest();
            DebugService.ResetSpawnRequest();

            return new List<Action>();
        }

        private void ReadCommand(Google.Protobuf.Collections.RepeatedField<ChatReceived> chatsReceived, Point camera)
        {
            foreach (var chatReceived in chatsReceived)
            {
                var match = Regex.Match(chatReceived.Message.ToLower(), @"spawn (\d+) friendly (.*)");
                if (match.Success)
                {
                    var quantity = match.Groups[1].Value;
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[2].Value, true);
                    DebugService.SpawnUnits(unitType, new Point2D { X = camera.X, Y = camera.Y }, (int)chatReceived.PlayerId, int.Parse(quantity));
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), @"spawn (\d+) enemy (.*)");
                if (match.Success)
                {
                    var quantity = match.Groups[1].Value;
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[2].Value, true);
                    var enemyId = 1;
                    if (chatReceived.PlayerId == 1)
                    {
                        enemyId = 2;
                    }
                    DebugService.SpawnUnits(unitType, new Point2D { X = camera.X, Y = camera.Y }, enemyId, int.Parse(quantity));
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "spawn friendly (.*)");
                if (match.Success)
                {
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[1].Value, true);
                    DebugService.SpawnUnit(unitType, new Point2D { X = camera.X, Y = camera.Y }, (int)chatReceived.PlayerId);                 
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "spawn enemy (.*)");
                if (match.Success)
                {
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[1].Value, true);
                    var enemyId = 1;
                    if (chatReceived.PlayerId == 1)
                    {
                        enemyId = 2;
                    }
                    DebugService.SpawnUnit(unitType, new Point2D { X = camera.X, Y = camera.Y }, enemyId);
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), @"camera (\d+\.?\d*) (\d+\.?\d*)");
                if (match.Success)
                {
                    var x = float.Parse(match.Groups[1].Value);
                    var y = float.Parse(match.Groups[2].Value);
                    DebugService.SetCamera(new Point { X = x, Y = y, Z = camera.Z });
                    return;
                }
            }
        }
    }
}
