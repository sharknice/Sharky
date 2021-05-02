using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class BuildingService
    {
        MapData MapData;
        ActiveUnitData ActiveUnitData;
        TargetingData TargetingData;

        public BuildingService(MapData mapData, ActiveUnitData activeUnitData, TargetingData targetingData)
        {
            MapData = mapData;
            ActiveUnitData = activeUnitData;
            TargetingData = targetingData;
        }

        public bool AreaBuildable(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x][(int)y].CurrentlyBuildable && MapData.Map[(int)x][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x][(int)y - (int)radius].CurrentlyBuildable
                && MapData.Map[(int)x + (int)radius][(int)y].CurrentlyBuildable && MapData.Map[(int)x + (int)radius][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x + (int)radius][(int)y - (int)radius].CurrentlyBuildable
                && MapData.Map[(int)x - (int)radius][(int)y].CurrentlyBuildable && MapData.Map[(int)x - (int)radius][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x - (int)radius][(int)y - (int)radius].CurrentlyBuildable;
        }

        public bool Blocked(float x, float y, float radius, float padding = .5f)
        {
            if (ActiveUnitData.NeutralUnits.Any(u => Vector2.DistanceSquared(new Vector2(x, y), u.Value.Position) < (u.Value.Unit.Radius + padding + radius) * (u.Value.Unit.Radius + padding + radius)))
            {
                return true;
            }

            if (ActiveUnitData.EnemyUnits.Any(u => !u.Value.Unit.IsFlying && Vector2.DistanceSquared(new Vector2(x, y), u.Value.Position) < (u.Value.Unit.Radius + padding + radius) * (u.Value.Unit.Radius + padding + radius)))
            {
                return true;
            }

            if (ActiveUnitData.Commanders.Any(c => (c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) || c.Value.UnitCalculation.Unit.BuildProgress < 1) && Vector2.DistanceSquared(new Vector2(x, y), c.Value.UnitCalculation.Position) < (c.Value.UnitCalculation.Unit.Radius + padding + radius) * (c.Value.UnitCalculation.Unit.Radius + padding + radius)))
            {
                return true;
            }

            return false;
        }

        public bool HasCreep(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x][(int)y].HasCreep && MapData.Map[(int)x][(int)y + (int)radius].HasCreep && MapData.Map[(int)x][(int)y - (int)radius].HasCreep
                && MapData.Map[(int)x + (int)radius][(int)y].HasCreep && MapData.Map[(int)x + (int)radius][(int)y + (int)radius].HasCreep && MapData.Map[(int)x + (int)radius][(int)y - (int)radius].HasCreep
                && MapData.Map[(int)x - (int)radius][(int)y].HasCreep && MapData.Map[(int)x - (int)radius][(int)y + (int)radius].HasCreep && MapData.Map[(int)x - (int)radius][(int)y - (int)radius].HasCreep;
        }

        public bool FullyWalled()
        {
            if (TargetingData.ForwardDefenseWallOffPoints == null) { return true; }

            foreach (var point in TargetingData.ForwardDefenseWallOffPoints)
            {
                var vector = new Vector2(point.X, point.Y);
                if (!ActiveUnitData.Commanders.Any(c => c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(vector, c.Value.UnitCalculation.Position) < c.Value.UnitCalculation.Unit.Radius * c.Value.UnitCalculation.Unit.Radius))
                {
                    return false;
                }
            }

            return true;
        }

        public bool PartiallyWalled()
        {
            if (TargetingData.ForwardDefenseWallOffPoints == null) { return true; }

            var gaps = 0;

            foreach (var point in TargetingData.ForwardDefenseWallOffPoints)
            {
                var vector = new Vector2(point.X, point.Y);
                if (!ActiveUnitData.Commanders.Any(c => c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(vector, c.Value.UnitCalculation.Position) < c.Value.UnitCalculation.Unit.Radius * c.Value.UnitCalculation.Unit.Radius))
                {
                    gaps++;
                    if (gaps > 1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public List<Point2D> UnWalledPoints()
        {
            if (TargetingData.ForwardDefenseWallOffPoints == null) { return new List<Point2D>(); }

            var gaps = new List<Point2D>();

            foreach (var point in TargetingData.ForwardDefenseWallOffPoints)
            {
                var vector = new Vector2(point.X, point.Y);
                if (!ActiveUnitData.Commanders.Any(c => c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(vector, c.Value.UnitCalculation.Position) < c.Value.UnitCalculation.Unit.Radius * c.Value.UnitCalculation.Unit.Radius))
                {
                    gaps.Add(point);
                }
            }

            return gaps;
        }
    }
}
