using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Managers
{
    public class TargetingManager : IManager
    {
        ActiveUnitData ActiveUnitData;
        UnitDataManager UnitDataManager;
        MapDataService MapDataService;
        BaseData BaseData;
        MacroData MacroData;
        TargetingData TargetingData;

        int baseCount;

        public TargetingManager(ActiveUnitData activeUnitData, UnitDataManager unitDataManager, MapDataService mapDataService, BaseData baseData, MacroData macroData, TargetingData targetingData)
        {
            ActiveUnitData = activeUnitData;
            UnitDataManager = unitDataManager;
            MapDataService = mapDataService;
            BaseData = baseData;
            MacroData = macroData;
            TargetingData = targetingData;

            baseCount = 0;
        }

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var location in gameInfo.StartRaw.StartLocations)
            {
                TargetingData.AttackPoint = location;
                TargetingData.EnemyMainBasePoint = location;
            }
            foreach (var unit in observation.Observation.RawData.Units.Where(u => u.Alliance == Alliance.Self && UnitDataManager.UnitData[(UnitTypes)u.UnitType].Attributes.Contains(SC2APIProtocol.Attribute.Structure)))
            {
                TargetingData.MainDefensePoint = new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
                TargetingData.ForwardDefensePoint = new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
                TargetingData.SelfMainBasePoint = new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
                return;
            }
        }

        public void OnEnd(ResponseObservation observation, Result result)
        {
        }

        public IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            UpdateDefensePoint();

            return new List<SC2APIProtocol.Action>();
        }

        void UpdateDefensePoint()
        {
            if (baseCount != BaseData.SelfBases.Count())
            {
                var ordered = BaseData.SelfBases.OrderBy(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y)));
                var closestBase = ordered.FirstOrDefault();
                if (closestBase != null)
                {
                    TargetingData.MainDefensePoint = closestBase.MineralLineLocation;
                    TargetingData.ForwardDefensePoint = closestBase.Location; // TODO: look for tops of ramps, set defensive point there
                }
                var farthestBase = ordered.LastOrDefault();
                if (farthestBase != null)
                {
                    TargetingData.SelfMainBasePoint = farthestBase.Location;
                }
                baseCount = BaseData.SelfBases.Count();
            }
            foreach (var task in MacroData.Proxies)
            {
                if (task.Value.Enabled)
                {
                    TargetingData.ForwardDefensePoint = task.Value.Location;
                }
            }
        }
    }
}
