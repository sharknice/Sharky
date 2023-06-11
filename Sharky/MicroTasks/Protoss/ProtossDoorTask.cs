using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.Chat;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.Pathing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
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
        protected MicroTaskData MicroTaskData;

        protected bool GotWallData;
        protected WallData WallData;
        int BasesDuringWallData;
        protected Point2D DoorSpot;
        protected Point2D ProbeSpot;

        protected bool BlockedChatSent;

        bool NeedProbe;
        bool NeedDoor;
        bool NeedBackupDoor;
        bool NeedAdept;

        bool DestroyWall;
        bool DestroyBlock;
        int BasesCountDuringBlock;

        int LastWallKillAquireFrame;

        UnitCommander DoorCommander;
        UnitCommander BackupDoorCommander;
        UnitCommander ProbeCommander;
        UnitCommander AdeptCommander;
        UnitCommander AdeptShadeCommander;

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
            MicroTaskData = defaultSharkyBot.MicroTaskData;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;

            GotWallData = false;
            PlacementPoints = new List<SC2APIProtocol.Point2D>();
            BlockedChatSent = false;

            NeedProbe = false;
            NeedDoor = false;
            NeedBackupDoor = false;
            DestroyWall = false;
            DestroyBlock = false;
            BasesDuringWallData = 1;
            LastWallKillAquireFrame = 0;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (DoorSpot != null)
            {
                var vector = new Vector2(DoorSpot.X, DoorSpot.Y);
                
                if (NeedAdept && AdeptCommander == null)
                {
                    foreach (var commander in commanders.Where(c => !c.Value.Claimed && c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT).OrderBy(u => Vector2.DistanceSquared(vector, u.Value.UnitCalculation.Position)))
                    {
                        commander.Value.UnitRole = UnitRole.Door;
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                        AdeptCommander = commander.Value;
                        Console.WriteLine("added door adept as needed");
                        UpdateNeeds();
                        break;
                    }
                }
                if (NeedAdept && AdeptShadeCommander == null)
                {
                    foreach (var commander in commanders.Where(c => !c.Value.Claimed && c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT).OrderBy(u => Vector2.DistanceSquared(vector, u.Value.UnitCalculation.Position)).Where(u => Vector2.DistanceSquared(vector, u.Value.UnitCalculation.Position) < 64))
                    {
                        commander.Value.UnitRole = UnitRole.Door;
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                        AdeptShadeCommander = commander.Value;
                        Console.WriteLine("added door adept shade as needed");
                        UpdateNeeds();
                        break;
                    }
                }
                if (NeedDoor && (DoorCommander == null && AdeptCommander == null))
                {
                    foreach (var commander in commanders.Where(c => !c.Value.Claimed && (c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_STALKER || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ZEALOT || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT)).OrderBy(u => Vector2.DistanceSquared(vector, u.Value.UnitCalculation.Position)))
                    {
                        commander.Value.UnitRole = UnitRole.Door;
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                        DoorCommander = commander.Value;
                        Console.WriteLine("added door as needed");
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
                        Console.WriteLine("added backup door as needed");
                        break;
                    }
                }
            }

            if (NeedProbe && ProbeCommander == null && ProbeSpot != null)
            {
                var vector = new Vector2(ProbeSpot.X, ProbeSpot.Y);
                foreach (var commander in commanders.Where(c => c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE).OrderBy(c => c.Value.Claimed).ThenBy(c => c.Value.UnitCalculation.Unit.BuffIds.Count()).ThenBy(u => Vector2.DistanceSquared(vector, u.Value.UnitCalculation.Position)))
                {
                    if (commander.Value.UnitRole != UnitRole.Gas && (!commander.Value.Claimed || commander.Value.UnitRole == UnitRole.Minerals) && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !commander.Value.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)) && commander.Value.UnitRole != UnitRole.Build)
                    {
                        if (Vector2.DistanceSquared(commander.Value.UnitCalculation.Position, new Vector2(ProbeSpot.X, ProbeSpot.Y)) < 400)
                        {
                            commander.Value.UnitRole = UnitRole.Door;
                            commander.Value.Claimed = true;
                            UnitCommanders.Add(commander.Value);
                            ProbeCommander = commander.Value;
                            Console.WriteLine("added probe door as needed");
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
            if (WallData == null) { return commands; }

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

                if (AdeptCommander != null)
                {
                    commands.AddRange(PerformDoorActions(frame, AdeptCommander));
                }

                if (AdeptShadeCommander != null)
                {
                    commands.AddRange(PerformShadeActions(frame, AdeptShadeCommander));
                }

                commands.AddRange(PerformPylonActions(frame));
            }

            var wallToKill = UnitCommanders.FirstOrDefault(c => c.UnitRole == UnitRole.Die);
            if (wallToKill != null)
            {
                foreach (var commander in UnitCommanders.Where(c => c.UnitRole == UnitRole.Attack))
                {
                    commands.AddRange(commander.Order(frame, Abilities.ATTACK, targetTag: wallToKill.UnitCalculation.Unit.Tag));
                }    
            }
            if (ProbeCommander != null)
            {
                commands.AddRange(PerformProbeActions(frame));
            }

            if (DestroyWall)
            {
                commands.AddRange(UpdateDestroyWall(frame));
            }
            else if (DestroyBlock)
            {
                commands.AddRange(UpdateDestroyBlock(frame));
            }
            else
            {
                UpdateWall(frame);
            }

            return commands;
        }

        private void UpdateWall(int frame)
        {
            if (BlockPylon != null && BlockPylon.UnitCalculation.FrameLastSeen == frame)
            {
                if (frame - BlockPylon.UnitCalculation.FrameFirstSeen > SharkyOptions.FramesPerSecond * 60 * 2)
                {
                    if (!BlockPylon.UnitCalculation.NearbyEnemies.Any() || (BlockPylon.UnitCalculation.TargetPriorityCalculation.OverallWinnability > 5 && BlockPylon.UnitCalculation.NearbyAllies.Count(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) > BlockPylon.UnitCalculation.NearbyEnemies.Count(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit))))
                    {
                        BlockPylon.UnitRole = UnitRole.Die;
                    }

                    if (frame - BlockPylon.UnitCalculation.FrameFirstSeen > SharkyOptions.FramesPerSecond * 60 * 5 || MacroData.Minerals > 600)
                    {
                        BlockPylon.UnitRole = UnitRole.Die;
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
                    if (BackupDoorCommander == null)
                    {
                        var vector = new Vector2(WallData.Block.X, WallData.Block.Y);
                        var commander = MicroTaskData[typeof(AttackTask).Name].UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_STALKER || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ZEALOT || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, vector)).FirstOrDefault();
                        if (commander != null)
                        {
                            MicroTaskData[typeof(AttackTask).Name].StealUnit(commander);
                            commander.UnitRole = UnitRole.Door;
                            commander.Claimed = true;
                            UnitCommanders.Add(commander);
                            BackupDoorCommander = commander;
                            Console.WriteLine("added backup door in updateneeds");
                        }
                    }
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
                if ((DoorCommander == null || DoorCommander.UnitCalculation.Unit.Health < DoorCommander.UnitCalculation.Unit.HealthMax) && (BackupDoorCommander == null || BackupDoorCommander.UnitCalculation.Unit.Health < BackupDoorCommander.UnitCalculation.Unit.HealthMax))
                {
                    // TODO: if enemy has tunneling claws, burrowed roach movement, build pylon to block them
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

            if (commander.UnitCalculation.Unit.BuildProgress < 1)
            {
                return commands;
            }

            if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT)
            {
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT && Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < 4))
                {
                    NeedAdept = true;
                    if (commander.AbilityOffCooldown(Abilities.EFFECT_ADEPTPHASESHIFT, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
                    {
                        return commander.Order(frame, Abilities.EFFECT_ADEPTPHASESHIFT, DoorSpot);
                    }
                }
            }

            if (BlockPylon != null && BlockPylon.UnitRole == UnitRole.Die)
            {
                return commander.Order(frame, Abilities.ATTACK, targetTag: BlockPylon.UnitCalculation.Unit.Tag);
            }
            var wallToKill = UnitCommanders.FirstOrDefault(c => c.UnitRole == UnitRole.Die);
            if (wallToKill != null)
            {
                if (!ActiveUnitData.Commanders.ContainsKey(wallToKill.UnitCalculation.Unit.Tag))
                {
                    UnitCommanders.Remove(wallToKill);
                }
                else
                {
                    return commander.Order(frame, Abilities.ATTACK, targetTag: wallToKill.UnitCalculation.Unit.Tag);
                }
            }

            var vector = new Vector2(DoorSpot.X, DoorSpot.Y);
            if (Vector2.DistanceSquared(commander.UnitCalculation.Position, vector) > .25f)
            {
                var doorVector = new Vector2(DoorSpot.X, DoorSpot.Y);
                if (DoorCommander == commander &&commander.UnitCalculation.NearbyEnemies.Any(e => Vector2.DistanceSquared(e.Position, doorVector) < 4))
                {
                    var betterDoor = commander.UnitCalculation.NearbyAllies.Where(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !e.Unit.IsFlying && Vector2.DistanceSquared(e.Position, doorVector) < 1).OrderBy(e => Vector2.DistanceSquared(e.Position, doorVector)).FirstOrDefault();
                    if (betterDoor != null)
                    {
                        var betterDoorCommander = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.Tag == betterDoor.Unit.Tag);
                        if (betterDoorCommander != null && !UnitCommanders.Contains(betterDoorCommander))
                        {
                            commander.Claimed = false;
                            commander.UnitRole = UnitRole.None;
                            UnitCommanders.Remove(commander);
                            betterDoorCommander.Claimed = true;
                            betterDoorCommander.UnitRole = UnitRole.Door;
                            MicroTaskData[typeof(AttackTask).Name].StealUnit(betterDoorCommander);
                            UnitCommanders.Add(betterDoorCommander);
                            DoorCommander = betterDoorCommander;
                            Console.WriteLine("added better door");
                        }
                    }
                }
                return commander.Order(frame, Abilities.MOVE, DoorSpot);
            }
            else if (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen > frame - 5 && Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < 4))
            {
                var changeling = commander.UnitCalculation.EnemiesInRange.FirstOrDefault(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_CHANGELINGZEALOT);
                if (changeling != null)
                {
                    return commander.Order(frame, Abilities.ATTACK, targetTag: changeling.Unit.Tag);
                }
                return commander.Order(frame, Abilities.HOLDPOSITION);
            }
            else if (commander.UnitCalculation.NearbyEnemies.Any())
            {
                var changeling = commander.UnitCalculation.EnemiesInRange.FirstOrDefault(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_CHANGELINGZEALOT);
                if (changeling != null)
                {
                    return commander.Order(frame, Abilities.ATTACK, targetTag: changeling.Unit.Tag);
                }
                return commander.Order(frame, Abilities.ATTACK, DoorSpot);
            }
            else
            {
                return commander.Order(frame, Abilities.MOVE, DoorSpot);
            }
            // TODO: use an individualmcirocontroller so it targets the correct units

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
                        Console.WriteLine("ProtossDoorTask: Canceling Blocking Pylon");
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
                    if (MacroData.Minerals >= 100 && ProbeCommander.UnitCalculation.NearbyEnemies.Any(e => (e.Unit.UnitType == (uint)UnitTypes.ZERG_ROACH || e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING) && e.FrameLastSeen > frame - 50 && Vector2.DistanceSquared(WallData.Block.ToVector2(), e.Position) < 25))
                    {
                        Console.WriteLine("ProtossDoorTask: Blocking Wall with pylon");
                        var probeCommand = ProbeCommander.Order(frame, Abilities.BUILD_PYLON, WallData.Block);
                        if (probeCommand != null)
                        {
                            commands.AddRange(probeCommand);
                        }

                        return commands;
                    }
                    else
                    {
                        var spot = ProbeSpot;
                        if (MacroData.Minerals < 100)
                        {
                            spot = WallData.Block;
                        }
                        var probeCommand = ProbeCommander.Order(frame, Abilities.MOVE, spot);
                        if (probeCommand != null)
                        {
                            commands.AddRange(probeCommand);
                        }

                        return commands;
                    }
                }
            }
            return commands;
        }

        List<SC2APIProtocol.Action> PerformPylonActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (WallData == null || WallData.Block == null || WallData.Pylons == null)
            {
                return commands;
            }

            var pylonPosition = WallData.Pylons.FirstOrDefault();
            if (pylonPosition != null)
            {
                BlockPylon = ActiveUnitData.Commanders.FirstOrDefault(u => u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && u.Value.UnitCalculation.Unit.Pos.X == WallData.Block.X && u.Value.UnitCalculation.Unit.Pos.Y == WallData.Block.Y).Value;
                if (BlockPylon != null)
                {
                    if (BlockPylon.UnitCalculation.Unit.BuildProgress < 1 && BlockPylon.UnitCalculation.Unit.BuildProgress > .95f && BlockPylon.UnitCalculation.EnemiesInRangeOf.Count() < 2)
                    {
                        Console.WriteLine("ProtossDoorTask: Canceling Blocking Pylon");
                        var cancelCommand = BlockPylon.Order(frame, Abilities.CANCEL);
                        if (cancelCommand != null)
                        {
                            commands.AddRange(cancelCommand);
                        }

                        return commands;
                    }
                }
            }
            return commands;
        }

        List<SC2APIProtocol.Action> PerformShadeActions(int frame, UnitCommander commander)
        {
            var vector = new Vector2(DoorSpot.X, DoorSpot.Y);
            if (Vector2.DistanceSquared(commander.UnitCalculation.Position, vector) > .25f)
            {
                return commander.Order(frame, Abilities.MOVE, DoorSpot);
            }
            else if (commander.UnitCalculation.NearbyEnemies.Any(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < 4))
            {
                return commander.Order(frame, Abilities.HOLDPOSITION);
            }
            else
            {
                return commander.Order(frame, Abilities.MOVE, DoorSpot);
            }
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
                if (AdeptCommander != null && AdeptCommander.UnitCalculation.Unit.Tag == tag)
                {
                    AdeptCommander = null;
                }
                if (AdeptShadeCommander != null && AdeptShadeCommander.UnitCalculation.Unit.Tag == tag)
                {
                    AdeptShadeCommander = null;
                }
                if (BlockPylon != null && BlockPylon.UnitCalculation.Unit.Tag == tag)
                {
                    BlockPylon = null;
                }
                UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
        }

        void StopDestroyingWall()
        {
            DestroyWall = false;
            foreach (var commander in ActiveUnitData.Commanders.Where(u => u.Value.UnitRole == UnitRole.Die))
            {
                commander.Value.UnitRole = UnitRole.None;
            }
            var commanders = UnitCommanders.Where(c => c.UnitRole == UnitRole.Attack);
            var ids = commanders.ToList().Select(c => c.UnitCalculation.Unit.Tag);
            foreach (var commander in commanders)
            {
                commander.UnitRole = UnitRole.None;
                commander.Claimed = false;
            }  
            UnitCommanders.RemoveAll(c => ids.Contains(c.UnitCalculation.Unit.Tag));
        }

        IEnumerable<SC2APIProtocol.Action> UpdateDestroyBlock(int frame)
        {
            if (!ActiveUnitData.Commanders.Any(u => u.Value.UnitRole == UnitRole.Die))
            {
                DestroyBlock = false;
                StopDestroyingWall();
                return new List<SC2APIProtocol.Action>();
            }

            if (UnitCommanders.Count(c => c.UnitRole == UnitRole.Attack) < 5 && WallData != null)
            {
                var vector = new Vector2(WallData.Block.X, WallData.Block.Y);
                var commanders = MicroTaskData[typeof(AttackTask).Name].UnitCommanders.Where(c => c.UnitCalculation.DamageGround).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, vector)).Take(5);
                foreach (var commander in commanders)
                {
                    commander.UnitRole = UnitRole.Attack;
                    MicroTaskData[typeof(AttackTask).Name].StealUnit(commander);
                    UnitCommanders.Add(commander);
                    Console.WriteLine("added destroyer for door");
                }
            }

            return new List<SC2APIProtocol.Action>();
        }

        IEnumerable<SC2APIProtocol.Action> UpdateDestroyWall(int frame)
        {
            if (!DestroyWall) { return new List<SC2APIProtocol.Action>(); }

            if (BasesCountDuringBlock != BaseData.SelfBases.Count())
            {
                StopDestroyingWall();
                var actions = new List<SC2APIProtocol.Action>();
                foreach (var commander in UnitCommanders.Where(c => c.UnitRole == UnitRole.Attack))
                {
                    actions.AddRange(commander.Order(frame, Abilities.STOP));
                }
                return actions;
            }

            if (!ActiveUnitData.Commanders.Any(u => u.Value.UnitRole == UnitRole.Die))
            {
                if (frame - LastWallKillAquireFrame > SharkyOptions.FramesPerSecond * 15)
                {
                    MarkNextBuildingForDeath(frame);
                }
            }
            
            if (UnitCommanders.Count(c => c.UnitRole == UnitRole.Attack) < 5 && WallData != null)
            {
                var vector = new Vector2(WallData.Block.X, WallData.Block.Y);
                var commanders = MicroTaskData[typeof(AttackTask).Name].UnitCommanders.Where(c => c.UnitCalculation.DamageGround).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, vector)).Take(5);
                foreach (var commander in commanders)
                {
                    commander.UnitRole = UnitRole.Attack;
                    MicroTaskData[typeof(AttackTask).Name].StealUnit(commander);
                    UnitCommanders.Add(commander);
                    Console.WriteLine("added destoyer door");
                }
            }

            return new List<SC2APIProtocol.Action>();
        }

        public void UnblockWall()
        {
            if (!DestroyWall)
            {
                DestroyWall = true;
                BasesCountDuringBlock = BaseData.SelfBases.Count();

                if (BaseData.SelfBases.Count() <= 2)
                {
                    MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.PROTOSS_SHIELDBATTERY] = 0;
                    MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.PROTOSS_PHOTONCANNON] = 0;
                }

                MarkNextBuildingForDeath(MacroData.Frame);
            }
        }

        public void DestroyBlocker()
        {
            if (DestroyBlock) { return; }

            DestroyBlock = true;
            GetWallData();
            if (WallData == null) { return; }
            var vector = new Vector2(WallData.Block.X, WallData.Block.Y);
            var block = ActiveUnitData.Commanders.Values.FirstOrDefault(u => (u.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON || u.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY) && u.UnitCalculation.Position.X == WallData.Block.X && u.UnitCalculation.Position.Y == WallData.Block.Y);
            if (block != null)
            {
                block.UnitRole = UnitRole.Die;
                UnitCommanders.Add(block);
            }
        }

        void MarkNextBuildingForDeath(int frame)
        {
            LastWallKillAquireFrame = frame;
            GetWallData();
            if (WallData == null) { return; }
            var vector = new Vector2(WallData.Block.X, WallData.Block.Y);
            var closestShieldBatteryOrPylon = ActiveUnitData.Commanders.Values.Where(u => (u.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON || u.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY) && !WallData.Pylons.Any(p => u.UnitCalculation.Position.X == p.X && u.UnitCalculation.Position.Y == p.Y)).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, vector)).FirstOrDefault();
            if (closestShieldBatteryOrPylon != null && Vector2.DistanceSquared(closestShieldBatteryOrPylon.UnitCalculation.Position, vector) < 16)
            {
                closestShieldBatteryOrPylon.UnitRole = UnitRole.Die;
                UnitCommanders.Add(closestShieldBatteryOrPylon);
            }
            else
            {
                var closestStructure = ActiveUnitData.Commanders.Values.Where(u => u.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && WallData?.Pylons != null && !WallData.Pylons.Any(p => u.UnitCalculation.Position.X == p.X && u.UnitCalculation.Position.Y == p.Y) && WallData?.Production != null && !WallData.Production.Any(p => u.UnitCalculation.Position.X == p.X && u.UnitCalculation.Position.Y == p.Y)).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, vector)).FirstOrDefault();
                if (closestStructure != null)
                {
                    closestStructure.UnitRole = UnitRole.Die;
                    UnitCommanders.Add(closestStructure);
                }
            }
        }

        public void EnableShadeBlock()
        {
            NeedAdept = true;
        }
    }
}
