using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class TerranBuildingPlacement : IBuildingPlacement
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        MapData MapData;
        TargetingData TargetingData;
        DebugService DebugService;
        BuildingService BuildingService;
        IBuildingPlacement WallOffPlacement;
        WallService WallService;

        public TerranBuildingPlacement(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, MapData mapData, TargetingData targetingData, DebugService debugService, BuildingService buildingService, IBuildingPlacement wallOffPlacement, WallService wallService)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            MapData = mapData;
            TargetingData = targetingData;
            DebugService = debugService;
            BuildingService = buildingService;
            WallOffPlacement = wallOffPlacement;
            WallService = wallService;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None)
        {
            var mineralProximity = 2;
            if (ignoreResourceProximity) { mineralProximity = 0; };

            if (wallOffType == WallOffType.Terran)
            {
                var point = WallOffPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance, requireSameHeight, wallOffType);
                if (point != null)
                {
                    return point;
                }
            }
            if (unitType == UnitTypes.TERRAN_BARRACKS || unitType == UnitTypes.TERRAN_FACTORY || unitType == UnitTypes.TERRAN_STARPORT)
            {
                return FindProductionPlacement(target, size, maxDistance, mineralProximity);
            }
            if (unitType == UnitTypes.TERRAN_SUPPLYDEPOT)
            {
                return FindSupplyDepotPlacement(target, size, maxDistance, mineralProximity);
            }
            return FindTechPlacement(target, size, maxDistance, mineralProximity);
        }

        Point2D FindSupplyDepotPlacement(Point2D target, float size, float maxDistance, float minimumMineralProximinity)
        {
            if (MapData != null && MapData.TerranWallData != null)
            {
                var wallData = MapData.TerranWallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.NaturalBasePoint.X && b.BasePosition.Y == TargetingData.NaturalBasePoint.Y);
                if (wallData != null && wallData.Depots != null)
                {
                    var existingDepots = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOT || u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
                    foreach (var spot in wallData.Depots)
                    {
                        if (!existingDepots.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y) && WallService.Buildable(spot, .5f))
                        {
                            return spot;
                        }
                    }
                }
            }

            return FindTechPlacement(target, size, maxDistance, minimumMineralProximinity);
        }

        public Point2D FindTechPlacement(Point2D reference, float size, float maxDistance, float minimumMineralProximinity = 2)
        {
            var x = reference.X;
            var y = reference.Y;
            var radius = size / 2f;

            // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than maxDistance
            while (radius < maxDistance / 2.0)
            {
                var fullCircle = Math.PI * 2;
                var sliceSize = fullCircle / (4.0 + radius);
                var angle = 0.0;
                while (angle + (sliceSize / 2) < fullCircle)
                {
                    var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                    if (BuildingService.AreaBuildable(point.X, point.Y, size / 2.0f) && !BuildingService.Blocked(point.X, point.Y, size / 2.0f) && !BuildingService.HasCreep(point.X, point.Y, size / 2.0f))
                    {
                        var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                        var squared = (1 + minimumMineralProximinity + (size/2f)) * (1 + minimumMineralProximinity + (size / 2f));
                        var clashes = mineralFields.Where(u => Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < squared);

                        if (clashes.Count() == 0)
                        {
                            var productionStructures = ActiveUnitData.SelfUnits.Where(u => u.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS || u.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY || u.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT);
                            if (!productionStructures.Any(u => Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < 16))
                            {
                                if (Vector2.DistanceSquared(new Vector2(reference.X, reference.Y), new Vector2(point.X, point.Y)) <= maxDistance * maxDistance)
                                {
                                    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });
                                    return point;
                                }
                            }
                        }
                    }

                    angle += sliceSize;
                }
                radius += 1;
            }

            return null;
        }

        public Point2D FindProductionPlacement(Point2D reference, float size, float maxDistance, float minimumMineralProximinity = 5)
        {
            return FindTechPlacement(reference, size + 4f, maxDistance, minimumMineralProximinity); // add to the radius to make room for the addon and completed units to exist
        }
    }
}
