using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Zerg
{
    public class CreepTumorPlacementFinder
    {
        BuildingService BuildingService;
        TargetingData TargetingData;
        MacroData MacroData;
        BaseData BaseData;
        IBuildingPlacement ZergBuildingPlacement;

        IPathFinder PathFinder;

        // Path from main to expansions, target, any base
        // Check if every single point is creep, if spot is not creep, place a creep tumor at the spot before it that is creep
        // For extending tumors follow path but only use points within range
        // Also spreed creep in main base for vision later

        public CreepTumorPlacementFinder(DefaultSharkyBot defaultSharkyBot, IPathFinder pathFinder)
        {
            BuildingService = defaultSharkyBot.BuildingService;
            TargetingData = defaultSharkyBot.TargetingData;
            MacroData = defaultSharkyBot.MacroData;
            BaseData = defaultSharkyBot.BaseData;
            ZergBuildingPlacement = defaultSharkyBot.ZergBuildingPlacement;

            PathFinder = pathFinder;
        }

        public Point2D FindTumorPlacement()
        {
            // path from main to natural
            var main = BaseData.SelfBases.FirstOrDefault();
            if (main != null)
            {
                var startLocation = main.Location;
                foreach (var baseLocation in BaseData.BaseLocations)
                {
                    var start = new Point2D { X = startLocation.X + 4, Y = startLocation.Y + 4 };
                    var end = new Point2D { X = baseLocation.Location.X + 4, Y = baseLocation.Location.Y + 4 };
                    var path = PathFinder.GetGroundPath(start.X, start.Y, end.X, end.Y, MacroData.Frame);

                    var spot = start;
                    foreach (var point in path)
                    {
                        if (!BuildingService.HasAnyCreep(point.X, point.Y, 1))
                        {
                            return ZergBuildingPlacement.FindPlacement(spot, UnitTypes.ZERG_CREEPTUMORQUEEN, 1);
                        }
                        else
                        {
                            spot = new Point2D { X = point.X, Y = point.Y };
                        }
                    }

                    startLocation = baseLocation.Location;
                }
            }

            return null;
        }

        public Point2D FindTumorExtensionPlacement(Vector2 location)
        {
            var start = new Point2D { X = location.X + 1, Y = location.Y + 1 };
            Point2D spot = null;

            var closestBase = BaseData.BaseLocations.OrderBy(b => Vector2.DistanceSquared(location, new Vector2(b.Location.X, b.Location.Y))).FirstOrDefault();
            if (closestBase != null)
            {
                var end = new Point2D { X = closestBase.Location.X + 4, Y = closestBase.Location.Y + 4 };
                var path = PathFinder.GetGroundPath(start.X, start.Y, end.X, end.Y, MacroData.Frame);
                if (!FullyCreeped(path))
                {
                    spot = GetSpot(location, start, end, path);
                }
            }

            if (spot == null)
            {
                var end = new Point2D { X = TargetingData.AttackPoint.X + 4, Y = TargetingData.AttackPoint.Y + 4 };
                spot = GetSpot(location, start, end);
            }

            if (spot == null)
            {
                spot = start;
            }
            return ZergBuildingPlacement.FindPlacement(spot, UnitTypes.ZERG_CREEPTUMORQUEEN, 1, maxDistance: 10);
        }

        private Point2D GetSpot(Vector2 location, Point2D start, Point2D end, List<Vector2> path = null)
        {
            if (path == null)
            {
                path = PathFinder.GetGroundPath(start.X, start.Y, end.X, end.Y, MacroData.Frame);
            }

            var spot = start;
            foreach (var point in path)
            {
                if (!BuildingService.HasAnyCreep(point.X, point.Y, 1) || Vector2.DistanceSquared(location, point) > 64)
                {
                    var result = ZergBuildingPlacement.FindPlacement(spot, UnitTypes.ZERG_CREEPTUMORQUEEN, 1, maxDistance: 10);
                    if (result == null)
                    {
                        result = ZergBuildingPlacement.FindPlacement(start, UnitTypes.ZERG_CREEPTUMORQUEEN, 1, maxDistance: 10);
                    }
                    return result;
                }
                else
                {
                    spot = new Point2D { X = point.X, Y = point.Y };
                }
            }
            return null;
        }

        private bool FullyCreeped(List<Vector2> path)
        {
            foreach (var point in path)
            {
                if (!BuildingService.HasAnyCreep(point.X, point.Y, 1))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
