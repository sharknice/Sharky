using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class DebugManager : SharkyManager
    {
        GameConnection GameConnection;
        SharkyOptions SharkyOptions;

        Request DrawRequest;

        public DebugManager(GameConnection gameConnection, SharkyOptions sharkyOptions)
        {
            GameConnection = gameConnection;
            SharkyOptions = sharkyOptions;

            ResetDrawRequest();
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            if (SharkyOptions.Debug)
            {
                GameConnection.SendRequest(DrawRequest).Wait();
            }

            ResetDrawRequest();

            return new List<Action>();
        }

        public void DrawLine(Point start, Point end, Color color)
        {
            if (SharkyOptions.Debug)
            {
                DrawRequest.Debug.Debug[0].Draw.Lines.Add(new DebugLine() { Color = color, Line = new Line() { P0 = start, P1 = end } });
            }
        }

        public void DrawSphere(Point point)
        {
            if (SharkyOptions.Debug)
            {
                DrawRequest.Debug.Debug[0].Draw.Spheres.Add(new DebugSphere() { Color = new Color() { R = 255, G = 0, B = 0 }, R = 2, P = point });
            }
        }

        private void ResetDrawRequest()
        {
            DrawRequest = new Request();
            DrawRequest.Debug = new RequestDebug();
            DebugCommand debugCommand = new DebugCommand();
            debugCommand.Draw = new DebugDraw();
            DrawRequest.Debug.Debug.Add(debugCommand);
        }
    }
}
