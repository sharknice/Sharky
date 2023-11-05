namespace Sharky.Managers.Protoss
{
    public class RallyPointManager : SharkyManager
    {
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;
        MapData MapData;
        WallService WallService;
        MapDataService MapDataService;
        BuildingService BuildingService;
        AreaService AreaService;

        public RallyPointManager(DefaultSharkyBot defaultSharkyBot)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            MapData = defaultSharkyBot.MapData;
            WallService = defaultSharkyBot.WallService;
            MapDataService = defaultSharkyBot.MapDataService;
            BuildingService = defaultSharkyBot.BuildingService;
            AreaService = defaultSharkyBot.AreaService;
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

                if (CouldGetStuck(spot))
                {
                    spot = GetLocationToAvoidGettingStuck(spot);
                }

                if (spot != null)
                {
                    var action = rallyBuilding.Order((int)observation.Observation.GameLoop, Abilities.RALLY_BUILDING, spot);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }

            return actions;
        }

        bool CouldGetStuck(Point2D spot)
        {
            if (GetValidPoint(spot.X, spot.Y, -1, 3) == null)
            {
                return true;
            }
            
            return false;
        }

        Point2D GetValidPoint(float x, float y, int baseHeight, float size = 2f)
        {
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight && (baseHeight == -1 || MapDataService.MapHeight((int)x, (int)y) == baseHeight))
            {
                if (BuildingService.AreaBuildable(x, y, size / 2.0f))
                {
                    if (!BuildingService.Blocked(x, y, 1, 0f))
                    {
                        return new Point2D { X = x, Y = y };
                    }
                }
            }

            return null;
        }

        Point2D GetLocationToAvoidGettingStuck(Point2D buildSpot)
        {
            var spots = AreaService.GetTargetArea(buildSpot, 4).Where(p => GetValidPoint(p.X, p.Y, -1, 3) != null);
            var gatewaySpots = spots.Where(s => Vector2.Distance(s.ToVector2(), buildSpot.ToVector2()) > 3).OrderBy(s => Vector2.Distance(s.ToVector2(), buildSpot.ToVector2()));
            return gatewaySpots.FirstOrDefault();
        }
    }
}
