using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.Pathing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class ForceFieldRampTask : MicroTask
    {
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;

        WallService WallService;
        MapDataService MapDataService;
        MapData MapData;

        int LastForceFieldFrame;

        Point2D ForceFieldPoint;

        public ForceFieldRampTask(TargetingData targetingData, ActiveUnitData activeUnitData, MapData mapData, WallService wallService, MapDataService mapDataService, bool enabled, float priority)
        {
            TargetingData = targetingData;
            ActiveUnitData = activeUnitData;
            MapData = mapData;
            WallService = wallService;
            MapDataService = mapDataService;
            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
            LastForceFieldFrame = 0;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY)
                {
                    commander.Value.Claimed = true;
                    UnitCommanders.Add(commander.Value);
                    break;
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            // if forcefield there move to defensive spot
            // TODO: if enemy ground army units on lower ground than force field location, force field ramp, (not for massive or adept shades though)

            SetForceFieldSpot(frame);

            var forceField = ActiveUnitData.NeutralUnits.FirstOrDefault(u => u.Value.Unit.UnitType == (uint)UnitTypes.NEUTRAL_FORCEFIELD && Vector2.DistanceSquared(new Vector2(ForceFieldPoint.X, ForceFieldPoint.Y), u.Value.Position) < 1).Value;

            foreach (var commander in UnitCommanders)
            {
                if (forceField != null || commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_FORCEFIELD))
                {
                    continue;
                }

                if (commander.UnitCalculation.Unit.Energy >= 50 && frame - LastForceFieldFrame > 20)
                {
                    var probeHeight = MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos);
                    if (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen >= frame - 1 && e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !e.Attributes.Contains(SC2APIProtocol.Attribute.Massive) && !e.Unit.IsFlying && e.Unit.UnitType != (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT && probeHeight > MapDataService.MapHeight(e.Unit.Pos)))
                    {
                        LastForceFieldFrame = frame;
                        var action = commander.Order(frame, Abilities.EFFECT_FORCEFIELD, ForceFieldPoint);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        continue;
                    }
                }

                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y)) > 9)
                {
                    var action = commander.Order(frame, Abilities.MOVE, TargetingData.ForwardDefensePoint);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
            }
 
            return commands;
        }

        private void SetForceFieldSpot(int frame)
        {
            if (ForceFieldPoint == null)
            {
                var baseLocation = WallService.GetBaseLocation();
                if (baseLocation == null) { return; }

                if (MapData != null && MapData.TerranWallData != null)
                {
                    var data = MapData.TerranWallData.FirstOrDefault(d => d.BasePosition.X == baseLocation.X && d.BasePosition.Y == baseLocation.Y);
                    if (data != null && data.RampCenter != null)
                    {
                        ForceFieldPoint = data.RampCenter;
                        return;
                    }
                }

                var chokePoint = TargetingData.ChokePoints.Good.FirstOrDefault();
                if (chokePoint != null)
                {
                    ForceFieldPoint = new Point2D { X = chokePoint.Center.X, Y = chokePoint.Center.Y };
                    return;
                }

                ForceFieldPoint = TargetingData.ForwardDefensePoint;
            }
        }
    }
}
