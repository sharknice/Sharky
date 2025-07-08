namespace Sharky
{
    public class DebugService
    {
        SharkyOptions SharkyOptions;
        ActiveUnitData ActiveUnitData;
        MacroData MacroData;

        int TextLine;
        public Color DefaultColor;
        public Color DefaultMicroTaskColor;

        public Request DrawRequest { get; set; }
        public Request SpawnRequest { get; set; }
        public bool Surrender { get; set; }

        public Dictionary<ulong, UnitDebugEntry> DebugUnitsInfo = new Dictionary<ulong, UnitDebugEntry>();

        public DebugService(SharkyOptions sharkyOptions, ActiveUnitData activeUnitData, MacroData macroData)
        {
            SharkyOptions = sharkyOptions;
            ActiveUnitData = activeUnitData;
            DefaultColor = new Color() { R = 255, G = 0, B = 0 };
            DefaultMicroTaskColor = new Color() { R = 255, G = 200, B = 150 };
            ResetDrawRequest();
            ResetSpawnRequest();
            MacroData=macroData;
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

        /// <summary>
        /// Draws text to common debug text area
        /// </summary>
        public void DrawText(string text)
        {
            if (SharkyOptions.Debug)
            {
                DrawText(text, 12, 0.05f, 0.1f + 0.02f * TextLine);
                TextLine++;
            }
        }

        /// <summary>
        /// Draws text into the space.
        /// Make sure the altitude of the text is above the terrain.
        /// </summary>
        public void DrawText(string text, Point pos, Color color, uint size = 12)
        {
            if (SharkyOptions.Debug)
            {
                DrawRequest.Debug.Debug[0].Draw.Text.Add(new DebugText() { Size = size, Color = color, Text = text, WorldPos = pos });
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

            // Remove unit debug info older than 5 frames
            DebugUnitsInfo = DebugUnitsInfo.Where(x=>(MacroData.Frame - x.Value.LastFrameUpdate <= 5)).ToDictionary(k => k.Key, v => v.Value);
        }

        public void ResetSpawnRequest()
        {
            TextLine = 0;
            SpawnRequest = new Request();
            SpawnRequest.Debug = new RequestDebug();
            DebugCommand debugCommand = new DebugCommand();
            DrawRequest.Debug.Debug.Add(debugCommand);
        }

        public void DebugUnitText(UnitCalculation unitCalculation, string text, Color color, uint size = 11)
        {
            DebugUnitsInfo[unitCalculation.Unit.Tag] = new UnitDebugEntry(MacroData.Frame, text, unitCalculation, color, size);
        }

        public void DrawUnitInfo()
        {
            foreach (var unit in DebugUnitsInfo)
            {
                unit.Value.Draw(this);
            }
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

        public void KillFriendlyUnits(UnitTypes unitType, int quantity)
        {
            var tags = ActiveUnitData.SelfUnits.Where(u => u.Value.Unit.UnitType == (uint)unitType).OrderByDescending(u => u.Value.Unit.IsOnScreen).ThenBy(u => u.Value.Unit.BuildProgress).Take(quantity).Select(u => u.Key);
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

        public void KillAllFriendlyUnits()
        {
            var tags = ActiveUnitData.SelfUnits.Select(u => u.Key);
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

        public void KillAllEnemyUnits()
        {
            var tags = ActiveUnitData.EnemyUnits.Select(u => u.Key);
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

        public void KillAlllUnits()
        {
            KillAllEnemyUnits();
            KillAllFriendlyUnits();
        }

        public void RemoveUnit(ulong tag)
        {
            var command = new DebugKillUnit();
            command.Tag.Add(tag);
            SpawnRequest.Debug.Debug.Add(new DebugCommand()
            {
                KillUnit = command,
            });          
        }

        public void KillCritters()
        {
            var tags = ActiveUnitData.NeutralUnits.Where(u => u.Value.Unit.Health == 10).Select(u => u.Key);
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

        public void DebugUnits()
        {
            SharkyOptions.DebugMicroTaskUnits = !SharkyOptions.DebugMicroTaskUnits;
        }

        public void DebugCreep()
        {
            SharkyOptions.DebugCreep = !SharkyOptions.DebugCreep;
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
