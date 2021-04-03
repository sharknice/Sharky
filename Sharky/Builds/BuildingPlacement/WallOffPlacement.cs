using SC2APIProtocol;
using Sharky.Pathing;
using System;
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

        public WallOffPlacement(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, DebugService debugService, MapData mapData, BuildingService buildingService)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            DebugService = debugService;
            MapData = mapData;
            BuildingService = buildingService;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false)
        {
            var mineralProximity = 2;
            if (ignoreResourceProximity) { mineralProximity = 0; };

            if (unitType == UnitTypes.PROTOSS_PYLON)
            {
                return FindPylonPlacement(target, maxDistance, mineralProximity);
            }
            else
            {
                return FindProductionPlacement(target, size, maxDistance, mineralProximity);
            }
        }

        public Point2D FindPylonPlacement(Point2D reference, float maxDistance, float minimumMineralProximinity = 2)
        {
            var x = reference.X;
            var y = reference.Y;
            var radius = 0f;

            // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than maxDistance
            while (radius < maxDistance / 2.0)
            {
                var fullCircle = Math.PI * 2;
                var sliceSize = fullCircle / (16.0 + radius);
                var angle = 0.0;
                while (angle + (sliceSize / 2) < fullCircle)
                {
                    var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                    //DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });

                    //if (!BuildingService.AreaBuildable(point.X, point.Y, 1f))
                    //{
                    //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 }, .5f, new Color { R = 255, G = 0, B = 0 });
                    //}
                    //else if (BuildingService.Blocked(point.X, point.Y, 1.25f, 0))
                    //{
                    //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 }, .5f, new Color { R = 255, G = 255, B = 0 });
                    //}
                    //else if (BuildingService.HasCreep(point.X, point.Y, 1))
                    //{
                    //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 }, .5f, new Color { R = 255, G = 255, B = 255 });
                    //}
                    //else
                    //{
                    //    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 }, .5f, new Color { R = 0, G = 255, B = 0 });
                    //}

                    if (BuildingService.AreaBuildable(point.X, point.Y, 1f) && !BuildingService.Blocked(point.X, point.Y, 1, 0) && !BuildingService.HasCreep(point.X, point.Y, 1))
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
                radius += 0.25f;
            }

            return null;
        }

        public Point2D FindProductionPlacement(Point2D target, float size, float maxDistance, float minimumMineralProximinity = 2)
        {
            var powerSources = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, new Vector2(target.X, target.Y)));
            foreach (var powerSource in powerSources)
            {
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

                        if (BuildingService.AreaBuildable(point.X, point.Y, size / 2.0f) && !BuildingService.Blocked(point.X, point.Y, size / 2.0f) && !BuildingService.HasCreep(point.X, point.Y, size / 2.0f))
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
            var powerSources = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, new Vector2(target.X, target.Y)));
            foreach (var powerSource in powerSources)
            {
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

                        if (BuildingService.AreaBuildable(point.X, point.Y, size / 2.0f) && !BuildingService.Blocked(point.X, point.Y, size / 2.0f, 0) && !BuildingService.HasCreep(point.X, point.Y, size / 2.0f))
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
