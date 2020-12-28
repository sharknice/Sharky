using SC2APIProtocol;
using Sharky.Chat;
using Sharky.Managers;
using Sharky.MicroControllers;
using Sharky.MicroControllers.Protoss;
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
        UnitDataManager UnitDataManager;
        ActiveUnitData ActiveUnitData;
        ChatService ChatService;

        float lastFrameTime;

        public List<DesiredUnitsClaim> DesiredUnitsClaims { get; set; }

        Point2D DefensiveLocation { get; set; }
        Point2D LoadingLocation { get; set; }
        int LoadingLocationHeight { get; set; }
        Point2D DropLocation { get; set; }
        Point2D TargetLocatoin { get; set; }
        int DropLocationHeight { get; set; }
        float InsideBaseDistanceSquared { get; set; }
        int PickupRangeSquared { get; set; }

        public WarpPrismElevatorTask(TargetingData targetingData, IMicroController microController, WarpPrismMicroController warpPrismMicroController, ProxyLocationService proxyLocationService, MapDataService mapDataService, DebugService debugService, UnitDataManager unitDataManager, ActiveUnitData activeUnitData, ChatService chatService, List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = true)
        {
            TargetingData = targetingData;
            MicroController = microController;
            WarpPrismMicroController = warpPrismMicroController;
            ProxyLocationService = proxyLocationService;
            MapDataService = mapDataService;
            DebugService = debugService;
            UnitDataManager = unitDataManager;
            ActiveUnitData = activeUnitData;

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
            var droppedAttackers = attackers.Where(c => MapDataService.MapHeight(c.UnitCalculation.Unit.Pos) == DropLocationHeight && Vector2.DistanceSquared(new Vector2(c.UnitCalculation.Unit.Pos.X, c.UnitCalculation.Unit.Pos.Y), new Vector2(TargetLocatoin.X, TargetLocatoin.Y)) >= InsideBaseDistanceSquared);
            var unDroppedAttackers = attackers.Where(c => MapDataService.MapHeight(c.UnitCalculation.Unit.Pos) != DropLocationHeight || Vector2.DistanceSquared(new Vector2(c.UnitCalculation.Unit.Pos.X, c.UnitCalculation.Unit.Pos.Y), new Vector2(TargetLocatoin.X, TargetLocatoin.Y)) < InsideBaseDistanceSquared);

            if (warpPrisms.Count() > 0)
            {
                foreach (var commander in warpPrisms)
                {
                    var action = OrderWarpPrism(commander, droppedAttackers, unDroppedAttackers, frame);
                    if (action != null)
                    {
                        actions.Add(action);
                    }
                }

                // move into the loading position
                foreach (var commander in unDroppedAttackers)
                {
                    var action = commander.Order(frame, Abilities.ATTACK, LoadingLocation);
                    if (action != null)
                    {
                        actions.Add(action);
                    }
                }
                //actions.AddRange(MicroController.Retreat(unDroppedAttackers, LoadingLocation, null, frame));
            }
            else
            {
                if (droppedAttackers.Count() > 0)
                {
                    // don't wait for another warp prism, just attack
                    actions.AddRange(MicroController.Attack(unDroppedAttackers, TargetLocatoin, DefensiveLocation, null, frame));
                }
                else
                {
                    // wait for a warp prism
                    actions.AddRange(MicroController.Retreat(unDroppedAttackers, DefensiveLocation, null, frame));
                }
            }

            actions.AddRange(MicroController.Attack(droppedAttackers, TargetLocatoin, DefensiveLocation, null, frame));

            stopwatch.Stop();
            lastFrameTime = stopwatch.ElapsedMilliseconds;
            return actions;
        }

        private void CheckComplete()
        {
            if (MapDataService.SelfVisible(TargetLocatoin) && !ActiveUnitData.EnemyUnits.Any(e => Vector2.DistanceSquared(new Vector2(TargetLocatoin.X, TargetLocatoin.Y), new Vector2(e.Value.Unit.Pos.X, e.Value.Unit.Pos.Y)) < 100))
            {
                Disable();
                ChatService.SendChatType("WarpPrismElevatorTask-TaskCompleted");
            }
        }

        private SC2APIProtocol.Action OrderWarpPrism(UnitCommander warpPrism, IEnumerable<UnitCommander> droppedAttackers, IEnumerable<UnitCommander> unDroppedAttackers, int frame)
        {
            var readyForPickup = unDroppedAttackers.Where(c => !warpPrism.UnitCalculation.Unit.Passengers.Any(p => p.Tag == c.UnitCalculation.Unit.Tag) && Vector2.DistanceSquared(new Vector2(c.UnitCalculation.Unit.Pos.X, c.UnitCalculation.Unit.Pos.Y), new Vector2(LoadingLocation.X, LoadingLocation.Y)) < 10);
            foreach (var pickup in readyForPickup)
            {
                if (warpPrism.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING)
                {
                    return warpPrism.Order(frame, Abilities.MORPH_WARPPRISMTRANSPORTMODE);
                }

                if (warpPrism.UnitCalculation.Unit.CargoSpaceMax - warpPrism.UnitCalculation.Unit.CargoSpaceTaken >= UnitDataManager.CargoSize((UnitTypes)pickup.UnitCalculation.Unit.UnitType) && !warpPrism.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.UNLOADALLAT_WARPPRISM))
                {
                    if (Vector2.DistanceSquared(new Vector2(warpPrism.UnitCalculation.Unit.Pos.X, warpPrism.UnitCalculation.Unit.Pos.Y), new Vector2(LoadingLocation.X, LoadingLocation.Y)) < PickupRangeSquared)
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
                var dropLoactionDistanceSquared = Vector2.DistanceSquared(new Vector2(warpPrism.UnitCalculation.Unit.Pos.X, warpPrism.UnitCalculation.Unit.Pos.Y), new Vector2(DropLocation.X, DropLocation.Y));
                if (dropLoactionDistanceSquared <= 5)
                {
                    return warpPrism.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, warpPrism.UnitCalculation.Unit.Tag);
                }
                var targetLoactionDistanceSquared = Vector2.DistanceSquared(new Vector2(warpPrism.UnitCalculation.Unit.Pos.X, warpPrism.UnitCalculation.Unit.Pos.Y), new Vector2(TargetLocatoin.X, TargetLocatoin.Y));
                if (dropLoactionDistanceSquared > 5 && targetLoactionDistanceSquared > InsideBaseDistanceSquared)
                {
                    return warpPrism.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, DropLocation);
                }
            }

            if (droppedAttackers.Count() > 0)
            {
                SC2APIProtocol.Action action = null;
                WarpPrismMicroController.SupportArmy(warpPrism, TargetLocatoin, DropLocation, null, frame, out action, droppedAttackers.Select(c => c.UnitCalculation));
                return action;
            }

            if (unDroppedAttackers.Count() > 0)
            {
                SC2APIProtocol.Action action = null;
                WarpPrismMicroController.SupportArmy(warpPrism, TargetLocatoin, DefensiveLocation, null, frame, out action, unDroppedAttackers.Select(c => c.UnitCalculation));
                return action;
            }

            return warpPrism.Order(frame, Abilities.MOVE, DefensiveLocation);
        }

        private void SetLocations()
        {
            if (DefensiveLocation == null)
            {
                DefensiveLocation = ProxyLocationService.GetCliffProxyLocation();
                TargetLocatoin = TargetingData.EnemyMainBasePoint;

                var angle = Math.Atan2(TargetLocatoin.Y - DefensiveLocation.Y, DefensiveLocation.X - TargetLocatoin.X);
                var x = -6 * Math.Cos(angle);
                var y = -6 * Math.Sin(angle);
                LoadingLocation = new Point2D { X = DefensiveLocation.X + (float)x, Y = DefensiveLocation.Y - (float)y };
                LoadingLocationHeight = MapDataService.MapHeight(LoadingLocation);

                x = -25 * Math.Cos(angle);
                y = -25 * Math.Sin(angle);
                DropLocation = new Point2D { X = DefensiveLocation.X + (float)x, Y = DefensiveLocation.Y - (float)y };
                DropLocationHeight = MapDataService.MapHeight(DropLocation);

                InsideBaseDistanceSquared = Vector2.DistanceSquared(new Vector2(DropLocation.X, DropLocation.Y), new Vector2(TargetLocatoin.X, TargetLocatoin.Y)) + 9f;
            }
            DebugService.DrawSphere(new Point { X = LoadingLocation.X, Y = LoadingLocation.Y, Z = 12 }, 3, new Color { R = 0, G = 0, B = 255 });
            DebugService.DrawSphere(new Point { X = DropLocation.X, Y = DropLocation.Y, Z = 12 }, 3, new Color { R = 0, G = 255, B = 0 });
        }
    }
}
