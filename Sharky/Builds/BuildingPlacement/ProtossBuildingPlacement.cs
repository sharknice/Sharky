using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Pathing;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class ProtossBuildingPlacement : IBuildingPlacement
    {
        UnitManager UnitManager;
        UnitDataManager UnitDataManager;
        DebugManager DebugManager;
        MapData MapData;

        public ProtossBuildingPlacement(UnitManager unitManager, UnitDataManager unitDataManager, DebugManager debugManager, MapData mapData)
        {
            UnitManager = unitManager;
            UnitDataManager = unitDataManager;
            DebugManager = debugManager;
            MapData = mapData;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size)
        {
            if (unitType == UnitTypes.PROTOSS_PYLON)
            {
                return FindPylonPlacement(target, 50);
            }
            else
            {
                return FindProductionPlacement(target, size, 50);
            }
        }

        public Point2D FindPylonPlacement(Point2D reference, float maxDistance, float minimumMineralProximinity = 4)
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
                    //DebugManager.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });

                    if (AreaBuildable(point.X, point.Y, 1) && !Blocked(point.X, point.Y, 1))
                    {
                        var mineralFields = UnitManager.NeutralUnits.Where(u => UnitDataManager.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                        var clashes = mineralFields.Where(u => Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(point.X, point.Y)) < (minimumMineralProximinity * minimumMineralProximinity));

                        if (clashes.Count() == 0)
                        {
                            return point;
                        }
                    }
                    angle += sliceSize;
                }
                radius += 1;
            }

            return null;
        }

        public Point2D FindProductionPlacement(Point2D target, float size, float maxDistance, float minimumMineralProximinity = 4)
        {
            var powerSources = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(new Vector2(c.UnitCalculation.Unit.Pos.X, c.UnitCalculation.Unit.Pos.Y), new Vector2(target.X, target.Y)));
            foreach (var powerSource in powerSources)
            {
                var x = powerSource.UnitCalculation.Unit.Pos.X;
                var y = powerSource.UnitCalculation.Unit.Pos.Y;
                var radius = size / 2f;
                var powerRadius = 7 - (size / 2f);

                // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than powerRadius
                while (radius < powerRadius)
                {
                    var fullCircle = Math.PI * 2;
                    var sliceSize = fullCircle / 12.0;
                    var angle = 0.0;
                    while (angle + (sliceSize / 2) < fullCircle)
                    {
                        var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                        if (AreaBuildable(point.X, point.Y, size / 2.0f) && !Blocked(point.X, point.Y, size / 2.0f))
                        {
                            var mineralFields = UnitManager.NeutralUnits.Where(u => UnitDataManager.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                            var clashes = mineralFields.Where(u => Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(point.X, point.Y)) < (minimumMineralProximinity * minimumMineralProximinity));

                            if (clashes.Count() == 0)
                            {
                                DebugManager.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });
                                return point;
                            }
                        }

                        angle += sliceSize;
                    }
                    radius += 1;
                }
            }
            return null;
        }

        private bool AreaBuildable(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x][(int)y].CurrentlyBuildable && MapData.Map[(int)x][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x][(int)y - (int)radius].CurrentlyBuildable
                && MapData.Map[(int)x + (int)radius][(int)y].CurrentlyBuildable && MapData.Map[(int)x + (int)radius][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x + (int)radius][(int)y - (int)radius].CurrentlyBuildable
                && MapData.Map[(int)x - (int)radius][(int)y].CurrentlyBuildable && MapData.Map[(int)x - (int)radius][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x - (int)radius][(int)y - (int)radius].CurrentlyBuildable; 
        }

        private bool Blocked(float x, float y, float radius)
        {
            if (UnitManager.NeutralUnits.Any(u => Vector2.DistanceSquared(new Vector2(x, y), new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y)) < (u.Value.Unit.Radius + radius) * (u.Value.Unit.Radius + radius)))
            {
                return true;
            }

            if (UnitManager.EnemyUnits.Any(u => !u.Value.Unit.IsFlying && Vector2.DistanceSquared(new Vector2(x, y), new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y)) < (u.Value.Unit.Radius + radius) * (u.Value.Unit.Radius + radius)))
            {
                return true;
            }

            if (UnitManager.Commanders.Any(c => c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(new Vector2(x, y), new Vector2(c.Value.UnitCalculation.Unit.Pos.X, c.Value.UnitCalculation.Unit.Pos.Y)) < (c.Value.UnitCalculation.Unit.Radius + radius) * (c.Value.UnitCalculation.Unit.Radius + radius)))
            {
                return true;
            }

            return false;
        }
    }
}
