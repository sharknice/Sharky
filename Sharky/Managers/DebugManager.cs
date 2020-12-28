using SC2APIProtocol;
using System.Collections.Generic;

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

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            if (SharkyOptions.Debug)
            {
                GameConnection.SendRequest(DebugService.DrawRequest).Wait();
            }

            DebugService.ResetDrawRequest();

            return new List<Action>();
        }
    }
}
