using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class TerranBuildingPlacement : IBuildingPlacement
    {
        IUnitManager UnitManager;
        UnitDataManager UnitDataManager;
        DebugManager DebugManager;
        BuildingService BuildingService;

        public TerranBuildingPlacement(IUnitManager unitManager, UnitDataManager unitDataManager, DebugManager debugManager, BuildingService buildingService)
        {
            UnitManager = unitManager;
            UnitDataManager = unitDataManager;
            DebugManager = debugManager;
            BuildingService = buildingService;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50)
        {
            var mineralProximity = 2;
            if (ignoreResourceProximity) { mineralProximity = 0; };

            if (unitType == UnitTypes.TERRAN_BARRACKS || unitType == UnitTypes.TERRAN_FACTORY || unitType == UnitTypes.TERRAN_STARPORT)
            {
                return FindProductionPlacement(target, size, maxDistance, mineralProximity);
            }
            return FindTechPlacement(target, size, maxDistance, mineralProximity);
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
                        var mineralFields = UnitManager.NeutralUnits.Where(u => UnitDataManager.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                        var squared = (1 + minimumMineralProximinity + (size/2f)) * (1 + minimumMineralProximinity + (size / 2f));
                        var clashes = mineralFields.Where(u => Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(point.X, point.Y)) < squared);

                        if (clashes.Count() == 0)
                        {
                            var productionStructures = UnitManager.SelfUnits.Where(u => u.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS || u.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY || u.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT);
                            if (!productionStructures.Any(u => Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(point.X, point.Y)) < 16))
                            {
                                if (Vector2.DistanceSquared(new Vector2(reference.X, reference.Y), new Vector2(point.X, point.Y)) <= maxDistance * maxDistance)
                                {
                                    DebugManager.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });
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
