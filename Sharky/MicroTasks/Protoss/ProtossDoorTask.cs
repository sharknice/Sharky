using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.Chat;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    // TODO: add to defaulsharky tasks but disabled
    // TODO: make zealot/stalker/adept/whatever stand in door walladata gap at main wall or expansion wall
    // if unit is getting low get a backup unit, get unit qualified that is closest

    // if going to lose unit, or no unit and no backup unit ready to take spot, build the block pylon

    // enable this against zerg

    /// <summary>
    /// Use a zealot/stalker/adept as a door to block enemy units from coming through the wall
    /// Will use a second backup door if the first one is dying
    /// Will also build a pylon to block the wall if the door unit is dying
    /// </summary>
    public class ProtossDoorTask : MicroTask
    {
        protected SharkyUnitData SharkyUnitData;
        protected ActiveUnitData ActiveUnitData;
        protected MacroData MacroData;
        protected MapData MapData;
        protected WallService WallService;
        protected ChatService ChatService;
        protected BaseData BaseData;
        protected SharkyOptions SharkyOptions;

        protected bool GotWallData;
        protected WallData WallData;
        int BasesDuringWallData;
        protected Point2D DoorSpot;
        protected Point2D ProbeSpot;

        protected bool BlockedChatSent;

        bool NeedProbe;
        bool NeedDoor;
        bool NeedBackupDoor;

        UnitCommander DoorCommander;
        UnitCommander BackupDoorCommander;
        UnitCommander ProbeCommander;

        UnitCommander BlockPylon;

        public List<SC2APIProtocol.Point2D> PlacementPoints { get; protected set; }

        public ProtossDoorTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            Priority = priority;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MacroData = defaultSharkyBot.MacroData;
            MapData = defaultSharkyBot.MapData;
            WallService = defaultSharkyBot.WallService;
            ChatService = defaultSharkyBot.ChatService;
            BaseData = defaultSharkyBot.BaseData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;

            GotWallData = false;
            PlacementPoints = new List<SC2APIProtocol.Point2D>();
            BlockedChatSent = false;

            NeedProbe = false;
            NeedDoor = false;
            NeedBackupDoor = false;
            BasesDuringWallData = 1;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (DoorSpot != null)
            {
                var vector = new Vector2(DoorSpot.X, DoorSpot.Y);
                if (NeedDoor && DoorCommander == null)
                {
                    foreach (var commander in commanders.Where(c => !c.Value.Claimed && (c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_STALKER || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ZEALOT || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT)).OrderBy(u => Vector2.DistanceSquared(vector, u.Value.UnitCalculation.Position)))
                    {
                        commander.Value.UnitRole = UnitRole.Door;
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                        DoorCommander = commander.Value;
                        UpdateNeeds();
                        break;
                    }
                }
                if (NeedBackupDoor && BackupDoorCommander == null)
                {
                    foreach (var commander in commanders.Where(c => !c.Value.Claimed && (c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_STALKER || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ZEALOT || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT)).OrderBy(u => Vector2.DistanceSquared(vector, u.Value.UnitCalculation.Position)))
                    {
                        commander.Value.UnitRole = UnitRole.Door;
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                        BackupDoorCommander = commander.Value;
                        break;
                    }
                }
            }

            if (NeedProbe && ProbeCommander == null && ProbeSpot != null)
            {
                var vector = new Vector2(ProbeSpot.X, ProbeSpot.Y);
                foreach (var commander in commanders.OrderBy(c => c.Value.Claimed).ThenBy(c => c.Value.UnitCalculation.Unit.BuffIds.Count()).ThenBy(u => Vector2.DistanceSquared(vector, u.Value.UnitCalculation.Position)))
                {
                    if (commander.Value.UnitRole != UnitRole.Gas && (!commander.Value.Claimed || commander.Value.UnitRole == UnitRole.Minerals) && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !commander.Value.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)) && commander.Value.UnitRole != UnitRole.Build)
                    {
                        if (Vector2.DistanceSquared(commander.Value.UnitCalculation.Position, new Vector2(ProbeSpot.X, ProbeSpot.Y)) < 400)
                        {
                            commander.Value.UnitRole = UnitRole.Door;
                            commander.Value.Claimed = true;
                            UnitCommanders.Add(commander.Value);
                            ProbeCommander = commander.Value;
                            break;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            GetWallData();
            UpdateNeeds();

            var commands = new List<SC2APIProtocol.Action>();

            if (DoorSpot != null)
            {
                if (DoorCommander != null)
                {
                    commands.AddRange(PerformDoorActions(frame, DoorCommander));
                }

                if (BackupDoorCommander != null)
                {
                    commands.AddRange(PerformDoorActions(frame, BackupDoorCommander));
                }
            }

            if (ProbeCommander != null)
            {
                commands.AddRange(PerformProbeActions(frame));
            }

            UpdateWall(frame);

            return commands;
        }

        private void UpdateWall(int frame)
        {
            // TODO: if wall bock, after 5 minutes destroy it if no ground enemies nearby
            if (BlockPylon != null && BlockPylon.UnitCalculation.FrameLastSeen == frame)
            {
                if (frame - BlockPylon.UnitCalculation.FrameFirstSeen > SharkyOptions.FramesPerSecond * 60 * 2)
                {
                    if (!BlockPylon.UnitCalculation.NearbyEnemies.Any() || (BlockPylon.UnitCalculation.TargetPriorityCalculation.OverallWinnability > 5 && BlockPylon.UnitCalculation.NearbyAllies.Count(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) > BlockPylon.UnitCalculation.NearbyEnemies.Count(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit))))
                    {
                        BlockPylon.UnitRole = UnitRole.Die;
                    }

                    if (frame - BlockPylon.UnitCalculation.FrameFirstSeen > SharkyOptions.FramesPerSecond * 60 * 5)
                    {
                        if (BlockPylon.UnitCalculation.TargetPriorityCalculation.OverallWinnability > 5)
                        {
                            BlockPylon.UnitRole = UnitRole.Die;
                        }
                    }
                }
            }
        }

        private void UpdateNeeds()
        {
            var wallCompleted = false;
            if (WallData?.WallSegments != null && WallData.WallSegments.Count > 0)
            {
                wallCompleted = WallData.WallSegments.Where(w => w.Size == 3).All(w => ActiveUnitData.SelfUnits.Any(u => u.Value.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && u.Value.Position.X == w.Position.X && u.Value.Position.Y == w.Position.Y));
            }

            if (WallData?.Door != null && wallCompleted) 
            {
                NeedDoor = true;
                if (DoorCommander != null && DoorCommander.UnitCalculation.Unit.Health < DoorCommander.UnitCalculation.Unit.HealthMax)
                {
                    NeedBackupDoor = true;
                    // TODO: steal unit from attack task
                }
                else
                {
                    NeedBackupDoor = false;
                    if (BackupDoorCommander != null)
                    {
                        BackupDoorCommander.UnitRole = UnitRole.None;
                        BackupDoorCommander.Claimed = false;
                        UnitCommanders.Remove(BackupDoorCommander);
                        BackupDoorCommander = null;
                    }
                }
            }
            else
            {
                NeedDoor = false;
                if (DoorCommander != null)
                {
                    DoorCommander.UnitRole = UnitRole.None;
                    DoorCommander.Claimed = false;
                    UnitCommanders.Remove(DoorCommander);
                    DoorCommander = null;
                }
            }

            if (WallData?.Block != null && wallCompleted)
            {
                if (DoorCommander == null || DoorCommander.UnitCalculation.Unit.Health < DoorCommander.UnitCalculation.Unit.HealthMax)
                {
                    NeedProbe = true;
                }
                else
                {
                    NeedProbe = false;
                    RemoveProbeCommander();
                }
            }
            else
            {
                NeedProbe = false;
                RemoveProbeCommander();
            }
        }

        private void RemoveProbeCommander()
        {
            if (ProbeCommander != null)
            {
                ProbeCommander.UnitRole = UnitRole.None;
                ProbeCommander.Claimed = false;
                UnitCommanders.Remove(ProbeCommander);
                ProbeCommander = null;
            }
        }

        List<SC2APIProtocol.Action> PerformDoorActions(int frame, UnitCommander commander)
        {
            var commands = new List<SC2APIProtocol.Action>();
            if (BlockPylon != null && BlockPylon.UnitRole == UnitRole.Die)
            {
                return commander.Order(frame, Abilities.ATTACK, targetTag: BlockPylon.UnitCalculation.Unit.Tag);
            }

            var vector = new Vector2(DoorSpot.X, DoorSpot.Y);
            if (Vector2.DistanceSquared(commander.UnitCalculation.Position, vector) > .5f)
            {
                return commander.Order(frame, Abilities.MOVE, DoorSpot);
            }

            if (commander.UnitCalculation.EnemiesInRange.Any())
            {
                // TODO: use an individualmcirocontroller so it targets the correct units
            }

            return commands;
        }

        List<SC2APIProtocol.Action> PerformProbeActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (ProbeCommander == null || WallData == null || WallData.Block == null || WallData.Pylons == null)
            {
                return commands;
            }

            var pylonPosition = WallData.Pylons.FirstOrDefault();
            if (pylonPosition != null)
            {
                if (ProbeCommander.UnitRole != UnitRole.Wall) { ProbeCommander.UnitRole = UnitRole.Wall; }

                BlockPylon = ActiveUnitData.Commanders.FirstOrDefault(u => u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && u.Value.UnitCalculation.Unit.Pos.X == WallData.Block.X && u.Value.UnitCalculation.Unit.Pos.Y == WallData.Block.Y).Value;
                if (BlockPylon != null)
                {

                    if (!BlockedChatSent)
                    {
                        ChatService.SendChatType("ProtossDoorTask-TaskCompleted");
                        BlockedChatSent = true;
                    }
                    if (BlockPylon.UnitCalculation.Unit.BuildProgress < 1 && BlockPylon.UnitCalculation.Unit.BuildProgress > .95f && BlockPylon.UnitCalculation.EnemiesInRangeOf.Count() < 2)
                    {
                        var cancelCommand = BlockPylon.Order(frame, Abilities.CANCEL);
                        if (cancelCommand != null)
                        {
                            commands.AddRange(cancelCommand);
                        }

                        return commands;
                    }
                    else
                    {
                        if (Vector2.DistanceSquared(ProbeCommander.UnitCalculation.Position, new Vector2(ProbeSpot.X, ProbeSpot.Y)) > 36)
                        {
                            var probeCommand = ProbeCommander.Order(frame, Abilities.MOVE, ProbeSpot);
                            if (probeCommand != null)
                            {
                                commands.AddRange(probeCommand);
                            }

                            return commands;
                        }
                    }
                }
                else
                {
                    if (MacroData.Minerals >= 100 && ProbeCommander.UnitCalculation.NearbyEnemies.Any(e => (e.Unit.UnitType == (uint)UnitTypes.ZERG_ROACH || e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING) && e.FrameLastSeen > frame - 50))
                    {
                        var probeCommand = ProbeCommander.Order(frame, Abilities.BUILD_PYLON, WallData.Block);
                        if (probeCommand != null)
                        {
                            commands.AddRange(probeCommand);
                        }

                        return commands;
                    }
                    else
                    {
                        if (Vector2.DistanceSquared(ProbeCommander.UnitCalculation.Position, new Vector2(ProbeSpot.X, ProbeSpot.Y)) > 4)
                        {
                            var probeCommand = ProbeCommander.Order(frame, Abilities.MOVE, ProbeSpot);
                            if (probeCommand != null)
                            {
                                commands.AddRange(probeCommand);
                            }

                            return commands;
                        }
                    }
                }
            }
            return commands;
        }

        protected virtual void GetWallData()
        {
            if (!GotWallData || BasesDuringWallData != BaseData.SelfBases.Count())
            {
                BasesDuringWallData = BaseData.SelfBases.Count();
                GotWallData = true;

                var baseLocation = WallService.GetBaseLocation();
                if (baseLocation == null) { return; }

                WallData = MapData.WallData.FirstOrDefault(b => b.BasePosition.X == baseLocation.X && b.BasePosition.Y == baseLocation.Y);
                if (WallData != null)
                {
                    if (WallData.Door != null)
                    {
                        DoorSpot = WallData.Door;
                    }

                    if (WallData.Block != null)
                    {
                        PlacementPoints.Add(WallData.Door);

                        var angle = System.Math.Atan2(WallData.Block.Y - baseLocation.Y, baseLocation.X - WallData.Block.X);
                        var x = System.Math.Cos(angle);
                        var y = System.Math.Sin(angle);
                        ProbeSpot = new Point2D { X = WallData.Block.X + (float)x, Y = WallData.Block.Y - (float)y };
                    }
                }
            }
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                if (ProbeCommander != null && ProbeCommander.UnitCalculation.Unit.Tag == tag)
                {
                    ProbeCommander = null;
                }
                if (DoorCommander != null && DoorCommander.UnitCalculation.Unit.Tag == tag)
                {
                    DoorCommander = null;
                }
                if (BackupDoorCommander != null && BackupDoorCommander.UnitCalculation.Unit.Tag == tag)
                {
                    BackupDoorCommander = null;
                }
                if (BlockPylon != null && BlockPylon.UnitCalculation.Unit.Tag == tag)
                {
                    BlockPylon = null;
                }
                UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
        }
    }
}
