using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class WallOffPlacement : IBuildingPlacement
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        DebugService DebugService;
        MapData MapData;
        BuildingService BuildingService;
        TargetingData TargetingData;

        public WallOffPlacement(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, DebugService debugService, MapData mapData, BuildingService buildingService, TargetingData targetingData)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            DebugService = debugService;
            MapData = mapData;
            BuildingService = buildingService;
            TargetingData = targetingData;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.Full)
        {
            var mineralProximity = 2;
            if (ignoreResourceProximity) { mineralProximity = 0; };

            if (TargetingData.ForwardDefenseWallOffPoints == null) { return null; }
            var wallPoint = TargetingData.ForwardDefenseWallOffPoints.FirstOrDefault();
            if (wallPoint == null) { return null; }
            if (Vector2.DistanceSquared(new Vector2(wallPoint.X, wallPoint.Y), new Vector2(target.X, target.Y)) > maxDistance * maxDistance) { return null; }

            if (unitType == UnitTypes.PROTOSS_PYLON)
            {
                return FindPylonPlacement(TargetingData.ForwardDefenseWallOffPoints, maxDistance, mineralProximity, wallOffType);
            }
            else
            {
                if (unitType == UnitTypes.PROTOSS_GATEWAY || unitType == UnitTypes.PROTOSS_CYBERNETICSCORE || unitType == UnitTypes.PROTOSS_SHIELDBATTERY || unitType == UnitTypes.PROTOSS_PHOTONCANNON)
                {
                    return FindProductionPlacement(TargetingData.ForwardDefenseWallOffPoints, size, maxDistance, mineralProximity, wallOffType);
                }
                return null;
            }
        }

        public Point2D FindPylonPlacement(List<Point2D> wallPoints, float maxDistance, float minimumMineralProximinity = 0, WallOffType wallOffType = WallOffType.Full)
        {
            if (ActiveUnitData.Commanders.Values.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && Vector2.DistanceSquared(c.UnitCalculation.Position, new Vector2(wallPoints.FirstOrDefault().X, wallPoints.FirstOrDefault().Y)) < 100) > 0)
            {
                return null; // if there is already a pylon by the wall don't build more there
            }

            if (wallOffType == WallOffType.Partial)
            {
                return FindPartialWallPylonPlacement(wallPoints, 4);
            }

            return FindFullWallPylonPlacement(wallPoints, 4);
        }

        public Point2D FindPartialWallPylonPlacement(List<Point2D> wallPoints, float maxDistance)
        {
            var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];
            var pylonRadius = (unitData.Size / 2f) - .00000f;

            var distance = 6;

            Point2D best = null;
            int bestCount = 0;

            while (distance <= 7)
            {
                foreach (var reference in wallPoints)
                {
                    var point = new Point2D { X = reference.X, Y = reference.Y };
                    var count = PylonPowersWall(point, pylonRadius);
                    if (count > bestCount) { 
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X, Y = reference.Y + distance };
                    count = PylonPowersWall(point, pylonRadius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X, Y = reference.Y - distance };
                    count = PylonPowersWall(point, pylonRadius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }

                    point = new Point2D { X = reference.X + distance, Y = reference.Y };
                    count = PylonPowersWall(point, pylonRadius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X + distance, Y = reference.Y + distance };
                    count = PylonPowersWall(point, pylonRadius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X + distance, Y = reference.Y - distance };
                    count = PylonPowersWall(point, pylonRadius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }

                    point = new Point2D { X = reference.X - distance, Y = reference.Y };
                    count = PylonPowersWall(point, pylonRadius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X - distance, Y = reference.Y + distance };
                    count = PylonPowersWall(point, pylonRadius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X - distance, Y = reference.Y - distance };
                    count = PylonPowersWall(point, pylonRadius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                }

                distance++;
            }

            return best;
        }

        public Point2D FindFullWallPylonPlacement(List<Point2D> wallPoints, float maxDistance)
        {
            return FindPartialWallPylonPlacement(wallPoints, maxDistance);
        }

        private int PylonPowersWall(Point2D point, float radius)
        {
            if (Buildable(point, radius) && !TargetingData.ForwardDefenseWallOffPoints.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), new Vector2(point.X, point.Y)) < 3))
            {
                return TargetingData.ForwardDefenseWallOffPoints.Count(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), new Vector2(point.X, point.Y)) <= 49);
            }

            return -1;
        }

        private int BuildablePartialWall(IEnumerable<UnitCommander> powerSources, Point2D point, float radius)
        {
            if (Buildable(point, radius) && Powered(powerSources, point, radius) && FormingPartialWall(point, radius))
            {
                return TargetingData.ForwardDefenseWallOffPoints.Count(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), new Vector2(point.X, point.Y)) <= (radius + 1) * (radius + 1));
            }

            return -1;
        }

        private int BuildableWall(IEnumerable<UnitCommander> powerSources, Point2D point, float radius)
        {
            if (Buildable(point, radius) && Powered(powerSources, point, radius) && FormingWall(point, radius))
            {
                return TargetingData.ForwardDefenseWallOffPoints.Count(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), new Vector2(point.X, point.Y)) <= (radius + 1) * (radius + 1));
            }

            return -1;
        }

        private bool Buildable(Point2D point, float radius)
        {
            if (BuildingService.AreaBuildable(point.X, point.Y, radius) && !BuildingService.Blocked(point.X, point.Y, radius, 0) && !BuildingService.HasCreep(point.X, point.Y, radius))
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

        private bool FormingWall(Point2D point, float radius)
        {
            if (!Buildable(point, radius + 1f))
            {
                return true;
            }

            return false;
        }

        private bool FormingPartialWall(Point2D point, float radius)
        {
            if (!Buildable(point, radius + 1f)) // it's touching a wall
            {
                var gaps = BuildingService.UnWalledPoints();
                foreach (var gap in gaps)
                {
                    if (Vector2.DistanceSquared(new Vector2(point.X, point.Y), new Vector2(gap.X, gap.Y)) > radius * radius)
                    {
                        return true; // at least one gap is open
                    }
                }
            }

            return false;
        }

        public Point2D FindProductionPlacement(List<Point2D> wallPoints, float size, float maxDistance, float minimumMineralProximinity = 2, WallOffType wallOffType = WallOffType.Full)
        {
            if (wallOffType == WallOffType.Partial)
            {
                return FindPartialWallProductionPlacement(wallPoints, size, 4);
            }

            return FindFullWallProductionPlacement(wallPoints, size, 4);
        }

        public Point2D FindPartialWallProductionPlacement(List<Point2D> wallPoints, float size, float maxDistance)
        {
            var powerSources = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).Where(c => Vector2.DistanceSquared(c.UnitCalculation.Position, new Vector2(wallPoints.FirstOrDefault().X, wallPoints.FirstOrDefault().Y)) < 15 * 15);
            if (powerSources.Count() == 0) { return null; }

            var radius = (size / 2f) - .00000f;

            var distance = 0;

            Point2D best = null;
            int bestCount = -1;

            while (distance < maxDistance)
            {
                foreach (var reference in wallPoints)
                {
                    var point = new Point2D { X = reference.X, Y = reference.Y };
                    var count = BuildablePartialWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X, Y = reference.Y + distance };
                    count = BuildablePartialWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X, Y = reference.Y - distance };
                    count = BuildablePartialWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }

                    point = new Point2D { X = reference.X + distance, Y = reference.Y };
                    count = BuildablePartialWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X + distance, Y = reference.Y + distance };
                    count = BuildablePartialWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X + distance, Y = reference.Y - distance };
                    count = BuildablePartialWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }

                    point = new Point2D { X = reference.X - distance, Y = reference.Y };
                    count = BuildablePartialWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X - distance, Y = reference.Y + distance };
                    count = BuildablePartialWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X - distance, Y = reference.Y - distance };
                    count = BuildablePartialWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                }

                distance++;
            }

            return best;
        }

        public Point2D FindFullWallProductionPlacement(List<Point2D> wallPoints, float size, float maxDistance)
        {
            var powerSources = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).Where(c => Vector2.DistanceSquared(c.UnitCalculation.Position, new Vector2(wallPoints.FirstOrDefault().X, wallPoints.FirstOrDefault().Y)) < 15 * 15);
            if (powerSources.Count() == 0) { return null; }

            var radius = (size / 2f) - .00000f;

            var distance = 0;

            Point2D best = null;
            int bestCount = 0;

            while (distance < maxDistance)
            {
                foreach (var reference in wallPoints)
                {
                    var point = new Point2D { X = reference.X, Y = reference.Y };
                    var count = BuildableWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X, Y = reference.Y + distance };
                    count = BuildableWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X, Y = reference.Y - distance };
                    count = BuildableWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }

                    point = new Point2D { X = reference.X + distance, Y = reference.Y };
                    count = BuildableWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X + distance, Y = reference.Y + distance };
                    count = BuildableWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X + distance, Y = reference.Y - distance };
                    count = BuildableWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }

                    point = new Point2D { X = reference.X - distance, Y = reference.Y };
                    count = BuildableWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X - distance, Y = reference.Y + distance };
                    count = BuildableWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                    point = new Point2D { X = reference.X - distance, Y = reference.Y - distance };
                    count = BuildableWall(powerSources, point, radius);
                    if (count > bestCount)
                    {
                        best = point;
                        bestCount = count;
                    }
                }

                distance++;
            }

            return null;
        }

        bool Powered(IEnumerable<UnitCommander> powerSources, Point2D point, float radius)
        {
            var vector = new Vector2(point.X, point.Y);
            return powerSources.Any(p => Vector2.DistanceSquared(p.UnitCalculation.Position, vector) <= (7 - radius) * (7 - radius));
        }
    }
}
