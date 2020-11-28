using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Pathing;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class WarpInPlacement : IBuildingPlacement
    {
        IUnitManager UnitManager;
        DebugManager DebugManager;
        MapData MapData;

        public WarpInPlacement(IUnitManager unitManager, DebugManager debugManager, MapData mapData)
        {
            UnitManager = unitManager;
            DebugManager = debugManager;
            MapData = mapData;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreMineralProximity = true, float maxDistance = 50)
        {
            var powerSources = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING && c.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(new Vector2(c.UnitCalculation.Unit.Pos.X, c.UnitCalculation.Unit.Pos.Y), new Vector2(target.X, target.Y)));
            foreach (var powerSource in powerSources)
            {
                var x = powerSource.UnitCalculation.Unit.Pos.X;
                var y = powerSource.UnitCalculation.Unit.Pos.Y;
                var sourceRadius = 7f;
                if (powerSource.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING)
                {
                    sourceRadius = 5f;
                }

                var radius = 1 + (size / 2f);
                var powerRadius = sourceRadius - (size / 2f);

                // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than powerRadius
                while (radius < powerRadius)
                {
                    var fullCircle = Math.PI * 2;
                    var sliceSize = fullCircle / 12.0;
                    var angle = 0.0;
                    while (angle + (sliceSize / 2) < fullCircle)
                    {
                        var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                        if (AreaPlaceable(point.X, point.Y, size / 2.0f) && !Blocked(point.X, point.Y, size / 2.0f))
                        {
                            DebugManager.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });
                            return point;
                        }

                        angle += sliceSize;
                    }
                    radius += 1;
                }
            }
            return null;
        }

        private bool AreaPlaceable(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x][(int)y].Walkable && MapData.Map[(int)x][(int)y + (int)radius].Walkable && MapData.Map[(int)x][(int)y - (int)radius].Walkable
                && MapData.Map[(int)x + (int)radius][(int)y].Walkable && MapData.Map[(int)x + (int)radius][(int)y + (int)radius].Walkable && MapData.Map[(int)x + (int)radius][(int)y - (int)radius].Walkable
                && MapData.Map[(int)x - (int)radius][(int)y].Walkable && MapData.Map[(int)x - (int)radius][(int)y + (int)radius].Walkable && MapData.Map[(int)x - (int)radius][(int)y - (int)radius].Walkable; 
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

            if (UnitManager.SelfUnits.Any(u => !u.Value.Unit.IsFlying && Vector2.DistanceSquared(new Vector2(x, y), new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y)) < (u.Value.Unit.Radius + radius) * (u.Value.Unit.Radius + radius)))
            {
                return true;
            }

            return false;
        }
    }
}
