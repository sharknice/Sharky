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
        BaseData BaseData;

        public BuildingService(MapData mapData, ActiveUnitData activeUnitData, TargetingData targetingData, BaseData baseData)
        {
            MapData = mapData;
            ActiveUnitData = activeUnitData;
            TargetingData = targetingData;
            BaseData = baseData;
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

        public bool SameHeight(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            var height = MapData.Map[(int)x][(int)y].TerrainHeight;
            return (height == MapData.Map[(int)x][(int)y + (int)radius].TerrainHeight) && (height == MapData.Map[(int)x][(int)y - (int)radius].TerrainHeight)
               && (height ==  MapData.Map[(int)x + (int)radius][(int)y].TerrainHeight) && (height == MapData.Map[(int)x + (int)radius][(int)y + (int)radius].TerrainHeight) && (height == MapData.Map[(int)x + (int)radius][(int)y - (int)radius].TerrainHeight)
               && (height == MapData.Map[(int)x - (int)radius][(int)y].TerrainHeight) && (height == MapData.Map[(int)x - (int)radius][(int)y + (int)radius].TerrainHeight) && (height == MapData.Map[(int)x - (int)radius][(int)y - (int)radius].TerrainHeight);
        }

        public bool Blocked(float x, float y, float radius, float padding = .5f, ulong tag = 0)
        {
            if (ActiveUnitData.NeutralUnits.Any(u => Vector2.DistanceSquared(new Vector2(x, y), u.Value.Position) < (u.Value.Unit.Radius + padding + radius) * (u.Value.Unit.Radius + padding + radius)))
            {
                return true;
            }

            if (ActiveUnitData.EnemyUnits.Any(u => !u.Value.Unit.IsFlying && Vector2.DistanceSquared(new Vector2(x, y), u.Value.Position) < (u.Value.Unit.Radius + padding + radius) * (u.Value.Unit.Radius + padding + radius)))
            {
                return true;
            }

            if (ActiveUnitData.Commanders.Any(c => c.Key != tag && ((c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !c.Value.UnitCalculation.Unit.IsFlying) || c.Value.UnitCalculation.Unit.BuildProgress < 1) && Vector2.DistanceSquared(new Vector2(x, y), c.Value.UnitCalculation.Position) < (c.Value.UnitCalculation.Unit.Radius + padding + radius) * (c.Value.UnitCalculation.Unit.Radius + padding + radius)))
            {
                return true;
            }

            return false;
        }

        public bool BlocksResourceCenter(float x, float y, float radius)
        {
            if (BaseData.BaseLocations.Any(b => Vector2.DistanceSquared(new Vector2(x, y), new Vector2(b.Location.X, b.Location.Y)) < (4  + radius) * (4 + radius)))
            {
                return true;
            }
            return false;
        }

        public bool BlocksPath(float x, float y, float unitRadius)
        {
            var radius = unitRadius + 2f;
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return true;
            }
            var blocked = MapData.Map[(int)x][(int)y].Walkable && MapData.Map[(int)x][(int)y + (int)radius].Walkable && MapData.Map[(int)x][(int)y - (int)radius].Walkable
                && MapData.Map[(int)x + (int)radius][(int)y].Walkable && MapData.Map[(int)x + (int)radius][(int)y + (int)radius].Walkable && MapData.Map[(int)x + (int)radius][(int)y - (int)radius].Walkable
                && MapData.Map[(int)x - (int)radius][(int)y].Walkable && MapData.Map[(int)x - (int)radius][(int)y + (int)radius].Walkable && MapData.Map[(int)x - (int)radius][(int)y - (int)radius].Walkable;

            if (blocked) { return true; }
            return !SameHeight(x, y, radius);
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

        public bool BlocksGas(float x, float y, float radius)
        {
            if (BaseData.BaseLocations.Any(b => b.VespeneGeysers.Any(g => Vector2.DistanceSquared(new Vector2(x, y), new Vector2(g.Pos.X, g.Pos.Y)) < (4 + radius) * (4 + radius))))
            {
                return true;
            }
            return false;
        }
    }
}
