using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class ProtossBuildingPlacement : IBuildingPlacement
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        DebugService DebugService;
        MapDataService MapDataService;
        BuildingService BuildingService;
        IBuildingPlacement WallOffPlacement;

        public ProtossBuildingPlacement(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, DebugService debugService, MapDataService mapDataService, BuildingService buildingService, IBuildingPlacement wallOffPlacement)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            DebugService = debugService;
            MapDataService = mapDataService;
            BuildingService = buildingService;
            WallOffPlacement = wallOffPlacement;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None)
        {
            var mineralProximity = 2;
            if (ignoreResourceProximity) { mineralProximity = 0; };

            if (wallOffType == WallOffType.Full)
            {
                if (!BuildingService.FullyWalled())
                {
                    var point = WallOffPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance, requireSameHeight, wallOffType);
                    if (point != null)
                    {
                        return point;
                    }
                }
            }
            else if (wallOffType == WallOffType.Partial)
            {
                if (!BuildingService.PartiallyWalled())
                {
                    var point = WallOffPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance, requireSameHeight, wallOffType);
                    if (point != null)
                    {
                        return point;
                    }
                }
            }

            if (unitType == UnitTypes.PROTOSS_PYLON)
            {
                return FindPylonPlacement(target, maxDistance, mineralProximity, requireSameHeight, wallOffType);
            }
            else
            {
                return FindProductionPlacement(target, size, maxDistance, mineralProximity, wallOffType);
            }
        }

        public Point2D FindPylonPlacement(Point2D reference, float maxDistance, float minimumMineralProximinity = 2, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None)
        {
            var x = reference.X;
            var y = reference.Y;
            var radius = 1f;

            // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than maxDistance
            while (radius < maxDistance / 2.0)
            {
                var fullCircle = Math.PI * 2;
                var sliceSize = fullCircle / (4.0 + radius);
                var angle = 0.0;
                while (angle + (sliceSize / 2) < fullCircle)
                {
                    var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                    //DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });

                    //if (!BuildingService.AreaBuildable(point.X, point.Y, 1.25f))
                    //{
                    //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 }, 1, new Color { R = 255, G = 0, B = 0 });
                    //}
                    //else if (BuildingService.Blocked(point.X, point.Y, 1.25f, .1f))
                    //{
                    //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 }, 1, new Color { R = 255, G = 255, B = 0 });
                    //}
                    //else if (BuildingService.HasCreep(point.X, point.Y, 1.5f))
                    //{
                    //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 }, 1, new Color { R = 255, G = 255, B = 255 });
                    //}
                    //else
                    //{
                    //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 }, 1, new Color { R = 0, G = 255, B = 0 });
                    //}

                    if (BuildingService.AreaBuildable(point.X, point.Y, 1.25f) && !BuildingService.Blocked(point.X, point.Y, 1.25f, .1f) && !BuildingService.HasCreep(point.X, point.Y, 1.5f) && (!requireSameHeight || MapDataService.MapHeight(point) == MapDataService.MapHeight(reference)))
                    {
                        var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType) || SharkyUnitData.GasGeyserTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                        var squared = (1 + minimumMineralProximinity + .5) * (1 + minimumMineralProximinity + .5);
                        var nexusDistanceSquared = 16f;
                        if (minimumMineralProximinity == 0) { nexusDistanceSquared = 0; }
                        var nexusClashes = ActiveUnitData.SelfUnits.Where(u => (u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS || u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON) && Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < squared + nexusDistanceSquared);
                        if (nexusClashes.Count() == 0)
                        {
                            var clashes = mineralFields.Where(u => Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < squared);
                            if (clashes.Count() == 0)
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

        public Point2D FindProductionPlacement(Point2D target, float size, float maxDistance, float minimumMineralProximinity = 2, WallOffType wallOffType = WallOffType.None)
        {
            var targetVector = new Vector2(target.X, target.Y);
            var powerSources = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, targetVector));
            foreach (var powerSource in powerSources)
            {
                if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), powerSource.UnitCalculation.Position) > (maxDistance + 8) * (maxDistance + 8)) 
                { 
                    break; 
                }

                var x = powerSource.UnitCalculation.Unit.Pos.X;
                var y = powerSource.UnitCalculation.Unit.Pos.Y;
                var radius = size / 2f;
                var powerRadius = 7 - (size / 2f);

                // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than powerRadius
                while (radius <= powerRadius)
                {
                    var fullCircle = Math.PI * 2;
                    var sliceSize = fullCircle / 24.0;
                    var angle = 0.0;
                    while (angle + (sliceSize / 2) < fullCircle)
                    {
                        var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                        //DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });

                        //if (!BuildingService.AreaBuildable(point.X, point.Y, 1.25f))
                        //{
                        //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 10 }, 1, new Color { R = 255, G = 0, B = 0 });
                        //}
                        //else if (BuildingService.Blocked(point.X, point.Y, 1.25f))
                        //{
                        //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 10 }, 1, new Color { R = 255, G = 255, B = 0 });
                        //}
                        //else if (BuildingService.HasCreep(point.X, point.Y, 1.5f))
                        //{
                        //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 10 }, 1, new Color { R = 255, G = 255, B = 255 });
                        //}
                        //else
                        //{
                        //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 10 }, 1, new Color { R = 0, G = 255, B = 0 });
                        //}
                        var vector = new Vector2(point.X, point.Y);
                        var tooClose = false;
                        if (MapDataService.MapData.BlockWallData != null && MapDataService.MapData.BlockWallData.Any(d => d.Pylons.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), vector) < 25)))
                        {
                            tooClose = true;
                        }

                        if (!tooClose && BuildingService.SameHeight(point.X, point.Y, size + 1 / 2.0f) && BuildingService.AreaBuildable(point.X, point.Y, (size + 1) / 2.0f) && !BuildingService.Blocked(point.X, point.Y, (size + 1) / 2.0f) && !BuildingService.HasCreep(point.X, point.Y, size / 2.0f)) // size +1 because want 1 space to move around to prevent walling self in
                        {
                            var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                            var squared = (1 + minimumMineralProximinity + (size / 2f)) * (1 + minimumMineralProximinity + (size / 2f));
                            var clashes = mineralFields.Where(u => Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < squared);

                            if (clashes.Count() == 0)
                            {
                                if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), new Vector2(point.X, point.Y)) <= maxDistance * maxDistance)
                                {
                                    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });
                                    return point;
                                }
                            }
                        }

                        angle += sliceSize;
                    }
                    radius += 1;
                }
            }
            return FindProductionPlacementTryHarder(target, size, maxDistance, minimumMineralProximinity);
        }

        Point2D FindProductionPlacementTryHarder(Point2D target, float size, float maxDistance, float minimumMineralProximinity)
        {
            var targetVector = new Vector2(target.X, target.Y);
            var powerSources = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, targetVector));

            foreach (var powerSource in powerSources)
            {
                if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), powerSource.UnitCalculation.Position) > (maxDistance + 8) * (maxDistance + 8))
                {
                    break;
                }

                var x = powerSource.UnitCalculation.Unit.Pos.X;
                var y = powerSource.UnitCalculation.Unit.Pos.Y;
                var radius = size / 2f;
                var powerRadius = 7 - (size / 2f);

                // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than powerRadius
                while (radius <= powerRadius)
                {
                    var fullCircle = Math.PI * 2;
                    var sliceSize = fullCircle / 48.0;
                    var angle = 0.0;
                    while (angle + (sliceSize / 2) < fullCircle)
                    {
                        var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                        //DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });

                        //if (!BuildingService.AreaBuildable(point.X, point.Y, size / 2.0f))
                        //{
                        //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 10 }, 1, new Color { R = 255, G = 0, B = 0 });
                        //}
                        //else if (BuildingService.Blocked(point.X, point.Y, size / 2.0f, 0))
                        //{
                        //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 10 }, 1, new Color { R = 255, G = 255, B = 0 });
                        //}
                        //else if (BuildingService.HasCreep(point.X, point.Y, size / 2.0f))
                        //{
                        //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 10 }, 1, new Color { R = 255, G = 255, B = 255 });
                        //}
                        //else
                        //{
                        //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 10 }, 1, new Color { R = 0, G = 255, B = 0 });
                        //}

                        var vector = new Vector2(point.X, point.Y);
                        var tooClose = false;
                        if (MapDataService.MapData.BlockWallData != null && MapDataService.MapData.BlockWallData.Any(d => d.Pylons.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), vector) < 16)))
                        {
                            tooClose = true;
                        }

                        if (!tooClose && BuildingService.AreaBuildable(point.X, point.Y, size / 2.0f) && !BuildingService.Blocked(point.X, point.Y, size / 2.0f, 0) && !BuildingService.HasCreep(point.X, point.Y, size / 2.0f))
                        {
                            var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                            var squared = (1 + minimumMineralProximinity + (size / 2f)) * (1 + minimumMineralProximinity + (size / 2f));
                            var clashes = mineralFields.Where(u => Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < squared);

                            if (clashes.Count() == 0)
                            {
                                if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), new Vector2(point.X, point.Y)) <= maxDistance * maxDistance)
                                {
                                    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });
                                    return point;
                                }
                            }
                        }

                        angle += sliceSize;
                    }
                    radius += 1;
                }
            }
            return null;
        }
    }
}
