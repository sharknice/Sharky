namespace Sharky.MicroTasks
{
    public class WallOffTask : MicroTask
    {
        protected SharkyUnitData SharkyUnitData;
        protected ActiveUnitData ActiveUnitData;
        protected MacroData MacroData;
        protected MapData MapData;
        protected WallService WallService;
        protected ChatService ChatService;

        protected bool GotWallData;
        protected WallData WallData;
        protected Point2D ProbeSpot { get; set; }

        protected bool BlockedChatSent;

        public List<SC2APIProtocol.Point2D> PlacementPoints { get; protected set; }

        public WallOffTask(SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, MacroData macroData, MapData mapData, WallService wallService, ChatService chatService, bool enabled, float priority)
        {
            SharkyUnitData = sharkyUnitData;
            Priority = priority;
            ActiveUnitData = activeUnitData;
            MacroData = macroData;
            MapData = mapData;
            WallService = wallService;
            ChatService = chatService;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;

            GotWallData = false;
            PlacementPoints = new List<SC2APIProtocol.Point2D>();
            BlockedChatSent = false;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (!UnitCommanders.Any() && ProbeSpot != null)
            {
                foreach (var commander in commanders.OrderBy(c => c.Value.Claimed).ThenBy(c => c.Value.UnitCalculation.Unit.BuffIds.Count()).ThenBy(c => DistanceToResourceCenter(c)))
                {
                    if (commander.Value.UnitRole != UnitRole.Gas && (!commander.Value.Claimed || commander.Value.UnitRole == UnitRole.Minerals) && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !commander.Value.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)) && commander.Value.UnitRole != UnitRole.Build)
                    {
                        if (Vector2.DistanceSquared(commander.Value.UnitCalculation.Position, new Vector2(ProbeSpot.X, ProbeSpot.Y)) < 400)
                        {
                            commander.Value.UnitRole = UnitRole.Door;
                            commander.Value.Claimed = true;
                            UnitCommanders.Add(commander.Value);
                            return;
                        }
                    }
                }
            }
        }

        protected float DistanceToResourceCenter(KeyValuePair<ulong, UnitCommander> commander)
        {
            var resourceCenter = commander.Value.UnitCalculation.NearbyAllies.FirstOrDefault(a => a.UnitClassifications.Contains(UnitClassification.ResourceCenter));
            if (resourceCenter != null)
            {
                return Vector2.DistanceSquared(commander.Value.UnitCalculation.Position, resourceCenter.Position);
            }
            return 0;
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            GetWallData();

            var commands = new List<SC2APIProtocol.Action>();
            if (WallData == null || WallData.Block == null || WallData.Pylons == null)
            {
                return commands;
            }

            var pylonPosition = WallData.Pylons.FirstOrDefault();
            if (pylonPosition != null)
            {
                var probe = UnitCommanders.FirstOrDefault();
                if (probe == null) { return commands; }
                if (probe.UnitRole != UnitRole.Wall) { probe.UnitRole = UnitRole.Wall; }

                var shieldBattery = ActiveUnitData.Commanders.FirstOrDefault(u => u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && u.Value.UnitCalculation.Unit.Pos.X == WallData.Block.X && u.Value.UnitCalculation.Unit.Pos.Y == WallData.Block.Y).Value;
                if (shieldBattery != null)
                {
                    if (!BlockedChatSent)
                    {
                        ChatService.SendChatType("WallOffTask-TaskCompleted");
                        BlockedChatSent = true;
                    }
                    if (shieldBattery.UnitCalculation.Unit.BuildProgress < 1 && shieldBattery.UnitCalculation.Unit.BuildProgress > .95f && shieldBattery.UnitCalculation.EnemiesInRangeOf.Count() < 2)
                    {
                        var cancelCommand = shieldBattery.Order(frame, Abilities.CANCEL);
                        if (cancelCommand != null)
                        {
                            commands.AddRange(cancelCommand);
                        }

                        return commands;
                    }
                    else
                    {
                        if (Vector2.DistanceSquared(probe.UnitCalculation.Position, new Vector2(ProbeSpot.X, ProbeSpot.Y)) > 36)
                        {
                            var probeCommand = probe.Order(frame, Abilities.MOVE, ProbeSpot);
                            if (probeCommand != null)
                            {
                                commands.AddRange(probeCommand);
                            }

                            return commands;
                        }
                    }
                }
                else if (probe != null)
                {
                    var pylon = ActiveUnitData.SelfUnits.FirstOrDefault(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && u.Value.Unit.Pos.X == pylonPosition.X && u.Value.Unit.Pos.Y == pylonPosition.Y).Value;
                    if (pylon != null)
                    {
                        if (MacroData.Minerals >= 100 && pylon.NearbyEnemies.Any(e => (e.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_ZEALOT || e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING) && e.FrameLastSeen > frame - 50))
                        {
                            var probeCommand = probe.Order(frame, Abilities.BUILD_SHIELDBATTERY, WallData.Block);
                            if (probeCommand != null)
                            {
                                commands.AddRange(probeCommand);
                            }

                            return commands;
                        }
                        else
                        {
                            if (Vector2.DistanceSquared(probe.UnitCalculation.Position, new Vector2(ProbeSpot.X, ProbeSpot.Y)) > 4)
                            {
                                var probeCommand = probe.Order(frame, Abilities.MOVE, ProbeSpot);
                                if (probeCommand != null)
                                {
                                    commands.AddRange(probeCommand);
                                }

                                return commands;
                            }
                        }
                    }
                }
            }

            return commands;
        }

        protected virtual void GetWallData()
        {
            if (!GotWallData)
            {
                GotWallData = true;

                var baseLocation = WallService.GetBaseLocation();
                if (baseLocation == null) { return; }

                WallData = MapData.WallData.FirstOrDefault(b => b.BasePosition.X == baseLocation.X && b.BasePosition.Y == baseLocation.Y);
                if (WallData != null && WallData.Block != null)
                {
                    PlacementPoints.Add(WallData.Block);

                    var angle = System.Math.Atan2(WallData.Block.Y - baseLocation.Y, baseLocation.X - WallData.Block.X);
                    var x = 2 * System.Math.Cos(angle);
                    var y = 2 * System.Math.Sin(angle);
                    ProbeSpot = new Point2D { X = WallData.Block.X + (float)x, Y = WallData.Block.Y - (float)y };
                }
            }
        }
    }
}
