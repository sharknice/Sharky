using SC2APIProtocol;
using Sharky.Chat;
using Sharky.Managers;
using Sharky.MicroControllers.Protoss;
using Sharky.Pathing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class OracleWorkerHarassTask : MicroTask
    {
        TargetingData TargetingData;
        BaseData BaseData;
        ChatService ChatService;
        MapDataService MapDataService;
        MapData MapData;
        OracleMicroController OracleMicroController;

        bool started { get; set; }
        int DesiredCount { get; set; }
        Point2D Target { get; set; }

        Point2D MidPoint;
        Point2D StagingPoint;
        float WallDistanceSquared;
        bool Left;
        bool Right;
        bool Top;
        bool Bottom;
        bool CheeseChatSent;
        int TargetIndex;

        public OracleWorkerHarassTask(TargetingData targetingData, BaseData baseData, ChatService chatService, MapDataService mapDataService, MapData mapData, OracleMicroController oracleMicroController, int desiredCount = 1, bool enabled = true, float priority = -1f)
        {
            TargetingData = targetingData;
            BaseData = baseData;
            ChatService = chatService;
            MapDataService = mapDataService;
            MapData = mapData;
            OracleMicroController = oracleMicroController;

            DesiredCount = desiredCount;
            Enabled = enabled;
            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
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
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ORACLE)
                    {
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                    }
                    if (UnitCommanders.Count() == DesiredCount)
                    {
                        started = true;
                        return;
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (Target == null)
            {
                Target = TargetingData.EnemyMainBasePoint;
                GetTargetPath(Target);
            }

            foreach (var commander in UnitCommanders)
            {
                var defensivePoint = TargetingData.ForwardDefensePoint;
                var commanderVector = commander.UnitCalculation.Position;
                var targetVector = new Vector2(Target.X, Target.Y);

                if ((commander.UnitCalculation.Unit.Energy > 50 || commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEWEAPON)) && commander.UnitCalculation.EnemiesInRangeOf.Count() == 0 &&
                    (commander.UnitCalculation.EnemiesInRange.Count(e => e.UnitClassifications.Contains(UnitClassification.Worker)) > 1 || (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.Worker)) > 2 && commander.UnitCalculation.NearbyEnemies.Count(e => e.DamageAir && e.Unit.BuildProgress == 1) == 0)))
                {
                    var action = OracleMicroController.HarassWorkers(commander, Target, defensivePoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                    continue;
                }

                if ((commander.UnitCalculation.Unit.Energy > 50 || commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEWEAPON)) && Vector2.DistanceSquared(commanderVector, targetVector) < WallDistanceSquared + 5)
                {
                    if (!CheeseChatSent)
                    {
                        ChatService.SendChatType("OracleHarass-FirstAttack");
                        CheeseChatSent = true;
                    }
                    if (!commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.ORACLEWEAPON) && (commander.UnitCalculation.Unit.Energy < 50 || commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax / 2))
                    {
                        var action = OracleMicroController.NavigateToPoint(commander, StagingPoint, defensivePoint, StagingPoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    var canHarass = CanHarass(commander, Target);
                    if (canHarass)
                    {
                        var action = OracleMicroController.HarassWorkers(commander, Target, defensivePoint, frame); // need to make it attack drones and not a pylon lol
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }     
                    }
                    else
                    {
                        GetNextTarget();

                    }
                    continue;
                }

                if (Vector2.DistanceSquared(commanderVector, targetVector) > Vector2.DistanceSquared(commanderVector, new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y)))
                {
                    var action = OracleMicroController.NavigateToPoint(commander, MidPoint, defensivePoint, MidPoint, frame);
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
                        if (commander.RetreatPathFrame + 20 < frame && Vector2.DistanceSquared(commanderVector, new Vector2(side.X, side.Y)) > 1)
                        {
                            var action = OracleMicroController.NavigateToPoint(commander, side, defensivePoint, side, frame);
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
                        if (commander.RetreatPathFrame + 20 < frame && Vector2.DistanceSquared(commanderVector, new Vector2(side.X, side.Y)) > 1)
                        {
                            var action = OracleMicroController.NavigateToPoint(commander, side, defensivePoint, side, frame);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                            continue;
                        }
                    }
                }

                var navigateAction = OracleMicroController.NavigateToPoint(commander, StagingPoint, defensivePoint, StagingPoint, frame);
                if (navigateAction != null)
                {
                    commands.AddRange(navigateAction);
                }
            }


            return commands;
        }

        bool CanHarass(UnitCommander commander, Point2D target)
        {
            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                return false;
            }
            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.Worker) && (Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) <= 100)) < 1 && MapDataService.SelfVisible(new Point2D { X = target.X, Y = target.Y, }))
            {
                return false;
            }
            if (commander.UnitCalculation.Unit.Shield <= 5 || (commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax && commander.UnitCalculation.NearbyEnemies.Where(e => !e.Attributes.Contains(Attribute.Structure) && e.DamageAir).Sum(e => e.Damage) * 3 > commander.UnitCalculation.Unit.Shield))
            {
                return false;
            }
            return true;
        }

        void GetNextTarget()
        {
            var target = BaseData.BaseLocations.OrderBy(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y))).Skip(TargetIndex + 1).FirstOrDefault();
            if (target == null)
            {
                TargetIndex = 0;
            }
            else
            {
                Target = target.MineralLineLocation;
                TargetIndex++;
                if (TargetIndex > 3)
                {
                    TargetIndex = 0;
                }
                GetTargetPath(Target);
            }
        }

        void GetTargetPath(Point2D target)
        {
            var closeestDistance = target.X - 0;
            Left = true;
            MidPoint = new Point2D { X = 0, Y = (target.Y + TargetingData.ForwardDefensePoint.Y) / 2f };
            StagingPoint = new Point2D { X = 0, Y = target.Y };

            var right = MapData.MapWidth - target.X;
            if (right < closeestDistance)
            {
                closeestDistance = right;
                Left = false;
                Right = true;
                MidPoint = new Point2D { X = MapData.MapWidth, Y = (target.Y + TargetingData.ForwardDefensePoint.Y) / 2f };
                StagingPoint = new Point2D { X = MapData.MapWidth - 0, Y = target.Y };
            }

            var top = target.Y;
            if (top < closeestDistance)
            {
                closeestDistance = top;
                Left = false;
                Right = false;
                Top = true;
                MidPoint = new Point2D { X = (target.X + TargetingData.ForwardDefensePoint.X) / 2f, Y = 0 };
                StagingPoint = new Point2D { X = target.X, Y = 0 };
            }

            var bottom = MapData.MapHeight - target.Y;
            if (bottom < closeestDistance)
            {
                closeestDistance = bottom;
                Left = false;
                Right = false;
                Top = false;
                Bottom = true;
                MidPoint = new Point2D { X = (target.X + TargetingData.ForwardDefensePoint.X) / 2f, Y = MapData.MapHeight };
                StagingPoint = new Point2D { X = target.X, Y = MapData.MapHeight - 0 };
            }

            WallDistanceSquared = closeestDistance * closeestDistance;
        }
    }
}
