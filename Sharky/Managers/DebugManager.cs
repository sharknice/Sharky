using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class DebugManager : SharkyManager
    {
        GameConnection GameConnection;
        SharkyOptions SharkyOptions;

        Request DrawRequest;
        int TextLine;

        Color DefaultColor;

        public DebugManager(GameConnection gameConnection, SharkyOptions sharkyOptions)
        {
            GameConnection = gameConnection;
            SharkyOptions = sharkyOptions;

            ResetDrawRequest();

            DefaultColor = new Color() { R = 255, G = 0, B = 0 };
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

        public void DrawSphere(Point point, float radius = 2, Color color = null)
        {
            if (SharkyOptions.Debug)
            {
                if (color == null)
                {
                    color = DefaultColor;
                }
                DrawRequest.Debug.Debug[0].Draw.Spheres.Add(new DebugSphere() { Color = color, R = radius, P = point });
            }
        }

        public void DrawText(string text)
        {
            if (SharkyOptions.Debug)
            {
                DrawText(text, 12, 0.05f, 0.1f + 0.02f * TextLine);
                TextLine++;
            }
        }

        void DrawText(string text, uint size, float x, float y)
        {
            if (SharkyOptions.Debug)
            {
                DrawRequest.Debug.Debug[0].Draw.Text.Add(new DebugText() { Text = text, Size = size, VirtualPos = new Point() { X = x, Y = y } });
            }
        }

        private void ResetDrawRequest()
        {
            TextLine = 0;
            DrawRequest = new Request();
            DrawRequest.Debug = new RequestDebug();
            DebugCommand debugCommand = new DebugCommand();
            debugCommand.Draw = new DebugDraw();
            DrawRequest.Debug.Debug.Add(debugCommand);
        }
    }
}
