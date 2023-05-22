using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.Chat;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.MicroControllers;
using Sharky.MicroControllers.Protoss;
using Sharky.MicroTasks.Attack;
using Sharky.Pathing;
using Sharky.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Proxy
{
    public class WarpPrismElevatorTask : MicroTask
    {
        TargetingData TargetingData;
        AdvancedMicroController MicroController;
        WarpPrismMicroController WarpPrismMicroController;
        ProxyLocationService ProxyLocationService;
        MapDataService MapDataService;
        DebugService DebugService;
        UnitDataService UnitDataService;
        ActiveUnitData ActiveUnitData;
        ChatService ChatService;
        AreaService AreaService;
        TargetingService TargetingService;
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;
        MicroTaskData MicroTaskData;
        EnemyData EnemyData;

        IBuildingPlacement ProtossBuildingPlacement;

        float lastFrameTime;

        public List<DesiredUnitsClaim> DesiredUnitsClaims { get; set; }

        Point2D DefensiveLocation { get; set; }
        Point2D LoadingLocation { get; set; }
        int LoadingLocationHeight { get; set; }
        Point2D DropLocation { get; set; }
        Point2D TargetLocation { get; set; }
        int DropLocationHeight { get; set; }
        float InsideBaseDistanceSquared { get; set; }
        int PickupRangeSquared { get; set; }
        List<Point2D> DropArea { get; set; }
        List<Point2D> AttackArea { get; set; }
        Point2D EnemyRampCenter { get; set; }
        int LastForceFieldFrame { get; set; }

        bool StartElevating { get; set; }
        public bool Completed { get; private set; }
        public bool AttackWithinArea { get; set; }

        public WarpPrismElevatorTask(DefaultSharkyBot defaultSharkyBot, AdvancedMicroController microController, WarpPrismMicroController warpPrismMicroController, List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = true)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            ProxyLocationService = defaultSharkyBot.ProxyLocationService;
            MapDataService = defaultSharkyBot.MapDataService;
            DebugService = defaultSharkyBot.DebugService;
            UnitDataService = defaultSharkyBot.UnitDataService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            AreaService = defaultSharkyBot.AreaService;
            ChatService = defaultSharkyBot.ChatService;
            TargetingService = defaultSharkyBot.TargetingService;
            ProtossBuildingPlacement = defaultSharkyBot.ProtossBuildingPlacement;
            MacroData = defaultSharkyBot.MacroData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            EnemyData = defaultSharkyBot.EnemyData;

            MicroController = microController;
            WarpPrismMicroController = warpPrismMicroController;

            DesiredUnitsClaims = desiredUnitsClaims;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();

            PickupRangeSquared = 25;

            StartElevating = false;
            Completed = false;
            LastForceFieldFrame = 0;
            AttackWithinArea = true;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    var unitType = commander.Value.UnitCalculation.Unit.UnitType;
                    foreach (var desiredUnitClaim in DesiredUnitsClaims)
                    {
                        if ((uint)desiredUnitClaim.UnitType == unitType && UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType) < desiredUnitClaim.Count)
                        {
                            commander.Value.Claimed = true;
                            if (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY)
                            {
                                commander.Value.UnitRole = UnitRole.SaveEnergy;
                            }
                            UnitCommanders.Add(commander.Value);
                        }
                    }
                }
            }

            var probeClaim = DesiredUnitsClaims.FirstOrDefault(c => c.UnitType == UnitTypes.PROTOSS_PROBE);
            if (probeClaim != null && probeClaim.Count > UnitCommanders.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE))
            {
                var commander = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !c.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b))).Where(c => (c.UnitRole == UnitRole.None || c.UnitRole == UnitRole.Minerals) && !c.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))).FirstOrDefault();

                if (commander != null)
                {
                    commander.UnitRole = UnitRole.Proxy;
                    commander.Claimed = true;
                    UnitCommanders.Add(commander);
                    MicroTaskData[typeof(MiningTask).Name].StealUnit(commander);
                    return;
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            SetLocations();

            var actions = new List<SC2APIProtocol.Action>();

            if (lastFrameTime > 5)
            {
                lastFrameTime = 0;
                return actions;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            CheckComplete();

            var warpPrisms = UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING);
            var attackers = UnitCommanders.Where(c => c.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_WARPPRISM && c.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_WARPPRISMPHASING);
            var droppedAttackers = attackers.Where(c => AreaService.InArea(c.UnitCalculation.Unit.Pos, DropArea));
            var droppedSentries = droppedAttackers.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY);
            var droppedProbes = droppedAttackers.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE);
            var unDroppedAttackers = attackers.Where(c => !AreaService.InArea(c.UnitCalculation.Unit.Pos, DropArea));

            if (warpPrisms.Count() > 0)
            {
                foreach (var commander in warpPrisms)
                {
                    var action = OrderWarpPrism(commander, droppedAttackers, unDroppedAttackers, frame);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }

                // move into the loading position
                foreach (var commander in unDroppedAttackers)
                {
                    var action = commander.Order(frame, Abilities.ATTACK, LoadingLocation);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }
            else
            {
                // wait for a warp prism
                var groupPoint = TargetingService.GetArmyPoint(unDroppedAttackers);
                actions.AddRange(MicroController.Retreat(unDroppedAttackers, DefensiveLocation, groupPoint, frame));
            }

            OrderSentries(frame, actions, droppedSentries);
            OrderProbes(frame, actions, droppedProbes);

            var mainAttackers = droppedAttackers.Where(c => c.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_SENTRY && c.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_PROBE);

            if (AttackWithinArea)
            {
                actions.AddRange(MicroController.AttackWithinArea(mainAttackers, AttackArea, TargetLocation, DefensiveLocation, null, frame));
            }
            else
            {
                actions.AddRange(MicroController.Attack(mainAttackers, TargetLocation, DefensiveLocation, null, frame));
            }

            stopwatch.Stop();
            lastFrameTime = stopwatch.ElapsedMilliseconds;
            return actions;
        }

        private void OrderProbes(int frame, List<SC2APIProtocol.Action> actions, IEnumerable<UnitCommander> droppedProbes)
        {
            foreach (var commander in droppedProbes)
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_PYLON) || (commander.LastAbility == Abilities.BUILD_PYLON && commander.LastOrderFrame + 20 > frame)) { continue; }

                if (!commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON))
                {
                    if (MacroData.Minerals >= 100)
                    {
                        var placement = ProtossBuildingPlacement.FindPlacement(DropLocation, UnitTypes.PROTOSS_PYLON, 1, true, 50, true);
                        if (placement != null)
                        {
                            var action = commander.Order(frame, Abilities.BUILD_PYLON, placement);
                            if (action != null)
                            {
                                actions.AddRange(action);
                                continue;
                            }
                        }
                    }
                }

                if (commander.UnitCalculation.EnemiesThreateningDamage.Any())
                {
                    actions.AddRange(MicroController.Retreat(new List<UnitCommander> { commander }, DropLocation, null, frame));
                }
                else
                {
                    var action = commander.Order(frame, Abilities.ATTACK, TargetLocation);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }
        }

        private void OrderSentries(int frame, List<SC2APIProtocol.Action> actions, IEnumerable<UnitCommander> droppedSentries)
        {
            var forceField = ActiveUnitData.NeutralUnits.Values.FirstOrDefault(u => u.Unit.UnitType == (uint)UnitTypes.NEUTRAL_FORCEFIELD && Vector2.DistanceSquared(EnemyRampCenter.ToVector2(), u.Position) < 1);
            var someoneCastingFF = droppedSentries.Any(commander => commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_FORCEFIELD) || (commander.LastAbility == Abilities.EFFECT_FORCEFIELD && commander.LastOrderFrame + 20 > frame));
            foreach (var commander in droppedSentries)
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_FORCEFIELD) || (commander.LastAbility == Abilities.EFFECT_FORCEFIELD && commander.LastOrderFrame + 20 > frame)) { continue; }

                if (forceField == null  && commander.UnitCalculation.Unit.Energy >= 50 && frame - LastForceFieldFrame > 20 && !someoneCastingFF)
                {
                    LastForceFieldFrame = frame;
                    var ffAction = commander.Order(frame, Abilities.EFFECT_FORCEFIELD, EnemyRampCenter);
                    if (ffAction != null)
                    {
                        actions.AddRange(ffAction);
                    }
                    continue;
                }

                if (commander.UnitCalculation.EnemiesThreateningDamage.Any())
                {
                    actions.AddRange(MicroController.Retreat(new List<UnitCommander> { commander }, DropLocation, null, frame));
                }
                else if (Vector2.DistanceSquared(commander.UnitCalculation.Position, EnemyRampCenter.ToVector2()) > 81)
                {
                    var action = commander.Order(frame, Abilities.MOVE, EnemyRampCenter);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
                else
                {
                    var action = commander.Order(frame, Abilities.ATTACK, TargetLocation);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }
        }

        private void CheckComplete()
        {
            if (MapDataService.SelfVisible(TargetLocation) && !ActiveUnitData.EnemyUnits.Any(e => Vector2.DistanceSquared(new Vector2(TargetLocation.X, TargetLocation.Y), e.Value.Position) < 100))
            {
                Disable();
                Completed = true;
                ChatService.SendChatType("WarpPrismElevatorTask-TaskCompleted");
            }
        }

        private List<SC2APIProtocol.Action> OrderWarpPrism(UnitCommander warpPrism, IEnumerable<UnitCommander> droppedAttackers, IEnumerable<UnitCommander> unDroppedAttackers, int frame)
        {
            var readyForPickup = unDroppedAttackers.Where(c => !c.UnitCalculation.Loaded && Vector2.DistanceSquared(c.UnitCalculation.Position, new Vector2(LoadingLocation.X, LoadingLocation.Y)) < 10);
            if (!StartElevating && readyForPickup.Any())
            {
                StartElevating = true;
            }

            if (!StartElevating)
            {
                List<SC2APIProtocol.Action> action = null;
                WarpPrismMicroController.SupportArmy(warpPrism, TargetLocation, DefensiveLocation, null, frame, out action, unDroppedAttackers.Select(c => c.UnitCalculation));
                return action;
            }

            foreach (var pickup in readyForPickup)
            {
                if (warpPrism.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING)
                {
                    return warpPrism.Order(frame, Abilities.MORPH_WARPPRISMTRANSPORTMODE);
                }

                if (warpPrism.UnitCalculation.Unit.CargoSpaceMax - warpPrism.UnitCalculation.Unit.CargoSpaceTaken >= UnitDataService.CargoSize((UnitTypes)pickup.UnitCalculation.Unit.UnitType) && !warpPrism.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.UNLOADALLAT_WARPPRISM))
                {
                    if (Vector2.DistanceSquared(warpPrism.UnitCalculation.Position, new Vector2(LoadingLocation.X, LoadingLocation.Y)) < PickupRangeSquared)
                    {
                        return warpPrism.Order(frame, Abilities.LOAD, null, pickup.UnitCalculation.Unit.Tag);
                    }
                    else
                    {
                        return warpPrism.Order(frame, Abilities.MOVE, LoadingLocation);
                    }
                }
            }

            if (warpPrism.UnitCalculation.Unit.Passengers.Count() > 0)
            {
                if (AreaService.InArea(warpPrism.UnitCalculation.Unit.Pos, DropArea))
                {
                    return warpPrism.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, warpPrism.UnitCalculation.Unit.Tag);
                }
                else
                {
                    return warpPrism.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, DropLocation);
                }
            }

            if (warpPrism.UnitCalculation.EnemiesThreateningDamage.Any() || warpPrism.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_VIKINGFIGHTER))
            {
                return WarpPrismMicroController.Retreat(warpPrism, DefensiveLocation, null, frame);
            }

            if (droppedAttackers.Count() > 0)
            {
                List<SC2APIProtocol.Action> action = null;
                WarpPrismMicroController.SupportArmy(warpPrism, TargetLocation, DropLocation, null, frame, out action, droppedAttackers.Select(c => c.UnitCalculation));
                return action;
            }

            if (unDroppedAttackers.Count() > 0)
            {
                List<SC2APIProtocol.Action> action = null;
                WarpPrismMicroController.SupportArmy(warpPrism, TargetLocation, DefensiveLocation, null, frame, out action, unDroppedAttackers.Select(c => c.UnitCalculation));
                return action;
            }

            return warpPrism.Order(frame, Abilities.MOVE, DefensiveLocation);
        }

        private void SetLocations()
        {
            if (DefensiveLocation == null)
            {
                DefensiveLocation = ProxyLocationService.GetCliffProxyLocation();
                TargetLocation = TargetingData.EnemyMainBasePoint;
                DropArea = AreaService.GetTargetArea(TargetLocation);

                var angle = Math.Atan2(TargetLocation.Y - DefensiveLocation.Y, DefensiveLocation.X - TargetLocation.X);
                var x = -6 * Math.Cos(angle);
                var y = -6 * Math.Sin(angle);
                LoadingLocation = new Point2D { X = DefensiveLocation.X + (float)x, Y = DefensiveLocation.Y - (float)y };
                LoadingLocationHeight = MapDataService.MapHeight(LoadingLocation);

                x = -7 * Math.Cos(angle);
                y = -7 * Math.Sin(angle);
                var loadingVector = new Vector2(LoadingLocation.X + (float)x, LoadingLocation.Y - (float)y);

                var dropVector = DropArea.OrderBy(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), loadingVector)).First();
                x = -2 * Math.Cos(angle);
                y = -2 * Math.Sin(angle);
                DropLocation = new Point2D { X = dropVector.X + (float)x, Y = dropVector.Y - (float)y };
                DropLocationHeight = MapDataService.MapHeight(DropLocation);

                InsideBaseDistanceSquared = Vector2.DistanceSquared(new Vector2(LoadingLocation.X, LoadingLocation.Y), new Vector2(TargetLocation.X, TargetLocation.Y));

                var wallData = MapDataService.MapData.WallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.EnemyMainBasePoint.X && b.BasePosition.Y == TargetingData.EnemyMainBasePoint.Y);
                if (wallData?.RampCenter != null)
                {
                    EnemyRampCenter = wallData.RampCenter;
                    var distanceSquaredToAvoid = 50;
                    if (EnemyData.EnemyRace == Race.Terran)
                    {
                        distanceSquaredToAvoid = 144;
                    }
                    AttackArea = DropArea.Where(p => Vector2.DistanceSquared(p.ToVector2(), EnemyRampCenter.ToVector2()) > distanceSquaredToAvoid).ToList();
                }
                else
                {
                    AttackArea = DropArea;
                }
            }
            DebugService.DrawSphere(new Point { X = LoadingLocation.X, Y = LoadingLocation.Y, Z = 12 }, 3, new Color { R = 0, G = 0, B = 255 });
            DebugService.DrawLine(new Point { X = LoadingLocation.X, Y = LoadingLocation.Y, Z = 16 }, new Point { X = LoadingLocation.X, Y = LoadingLocation.Y, Z = 0 }, new Color { R = 0, G = 0, B = 255 });
            DebugService.DrawSphere(new Point { X = DropLocation.X, Y = DropLocation.Y, Z = 12 }, 3, new Color { R = 0, G = 255, B = 0 });
            DebugService.DrawLine(new Point { X = DropLocation.X, Y = DropLocation.Y, Z = 16 }, new Point { X = DropLocation.X, Y = DropLocation.Y, Z = 0 }, new Color { R = 0, G = 255, B = 0 });
        }
    }
}
