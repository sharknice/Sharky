using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class HardCodedWallOffPlacement : IBuildingPlacement
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        DebugService DebugService;
        MapData MapData;
        BuildingService BuildingService;
        TargetingData TargetingData;
        BaseData BaseData;

        public HardCodedWallOffPlacement(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, DebugService debugService, MapData mapData, BuildingService buildingService, TargetingData targetingData, BaseData baseData)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            DebugService = debugService;
            MapData = mapData;
            BuildingService = buildingService;
            TargetingData = targetingData;
            BaseData = baseData;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.Full)
        {
            var mineralProximity = 2;
            if (ignoreResourceProximity) { mineralProximity = 0; };

            if (wallOffType == WallOffType.Partial && MapData.PartialWallData == null) { return null; }

            var baseLocation = GetBaseLocation();
            if (baseLocation == null) { return null; }

            WallData wallData = null;
            if (wallOffType == WallOffType.Partial)
            {
                wallData = MapData.PartialWallData.FirstOrDefault(b => b.BasePosition.X == baseLocation.X && b.BasePosition.Y == baseLocation.Y);
                if (wallData == null) { return null; }
            }

            if (unitType == UnitTypes.PROTOSS_PYLON)
            {
                var placement = FindPylonPlacement(wallData, maxDistance, mineralProximity, wallOffType);
                if (placement == null) { return null; }
                if (Vector2.DistanceSquared(new Vector2(placement.X, placement.Y), new Vector2(target.X, target.Y)) > maxDistance * maxDistance) { return null; }
                return placement;
            }
            else
            {
                var placement = FindProductionPlacement(wallData, size, maxDistance, mineralProximity, wallOffType);
                if (placement == null) { return null; }
                if (Vector2.DistanceSquared(new Vector2(placement.X, placement.Y), new Vector2(target.X, target.Y)) > maxDistance * maxDistance) { return null; }
                return placement;
            }
        }

        Point2D GetBaseLocation()
        {
            if (TargetingData.WallOffBasePosition == WallOffBasePosition.Main)
            {
                return TargetingData.EnemyMainBasePoint;
            }
            else if (TargetingData.WallOffBasePosition == WallOffBasePosition.Natural)
            {
                return TargetingData.NaturalBasePoint;
            }
            else
            {
                if (TargetingData.ForwardDefenseWallOffPoints == null) { return null; }
                var wallPoint = TargetingData.ForwardDefenseWallOffPoints.FirstOrDefault();
                if (wallPoint == null) { return null; }

                var baseLocation = BaseData.SelfBases.OrderBy(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), new Vector2(wallPoint.X, wallPoint.Y))).FirstOrDefault();
                if (baseLocation == null) { return null; }
                return baseLocation.Location;
            }
        }

        public Point2D FindPylonPlacement(WallData wallData, float maxDistance, float minimumMineralProximinity = 0, WallOffType wallOffType = WallOffType.Full)
        {

            if (wallOffType == WallOffType.Partial)
            {
                return FindPartialWallPylonPlacement(wallData);
            }

            return FindFullWallPylonPlacement(wallData);
        }

        public Point2D FindPartialWallPylonPlacement(WallData wallData)
        {
            if (wallData.Pylons == null) { return null; }
            var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];
            var pylonRadius = (unitData.Size / 2f) - .00000f;
            var existingPylons = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON);
            foreach (var pylon in wallData.Pylons)
            {
                if (!existingPylons.Any(e => e.Position.X == pylon.X && e.Position.Y == pylon.Y) && Buildable(pylon, pylonRadius))
                {
                    return pylon;
                }
            }
            return null;
        }

        public Point2D FindFullWallPylonPlacement(WallData wallData)
        {
            return FindPartialWallPylonPlacement(wallData);
        }

        private bool Buildable(Point2D point, float radius)
        {
            if (BuildingService.AreaBuildable(point.X, point.Y, radius) && !BuildingService.Blocked(point.X, point.Y, radius, -.5f) && !BuildingService.HasCreep(point.X, point.Y, radius))
            {
                var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType) || SharkyUnitData.GasGeyserTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                var squared = (1 + .5) * (1 + .5);
                var nexusDistanceSquared = 0;
                var nexusClashes = ActiveUnitData.SelfUnits.Where(u => (u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS || u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON) && Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < squared + nexusDistanceSquared);
                if (nexusClashes.Count() == 0)
                {
                    var clashes = mineralFields.Where(u => Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < squared);
                    if (clashes.Count() == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Point2D FindProductionPlacement(WallData wallData, float size, float maxDistance, float minimumMineralProximinity = 2, WallOffType wallOffType = WallOffType.Full)
        {
            if (wallOffType == WallOffType.Partial)
            {
                return FindPartialWallProductionPlacement(wallData, size, 4);
            }

            return FindFullWallProductionPlacement(wallData, size, 4);
        }

        public Point2D FindPartialWallProductionPlacement(WallData wallData, float size, float maxDistance)
        {
            if (wallData.WallSegments == null) { return null; }
            var existingBuildings = ActiveUnitData.SelfUnits.Values.Where(u => u.Attributes.Contains(Attribute.Structure));
            var radius = (size / 2f);
            var powerSources = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).Where(c => Vector2.DistanceSquared(c.UnitCalculation.Position, new Vector2(wallData.Pylons.FirstOrDefault().X, wallData.Pylons.FirstOrDefault().Y)) < 15 * 15);

            foreach (var segment in wallData.WallSegments.Where(w => w.Size == size))
            {
                var point = segment.Position;
                if (!existingBuildings.Any(e => e.Position.X == point.X && e.Position.Y == point.Y) && Buildable(point, radius) && Powered(powerSources, point, radius))
                {
                    return point;
                }
            }
            return null;
        }

        public Point2D FindFullWallProductionPlacement(WallData wallData, float size, float maxDistance)
        {
            return FindPartialWallProductionPlacement(wallData, size, maxDistance);
        }

        bool Powered(IEnumerable<UnitCommander> powerSources, Point2D point, float radius)
        {
            var vector = new Vector2(point.X, point.Y);
            return powerSources.Any(p => Vector2.DistanceSquared(p.UnitCalculation.Position, vector) <= (7) * (7));
        }
    }
}
