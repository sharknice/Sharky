using SC2APIProtocol;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class TerranBuildingPlacement : IBuildingPlacement
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        BaseData BaseData;
        DebugService DebugService;
        BuildingService BuildingService;
        IBuildingPlacement WallOffPlacement;
        TerranWallService TerranWallService;
        TerranSupplyDepotGridPlacement TerranBuildingGridPlacement;
        TerranProductionGridPlacement TerranProductionGridPlacement;
        TerranTechGridPlacement TerranTechGridPlacement;
        IBuildingPlacement MissileTurretPlacement;

        public TerranBuildingPlacement(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, BaseData baseData, DebugService debugService, BuildingService buildingService, IBuildingPlacement wallOffPlacement, TerranWallService terranWallService, TerranSupplyDepotGridPlacement terranBuildingGridPlacement, TerranProductionGridPlacement terranProductionGridPlacement, TerranTechGridPlacement terranTechGridPlacement, IBuildingPlacement missileTurretPlacement)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            BaseData = baseData;
            DebugService = debugService;
            BuildingService = buildingService;
            WallOffPlacement = wallOffPlacement;
            TerranWallService = terranWallService;
            TerranBuildingGridPlacement = terranBuildingGridPlacement;
            TerranProductionGridPlacement = terranProductionGridPlacement;
            TerranTechGridPlacement = terranTechGridPlacement;
            MissileTurretPlacement = missileTurretPlacement;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None, bool requireVision = false)
        {
            var mineralProximity = 2;
            if (ignoreResourceProximity) { mineralProximity = 0; };

            if (unitType == UnitTypes.TERRAN_BARRACKS || unitType == UnitTypes.TERRAN_FACTORY || unitType == UnitTypes.TERRAN_STARPORT || unitType == UnitTypes.TERRAN_BARRACKSTECHLAB || unitType == UnitTypes.TERRAN_COMMANDCENTER)
            {
                return FindProductionPlacement(target, unitType, size, maxDistance, wallOffType, mineralProximity);
            }
            if (unitType == UnitTypes.TERRAN_SUPPLYDEPOT)
            {
                return FindSupplyDepotPlacement(target, size, maxDistance, wallOffType, mineralProximity);
            }
            else if (unitType == UnitTypes.TERRAN_BUNKER)
            {
                var spot = FindBunkerPlacement(target, size, maxDistance, wallOffType, mineralProximity);
                if (spot != null) { return spot; }
            }
            else if (unitType == UnitTypes.TERRAN_MISSILETURRET)
            {
                var spot = MissileTurretPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance, requireSameHeight, wallOffType);
                if (spot != null) { return spot; }
            }
            else
            {
                var spot = TerranTechGridPlacement.FindPlacement(target, unitType, size, maxDistance, mineralProximity);
                if (spot != null) { return spot; }
            }
            if (unitType == UnitTypes.TERRAN_COMMANDCENTER)
            {
                return null;
            }
            return FindTechPlacement(target, size, maxDistance, mineralProximity);
        }

        Point2D FindSupplyDepotPlacement(Point2D target, float size, float maxDistance, WallOffType wallOffType, float minimumMineralProximinity)
        {
            Point2D spot;
            if (wallOffType == WallOffType.Terran)
            {
                spot = TerranWallService.FindSupplyDepotWallPlacement(target, size, maxDistance, minimumMineralProximinity);
                if (spot != null) { return spot; }
            }

            spot = TerranBuildingGridPlacement.FindPlacement(target, size, maxDistance, minimumMineralProximinity);
            if (spot != null) { return spot; }

            return FindTechPlacement(target, size, maxDistance, minimumMineralProximinity);
        }

        Point2D FindBunkerPlacement(Point2D target, float size, float maxDistance, WallOffType wallOffType, float minimumMineralProximinity)
        {
            Point2D spot;
            if (wallOffType == WallOffType.Terran)
            {
                spot = TerranWallService.FindBunkerPlacement(target, size, maxDistance, minimumMineralProximinity);
                if (spot != null) { return spot; }
            }

            return FindTechPlacement(target, size, maxDistance, minimumMineralProximinity);
        }

        public Point2D FindProductionPlacement(Point2D reference, UnitTypes unitType, float size, float maxDistance, WallOffType wallOffType, float minimumMineralProximinity = 5)
        {
            Point2D spot;
            if (wallOffType == WallOffType.Terran)
            {
                spot = TerranWallService.FindProductionWallPlacement(reference, unitType, size, maxDistance, minimumMineralProximinity);
                if (spot != null) { return spot; }
            }

            if (unitType == UnitTypes.TERRAN_BARRACKSTECHLAB || unitType == UnitTypes.TERRAN_BARRACKSREACTOR || unitType == UnitTypes.TERRAN_FACTORYTECHLAB || unitType == UnitTypes.TERRAN_FACTORYREACTOR || unitType == UnitTypes.TERRAN_STARPORTTECHLAB || unitType == UnitTypes.TERRAN_STARPORTREACTOR)
            {
                size += 2;
            }

            spot = TerranProductionGridPlacement.FindPlacement(reference, unitType, size, maxDistance, minimumMineralProximinity);
            if (spot != null) { return spot; }

            return FindTechPlacement(reference, size + 4f, maxDistance, minimumMineralProximinity); // add to the radius to make room for the addon and completed units to exist
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
                    if (BuildingService.AreaBuildable(point.X, point.Y, size / 2.0f) && !BuildingService.Blocked(point.X, point.Y, size / 2.0f) && !BuildingService.HasAnyCreep(point.X, point.Y, size / 2.0f))
                    {
                        var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                        var squared = (1 + minimumMineralProximinity + (size/2f)) * (1 + minimumMineralProximinity + (size / 2f));
                        var vector = new Vector2(point.X, point.Y);
                        var clashes = mineralFields.Where(u => Vector2.DistanceSquared(u.Value.Position, vector) < squared);
                        bool blocksBase = false;
                        if (minimumMineralProximinity != 0)
                        {
                            if (BaseData.BaseLocations.Any(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), vector) < 25))
                            {
                                blocksBase = true;
                            }
                        }

                        if (!blocksBase && clashes.Count() == 0)
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
    }
}
