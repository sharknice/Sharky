using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Managers
{
    public class TargetingManager : ITargetingManager
    {
        public Point2D AttackPoint { get; private set; }
        public Point2D MainDefensePoint { get; private set; }
        public Point2D ForwardDefensePoint { get; private set; }
        public Point2D SelfMainBasePoint { get; private set; }
        public Point2D EnemyMainBasePoint { get; private set; }

        ActiveUnitData ActiveUnitData;
        UnitDataManager UnitDataManager;
        MapDataService MapDataService;
        IBaseManager BaseManager;
        MacroData MacroData;

        int baseCount;

        public TargetingManager(ActiveUnitData activeUnitData, UnitDataManager unitDataManager, MapDataService mapDataService, IBaseManager baseManager, MacroData macroData)
        {
            ActiveUnitData = activeUnitData;
            UnitDataManager = unitDataManager;
            MapDataService = mapDataService;
            BaseManager = baseManager;
            MacroData = macroData;

            baseCount = 0;
        }

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var location in gameInfo.StartRaw.StartLocations)
            {
                AttackPoint = location;
                EnemyMainBasePoint = location;
            }
            foreach (var unit in observation.Observation.RawData.Units.Where(u => u.Alliance == Alliance.Self && UnitDataManager.UnitData[(UnitTypes)u.UnitType].Attributes.Contains(SC2APIProtocol.Attribute.Structure)))
            {
                MainDefensePoint = new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
                ForwardDefensePoint = new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
                SelfMainBasePoint = new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
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
            if (baseCount != BaseManager.SelfBases.Count())
            {
                var ordered = BaseManager.SelfBases.OrderBy(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), new Vector2(AttackPoint.X, AttackPoint.Y)));
                var closestBase = ordered.FirstOrDefault();
                if (closestBase != null)
                {
                    MainDefensePoint = closestBase.MineralLineLocation;
                    ForwardDefensePoint = closestBase.Location; // TODO: look for tops of ramps, set defensive point there
                }
                var farthestBase = ordered.LastOrDefault();
                if (farthestBase != null)
                {
                    SelfMainBasePoint = farthestBase.Location;
                }
                baseCount = BaseManager.SelfBases.Count();
            }
            foreach (var task in MacroData.Proxies)
            {
                if (task.Value.Enabled)
                {
                    ForwardDefensePoint = task.Value.Location;
                }
            }
        }

        public Point2D GetAttackPoint(Point2D armyPoint)
        {
            var enemyBuilding = ActiveUnitData.EnemyUnits.Where(e => e.Value.UnitTypeData.Attributes.Contains(SC2APIProtocol.Attribute.Structure)).OrderBy(e => Vector2.DistanceSquared(new Vector2(e.Value.Unit.Pos.X, e.Value.Unit.Pos.Y), new Vector2(armyPoint.X, armyPoint.Y))).FirstOrDefault().Value;
            if (enemyBuilding != null)
            {
                return new Point2D { X = enemyBuilding.Unit.Pos.X, Y = enemyBuilding.Unit.Pos.Y };
            }

            // TODO: if we have vision of AttackPoint find a new AttackPoint, choose a random base location
            if (MapDataService.SelfVisible(AttackPoint))
            {
                var bases = BaseManager.BaseLocations.Where(b => !MapDataService.SelfVisible(b.Location));
                if (bases.Count() == 0)
                {
                    // TODO: find a random spot on the map and check there
                    AttackPoint = new Point2D { X = new Random().Next(0, MapDataService.MapData.MapWidth), Y = new Random().Next(0, MapDataService.MapData.MapHeight) };
                }
                else
                {
                    AttackPoint = bases.ToList()[new Random().Next(0, bases.Count() - 1)].Location;
                }
            }
            return AttackPoint;
        }
    }
}
