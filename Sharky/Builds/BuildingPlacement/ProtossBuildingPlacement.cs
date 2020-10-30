using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class ProtossBuildingPlacement : IBuildingPlacement
    {
        UnitManager UnitManager;

        public ProtossBuildingPlacement(UnitManager unitManager)
        {
            UnitManager = unitManager;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size)
        {
            if (unitType == UnitTypes.PROTOSS_PYLON)
            {
                return FindPylonPlacement(target, 1000);
            }
            else
            {
                return FindProductionPlacement(target, size, 1000);
            }
        }

        public Point2D FindPylonPlacement(Point2D reference, float maxDistance)
        {
            var x = reference.X;
            var y = reference.Y;
            var radius = 1f;

            // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than maxDistance
            while (radius < maxDistance)
            {
                var fullCircle = Math.PI * 2;
                var sliceSize = fullCircle / 12.0;
                var angle = 0.0;
                while (angle + (sliceSize / 2) < fullCircle)
                {
                    var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                    if (AreaBuildable(point.X, point.Y, radius) && !Blocked(point.X, point.Y, radius))
                    {
                        return point;
                    }

                    angle += sliceSize;
                }
                radius += 1;
            }

            return null;
        }

        public Point2D FindProductionPlacement(Point2D target, int size, float maxDistance)
        {
            var powerSources = UnitManager.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(new Vector2(c.UnitCalculation.Unit.Pos.X, c.UnitCalculation.Unit.Pos.Y), new Vector2(target.X, target.Y)));
            foreach (var powerSource in powerSources)
            {
                var x = powerSource.UnitCalculation.Unit.Pos.X;
                var y = powerSource.UnitCalculation.Unit.Pos.Y;
                var radius = size / 2f;
                var powerRadius = 7f;

                // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than powerRadius
                while (radius < powerRadius)
                {
                    var fullCircle = Math.PI * 2;
                    var sliceSize = fullCircle / 12.0;
                    var angle = 0.0;
                    while (angle + (sliceSize / 2) < fullCircle)
                    {
                        var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                        if (AreaBuildable(point.X, point.Y, radius) && !Blocked(point.X, point.Y, radius))
                        {
                            return point;
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
            return true; // TODO: check the map if this grid area is buildable
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
