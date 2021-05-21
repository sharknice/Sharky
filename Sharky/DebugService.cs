using SC2APIProtocol;

namespace Sharky
{
    public class DebugService
    {
        SharkyOptions SharkyOptions;

        int TextLine;
        Color DefaultColor;

        public Request DrawRequest { get; set; }
        public Request SpawnRequest { get; set; }

        public DebugService(SharkyOptions sharkyOptions)
        {
            SharkyOptions = sharkyOptions;
            DefaultColor = new Color() { R = 255, G = 0, B = 0 };
            ResetDrawRequest();
            ResetSpawnRequest();
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

        public void ResetDrawRequest()
        {
            TextLine = 0;
            DrawRequest = new Request();
            DrawRequest.Debug = new RequestDebug();
            DebugCommand debugCommand = new DebugCommand();
            debugCommand.Draw = new DebugDraw();
            DrawRequest.Debug.Debug.Add(debugCommand);
        }

        public void ResetSpawnRequest()
        {
            TextLine = 0;
            SpawnRequest = new Request();
            SpawnRequest.Debug = new RequestDebug();
            DebugCommand debugCommand = new DebugCommand();
            DrawRequest.Debug.Debug.Add(debugCommand);
        }

        public void SpawnUnit(UnitTypes unitType, Point2D location, int playerId)
        {
            SpawnRequest.Debug.Debug.Add(new DebugCommand()
            {
                CreateUnit = new DebugCreateUnit()
                {
                    Owner = playerId,
                    Pos = location,
                    Quantity = 1,
                    UnitType = (uint)unitType
                }
            });
        }

        public void SpawnUnits(UnitTypes unitType, Point2D location, int playerId, int quantity)
        {
            SpawnRequest.Debug.Debug.Add(new DebugCommand()
            {
                CreateUnit = new DebugCreateUnit()
                {
                    Owner = playerId,
                    Pos = location,
                    Quantity = (uint)quantity,
                    UnitType = (uint)unitType
                }
            });
        }
    }
}
