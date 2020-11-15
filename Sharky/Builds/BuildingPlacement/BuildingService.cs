using Sharky.Managers;
using Sharky.Pathing;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class BuildingService
    {
        MapData MapData;
        UnitManager UnitManager;

        public BuildingService(MapData mapData, UnitManager unitManager)
        {
            MapData = mapData;
            UnitManager = unitManager;
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

        public bool Blocked(float x, float y, float radius)
        {
            if (UnitManager.NeutralUnits.Any(u => Vector2.DistanceSquared(new Vector2(x, y), new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y)) < (u.Value.Unit.Radius + .5 + radius) * (u.Value.Unit.Radius + .5 + radius)))
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
