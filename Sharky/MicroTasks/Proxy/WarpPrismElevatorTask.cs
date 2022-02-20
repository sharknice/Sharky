using SC2APIProtocol;
using Sharky.Chat;
using Sharky.DefaultBot;
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
        IMicroController MicroController;
        WarpPrismMicroController WarpPrismMicroController;
        ProxyLocationService ProxyLocationService;
        MapDataService MapDataService;
        DebugService DebugService;
        UnitDataService UnitDataService;
        ActiveUnitData ActiveUnitData;
        ChatService ChatService;
        AreaService AreaService;
        TargetingService TargetingService;

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

        public WarpPrismElevatorTask(DefaultSharkyBot defaultSharkyBot, IMicroController microController, WarpPrismMicroController warpPrismMicroController, List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = true)
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

            MicroController = microController;
            WarpPrismMicroController = warpPrismMicroController;

            DesiredUnitsClaims = desiredUnitsClaims;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();

            Enabled = true;

            PickupRangeSquared = 25;
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
                            UnitCommanders.Add(commander.Value);
                        }
                    }
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
                //actions.AddRange(MicroController.Retreat(unDroppedAttackers, LoadingLocation, null, frame));
            }
            else
            {
                if (droppedAttackers.Count() > 0)
                {
                    // don't wait for another warp prism, just attack
                    var groupPoint = TargetingService.GetArmyPoint(unDroppedAttackers);
                    actions.AddRange(MicroController.Attack(unDroppedAttackers, TargetLocation, DefensiveLocation, groupPoint, frame));
                }
                else
                {
                    // wait for a warp prism
                    var groupPoint = TargetingService.GetArmyPoint(unDroppedAttackers);
                    actions.AddRange(MicroController.Retreat(unDroppedAttackers, DefensiveLocation, groupPoint, frame));
                }
            }

            actions.AddRange(MicroController.Attack(droppedAttackers, TargetLocation, DefensiveLocation, null, frame));

            stopwatch.Stop();
            lastFrameTime = stopwatch.ElapsedMilliseconds;
            return actions;
        }

        private void CheckComplete()
        {
            if (MapDataService.SelfVisible(TargetLocation) && !ActiveUnitData.EnemyUnits.Any(e => Vector2.DistanceSquared(new Vector2(TargetLocation.X, TargetLocation.Y), e.Value.Position) < 100))
            {
                Disable();
                ChatService.SendChatType("WarpPrismElevatorTask-TaskCompleted");
            }
        }

        private List<SC2APIProtocol.Action> OrderWarpPrism(UnitCommander warpPrism, IEnumerable<UnitCommander> droppedAttackers, IEnumerable<UnitCommander> unDroppedAttackers, int frame)
        {
            var readyForPickup = unDroppedAttackers.Where(c => !c.UnitCalculation.Loaded && Vector2.DistanceSquared(c.UnitCalculation.Position, new Vector2(LoadingLocation.X, LoadingLocation.Y)) < 10);
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

                var angle = Math.Atan2(TargetLocation.Y - DefensiveLocation.Y, DefensiveLocation.X - TargetLocation.X);
                var x = -6 * Math.Cos(angle);
                var y = -6 * Math.Sin(angle);
                LoadingLocation = new Point2D { X = DefensiveLocation.X + (float)x, Y = DefensiveLocation.Y - (float)y };
                LoadingLocationHeight = MapDataService.MapHeight(LoadingLocation);

                var loadingVector = new Vector2(LoadingLocation.X, LoadingLocation.Y);
                DropArea = AreaService.GetTargetArea(TargetLocation);
                var dropVector = DropArea.OrderBy(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), loadingVector)).First();
                x = -2 * Math.Cos(angle);
                y = -2 * Math.Sin(angle);
                DropLocation = new Point2D { X = dropVector.X + (float)x, Y = dropVector.Y - (float)y };
                DropLocationHeight = MapDataService.MapHeight(DropLocation);

                InsideBaseDistanceSquared = Vector2.DistanceSquared(new Vector2(LoadingLocation.X, LoadingLocation.Y), new Vector2(TargetLocation.X, TargetLocation.Y));
            }
            DebugService.DrawSphere(new Point { X = LoadingLocation.X, Y = LoadingLocation.Y, Z = 12 }, 3, new Color { R = 0, G = 0, B = 255 });
            DebugService.DrawSphere(new Point { X = DropLocation.X, Y = DropLocation.Y, Z = 12 }, 3, new Color { R = 0, G = 255, B = 0 });
        }
    }
}
