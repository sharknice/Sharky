using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers.Protoss
{
    public class RallyPointManager : SharkyManager
    {
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;
        MapData MapData;
        WallService WallService;

        public RallyPointManager(ActiveUnitData activeUnitData, TargetingData targetingData, MapData mapData, WallService wallService)
        {
            ActiveUnitData = activeUnitData;
            TargetingData = targetingData;
            MapData = mapData;
            WallService = wallService;
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var rallyBuilding in ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_GATEWAY && c.UnitCalculation.Unit.BuildProgress == 1 && !c.RallyPointSet))
            {
                rallyBuilding.RallyPointSet = true;

                var spot = TargetingData.MainDefensePoint;

                var baseLocation = WallService.GetBaseLocation();
                if (baseLocation != null)
                {
                    if (MapData != null && MapData.WallData != null)
                    {
                        var data = MapData.WallData.FirstOrDefault(d => d.BasePosition.X == baseLocation.X && d.BasePosition.Y == baseLocation.Y);
                        if (data != null && data.RampCenter != null)
                        {
                            var angle = Math.Atan2(rallyBuilding.UnitCalculation.Position.Y - data.RampCenter.Y, data.RampCenter.X - rallyBuilding.UnitCalculation.Position.X);
                            var x = -2 * Math.Cos(angle);
                            var y = -2 * Math.Sin(angle);
                            spot = new Point2D { X = rallyBuilding.UnitCalculation.Position.X + (float)x, Y = rallyBuilding.UnitCalculation.Position.Y - (float)y };
                        }
                    }
                }

                var action = rallyBuilding.Order((int)observation.Observation.GameLoop, Abilities.RALLY_BUILDING, spot);
                if (action != null)
                {
                    actions.AddRange(action);
                }
            }

            return actions;
        }
    }
}
