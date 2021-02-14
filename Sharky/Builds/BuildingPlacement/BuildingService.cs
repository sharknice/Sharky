using Sharky.Pathing;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class BuildingService
    {
        MapData MapData;
        ActiveUnitData ActiveUnitData;

        public BuildingService(MapData mapData, ActiveUnitData activeUnitData)
        {
            MapData = mapData;
            ActiveUnitData = activeUnitData;
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
            if (ActiveUnitData.NeutralUnits.Any(u => Vector2.DistanceSquared(new Vector2(x, y), new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y)) < (u.Value.Unit.Radius + padding + radius) * (u.Value.Unit.Radius + padding + radius)))
            {
                return true;
            }

            if (ActiveUnitData.EnemyUnits.Any(u => !u.Value.Unit.IsFlying && Vector2.DistanceSquared(new Vector2(x, y), new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y)) < (u.Value.Unit.Radius + padding + radius) * (u.Value.Unit.Radius + padding + radius)))
            {
                return true;
            }

            if (ActiveUnitData.Commanders.Any(c => (c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) || c.Value.UnitCalculation.Unit.BuildProgress < 1) && Vector2.DistanceSquared(new Vector2(x, y), new Vector2(c.Value.UnitCalculation.Unit.Pos.X, c.Value.UnitCalculation.Unit.Pos.Y)) < (c.Value.UnitCalculation.Unit.Radius + padding + radius) * (c.Value.UnitCalculation.Unit.Radius + padding + radius)))
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
    }
}
