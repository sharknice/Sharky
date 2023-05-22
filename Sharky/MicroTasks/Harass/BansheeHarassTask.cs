using SC2APIProtocol;
using Sharky.Chat;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.MicroControllers;
using Sharky.Pathing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Harass
{
    public class BansheeHarassTask : MicroTask
    {
        BaseData BaseData;
        TargetingData TargetingData;
        MapDataService MapDataService;
        ChatService ChatService;
        DebugService DebugService;
        ActiveUnitData ActiveUnitData;
        MapData MapData;
        SharkyOptions SharkyOptions;
        SharkyUnitData SharkyUnitData;
        MacroData MacroData;
        FrameToTimeConverter FrameToTimeConverter;

        IIndividualMicroController IndividualMicroController;

        public Dictionary<ulong, UnitCalculation> Kills { get; private set; }

        int DesiredCount { get; set; }
        bool started { get; set; }
        bool CheeseChatSent { get; set; }
        bool YoloChatSent { get; set; }

        Point2D Target { get; set; }

        Point2D MidPoint;
        Point2D StagingPoint;
        bool Left;
        bool Right;
        bool Top;
        bool Bottom;
        int TargetIndex;
        int TargetAquisitionFrame;


        // TODO: defend base if near it and enemies attacking
        public BansheeHarassTask(DefaultSharkyBot defaultSharkyBot, IIndividualMicroController microController, int desiredCount = 2, bool enabled = true, float priority = -1f)
        {
            BaseData = defaultSharkyBot.BaseData;
            TargetingData = defaultSharkyBot.TargetingData;
            MapDataService = defaultSharkyBot.MapDataService;
            ChatService = defaultSharkyBot.ChatService;
            DebugService = defaultSharkyBot.DebugService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MapData = defaultSharkyBot.MapData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            MacroData = defaultSharkyBot.MacroData;
            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;
            IndividualMicroController = microController;
            DesiredCount = desiredCount;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();

            Kills = new Dictionary<ulong, UnitCalculation>();
        }

        public override void Enable()
        {
            Enabled = true;
            started = false;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < DesiredCount)
            {
                if (started)
                {
                    Disable();
                    return;
                }

                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BANSHEE)
                    {
                        commander.Value.Claimed = true;
                        commander.Value.UnitRole = UnitRole.Harass;
                        UnitCommanders.Add(commander.Value);

                        if (UnitCommanders.Count() == DesiredCount)
                        {
                            started = true;
                            return;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (Target == null)
            {
                Target = BaseData.EnemyBaseLocations.FirstOrDefault().MiddleMineralLocation;
                GetTargetPath(Target, frame);
            }

            DebugService.DrawSphere(new Point { X = Target.X, Y = Target.Y, Z = 10 }, .5f);

            foreach (var commander in UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BANSHEE))
            {
                var defensivePoint = TargetingData.ForwardDefensePoint;
                var commanderVector = commander.UnitCalculation.Position;
                var targetVector = new Vector2(Target.X, Target.Y);

                if (!CloakedAndUndetected(commander) && commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax)
                {
                    commander.UnitRole = UnitRole.None;
                    commander.Claimed = false;
                    UnitCommanders.Remove(commander);
                    return commands;
                }

                if ((CloakedAndUndetected(commander) || commander.UnitCalculation.EnemiesInRangeOfAvoid.Count(e => e.Unit.UnitType != (uint)UnitTypes.TERRAN_BUNKER) == 0) && commander.UnitCalculation.NearbyEnemies.Count(e => e.FrameLastSeen == frame && e.UnitClassifications.Contains(UnitClassification.Worker)) > 0)
                {
                    if (commander.UnitCalculation.EnemiesInRange.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)) || (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame && e.UnitClassifications.Contains(UnitClassification.Worker)) && (CloakedAndUndetected(commander) || !commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageAir && e.Unit.BuildProgress == 1))))
                    {
                        var action = IndividualMicroController.HarassWorkers(commander, Target, defensivePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        continue;
                    }
                }

                if (CloakedAndUndetected(commander))
                {
                    var buildingUnderAttack = commander.UnitCalculation.NearbyAllies.FirstOrDefault(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && a.NearbyEnemies.Any(e => !e.Unit.IsFlying) && !MapDataService.InEnemyDetection(a.Unit.Pos));
                    if (buildingUnderAttack != null)
                    {
                        var enemyInvader = buildingUnderAttack.NearbyEnemies.FirstOrDefault(e => !e.Unit.IsFlying);
                        var action = IndividualMicroController.Attack(commander, enemyInvader.Position.ToPoint2D(), defensivePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        continue;
                    }
                }

                if (Vector2.DistanceSquared(commanderVector, targetVector) < 25)
                {
                    if (!CheeseChatSent)
                    {
                        ChatService.SendChatType("BansheeHarass-FirstAttack");
                        CheeseChatSent = true;
                    }
                    var canHarass = CanHarass(commander, Target);
                    if (canHarass)
                    {
                        var action = IndividualMicroController.HarassWorkers(commander, Target, defensivePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        continue;
                    }
                    else
                    {
                        GetNextTarget(frame);
                    }
                    continue;
                }

                if (!CloakedAndUndetected(commander) && !commander.UnitCalculation.NearbyAllies.Any() && commander.UnitCalculation.EnemiesInRangeOf.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOENIX))
                {
                    // no escape, just kill as many workers as possible
                    if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))
                    {
                        var action = IndividualMicroController.HarassWorkers(commander, Target, defensivePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        SendYoloChat();
                        continue;
                    }
                }

                if (Vector2.DistanceSquared(commanderVector, targetVector) > Vector2.DistanceSquared(commanderVector, new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y)))
                {
                    var action = IndividualMicroController.NavigateToPoint(commander, MidPoint, defensivePoint, MidPoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }

                if (commander.RetreatPath.Count() < 1 || Vector2.DistanceSquared(commander.RetreatPath.Last(), new Vector2(StagingPoint.X, StagingPoint.Y)) > 9)
                {
                    if (Left || Right)
                    {
                        var side = new Point2D { X = StagingPoint.X, Y = commander.UnitCalculation.Unit.Pos.Y };
                        if (commander.RetreatPathFrame + 2 < frame && Vector2.DistanceSquared(commanderVector, new Vector2(side.X, side.Y)) > 4)
                        {
                            var action = IndividualMicroController.NavigateToPoint(commander, side, defensivePoint, side, frame);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                            continue;
                        }
                    }
                    else if (Top || Bottom)
                    {
                        var side = new Point2D { X = commander.UnitCalculation.Unit.Pos.X, Y = StagingPoint.Y };
                        if (commander.RetreatPathFrame + 2 < frame && Vector2.DistanceSquared(commanderVector, new Vector2(side.X, side.Y)) > 4)
                        {
                            var action = IndividualMicroController.NavigateToPoint(commander, side, defensivePoint, side, frame);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                            continue;
                        }
                    }
                }

                if (commander.UnitCalculation.EnemiesInRangeOfAvoid.Any(e => e.Unit.UnitType != (uint)UnitTypes.TERRAN_BUNKER) && Vector2.DistanceSquared(new Vector2(StagingPoint.X, StagingPoint.Y), commanderVector) < 10)
                {
                    var action = IndividualMicroController.Retreat(commander, defensivePoint, null, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    GetNextTarget(frame);
                    continue;
                }
                var navigateAction = IndividualMicroController.NavigateToPoint(commander, StagingPoint, defensivePoint, StagingPoint, frame);
                if (navigateAction != null)
                {
                    commands.AddRange(navigateAction);
                    if (frame - TargetAquisitionFrame > 60 * SharkyOptions.FramesPerSecond)
                    {
                        GetNextTarget(frame);
                    }
                }
            }


            return commands;
        }

        private void SendYoloChat()
        {
            if (!YoloChatSent)
            {
                ChatService.SendChatType("BansheeHarass-Dying");
                YoloChatSent = true;
            }
        }

        bool CanHarass(UnitCommander commander, Point2D target)
        {
            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.LOCKON))
            {
                return false;
            }
            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                return false;
            }
            if (MapDataService.SelfVisible(new Point2D { X = target.X, Y = target.Y, }) && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker) && (Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) <= 100)))
            {
                return false;
            }
            if (!CloakedAndUndetected(commander) && commander.UnitCalculation.NearbyEnemies.Where(e => !e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && e.DamageAir).Sum(e => e.Damage) * 3 > commander.UnitCalculation.Unit.Health)
            {
                return false;
            }
            return true;
        }

        void GetNextTarget(int frame)
        {
            var target = BaseData.BaseLocations.OrderBy(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y))).Skip(TargetIndex + 1).FirstOrDefault();
            if (ActiveUnitData.SelfUnits.Values.Any(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && u.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit))))
            {
                target = BaseData.EnemyBaseLocations.FirstOrDefault();
            }
            if (target == null)
            {
                TargetIndex = 0;
            }
            else
            {
                Target = target.MiddleMineralLocation;
                TargetIndex++;
                if (TargetIndex > 3)
                {
                    TargetIndex = 0;
                }
                GetTargetPath(Target, frame);
            }
        }

        void GetTargetPath(Point2D target, int frame)
        {
            TargetAquisitionFrame = frame;
            var closestDistance = target.X - 0;
            Left = true;
            MidPoint = new Point2D { X = 0, Y = (target.Y + TargetingData.ForwardDefensePoint.Y) / 2f };
            StagingPoint = new Point2D { X = 0, Y = target.Y };

            var right = MapData.MapWidth - target.X;
            if (right < closestDistance)
            {
                closestDistance = right;
                Left = false;
                Right = true;
                MidPoint = new Point2D { X = MapData.MapWidth, Y = (target.Y + TargetingData.ForwardDefensePoint.Y) / 2f };
                StagingPoint = new Point2D { X = MapData.MapWidth - 0, Y = target.Y };
            }

            var top = target.Y;
            if (top < closestDistance)
            {
                closestDistance = top;
                Left = false;
                Right = false;
                Top = true;
                MidPoint = new Point2D { X = (target.X + TargetingData.ForwardDefensePoint.X) / 2f, Y = 0 };
                StagingPoint = new Point2D { X = target.X, Y = 0 };
            }

            var bottom = MapData.MapHeight - target.Y;
            if (bottom < closestDistance)
            {
                closestDistance = bottom;
                Left = false;
                Right = false;
                Top = false;
                Bottom = true;
                MidPoint = new Point2D { X = (target.X + TargetingData.ForwardDefensePoint.X) / 2f, Y = MapData.MapHeight };
                StagingPoint = new Point2D { X = target.X, Y = MapData.MapHeight - 0 };
            }
        }

        private bool CloakedAndUndetected(UnitCommander commander)
        {
            return (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.BANSHEECLOAK) || (commander.UnitCalculation.Unit.Energy > 50 && SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BANSHEECLOAK))) && !MapDataService.InEnemyDetection(commander.UnitCalculation.Unit.Pos);
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            var deaths = 0;
            var kills = 0;
            foreach (var tag in deadUnits)
            {
                if (UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag) > 0)
                {
                    deaths++;
                }
                else
                {
                    var kill = UnitCommanders.SelectMany(c => c.UnitCalculation.PreviousUnitCalculation.NearbyEnemies.Where(e => Vector2.DistanceSquared(c.UnitCalculation.Position, e.Position) < 50)).FirstOrDefault(e => e.Unit.Tag == tag);
                    if (kill != null && !SharkyUnitData.UndeadTypes.Contains((UnitTypes)kill.Unit.UnitType))
                    {
                        kills++;
                        Kills[tag] = kill;
                    }
                }
            }

            if (deaths > 0)
            {
                Deaths += deaths;
            }
            if (kills > 0 || deaths > 0)
            {
                ReportResults();
            }
        }

        private void ReportResults()
        {
            Console.WriteLine($"{FrameToTimeConverter.GetTime(MacroData.Frame)} BansheeHarass Report: Deaths:{Deaths}, Kills:{Kills.Count()}");
            foreach (var killGroup in Kills.Values.GroupBy(k => k.Unit.UnitType))
            {
                Console.WriteLine($"{(UnitTypes)killGroup.Key}: {killGroup.Count()}");
            }
        }

        public override void PrintReport(int frame)
        {
            ReportResults();
            base.PrintReport(frame);
        }

        public override void Disable()
        {
            ReportResults();
            base.Disable();
        }
    }
}
