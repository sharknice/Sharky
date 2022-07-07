using SC2APIProtocol;
using System.Linq;

namespace Sharky
{
    public class DebugService
    {
        SharkyOptions SharkyOptions;
        ActiveUnitData ActiveUnitData;

        int TextLine;
        Color DefaultColor;

        public Request DrawRequest { get; set; }
        public Request SpawnRequest { get; set; }
        public bool Surrender { get; private set; }

        public DebugService(SharkyOptions sharkyOptions, ActiveUnitData activeUnitData)
        {
            SharkyOptions = sharkyOptions;
            ActiveUnitData = activeUnitData;
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

        public void KillFriendlyUnits(UnitTypes unitType)
        {
            var tags = ActiveUnitData.SelfUnits.Where(u => u.Value.Unit.UnitType == (uint)unitType).Select(u => u.Key);
            if (tags.Any())
            {
                var command = new DebugKillUnit();
                command.Tag.AddRange(tags);
                SpawnRequest.Debug.Debug.Add(new DebugCommand()
                {
                    KillUnit = command,
                });
            }
        }

        public void KillEnemyUnits(UnitTypes unitType)
        {
            var tags = ActiveUnitData.EnemyUnits.Where(u => u.Value.Unit.UnitType == (uint)unitType).Select(u => u.Key);
            if (tags.Any())
            {
                var command = new DebugKillUnit();
                command.Tag.AddRange(tags);
                SpawnRequest.Debug.Debug.Add(new DebugCommand()
                {
                    KillUnit = command,
                });
            }
        }

        public void SetEnergy(ulong unitTag, float value)
        {
            SpawnRequest.Debug.Debug.Add(new DebugCommand()
            {
                UnitValue = new DebugSetUnitValue()
                {
                    UnitTag = unitTag,
                    UnitValue = DebugSetUnitValue.Types.UnitValue.Energy,
                    Value = value
                }
            });
        }

        public void SetCamera(Point location)
        {
            var action = new SC2APIProtocol.Action
            {
                ActionRaw = new ActionRaw
                {
                    CameraMove = new ActionRawCameraMove()
                    {
                        CenterWorldSpace = location
                    }
                }
            };
            SpawnRequest.Action = new RequestAction();
            SpawnRequest.Action.Actions.Add(action);
        }

        public void Quit()
        {
            Surrender = true;
        }
    }
}
