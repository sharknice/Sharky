using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Managers
{
    public class TargetingManager : SharkyManager
    {
        SharkyUnitData SharkyUnitData;
        BaseData BaseData;
        MacroData MacroData;
        TargetingData TargetingData;
        MapData MapData;
        ActiveUnitData ActiveUnitData;
        EnemyData EnemyData;

        ChokePointService ChokePointService;
        ChokePointsService ChokePointsService;
        DebugService DebugService;
        WallDataService WallDataService;
        BaseToBasePathingService BaseToBasePathingService;
        AttackPathingService AttackPathingService;

        int baseCount;

        Point2D PreviousAttackPoint;
        Point2D PreviousDefensePoint;
        int LastUpdateFrame;

        public TargetingManager(DefaultSharkyBot defaultSharkyBot)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            BaseData = defaultSharkyBot.BaseData;
            MacroData = defaultSharkyBot.MacroData;
            TargetingData = defaultSharkyBot.TargetingData;
            MapData = defaultSharkyBot.MapData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            EnemyData = defaultSharkyBot.EnemyData;
            BaseToBasePathingService = defaultSharkyBot.BaseToBasePathingService;

            ChokePointService = defaultSharkyBot.ChokePointService;
            ChokePointsService = defaultSharkyBot.ChokePointsService;
            DebugService = defaultSharkyBot.DebugService;
            WallDataService = defaultSharkyBot.WallDataService;
            AttackPathingService = defaultSharkyBot.AttackPathingService;

            baseCount = 0;
            LastUpdateFrame = -10000;
            TargetingData.ChokePoints = new ChokePoints();
            TargetingData.WallOffBasePosition = WallOffBasePosition.Current;
            TargetingData.WallBuildings = new List<UnitCalculation>();
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var location in gameInfo.StartRaw.StartLocations)
            {
                TargetingData.AttackPoint = location;
                TargetingData.EnemyMainBasePoint = location;
            }
            foreach (var unit in observation.Observation.RawData.Units.Where(u => u.Alliance == Alliance.Self && SharkyUnitData.UnitData[(UnitTypes)u.UnitType].Attributes.Contains(SC2APIProtocol.Attribute.Structure)))
            {
                TargetingData.MainDefensePoint = new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };

                var chokePoint = ChokePointService.FindDefensiveChokePoint(new Point2D { X = unit.Pos.X + 4, Y = unit.Pos.Y + 4 }, TargetingData.AttackPoint, 0);
                if (chokePoint != null)
                {
                    TargetingData.ForwardDefensePoint = chokePoint;
                    var chokePoints = ChokePointService.GetEntireChokePoint(chokePoint);
                    if (chokePoints != null)
                    {
                        var wallPoints = ChokePointService.GetWallOffPoints(chokePoints);
                        if (wallPoints != null)
                        {
                            TargetingData.ForwardDefenseWallOffPoints = wallPoints;
                        }
                    }
                }
                else
                {
                    TargetingData.ForwardDefensePoint = new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
                }

                if (MapData.WallData != null)
                {
                    var wallData = MapData.WallData.FirstOrDefault(b => b.BasePosition.X == unit.Pos.X && b.BasePosition.Y == unit.Pos.Y);
                    if (wallData != null && wallData.Door != null)
                    {
                        TargetingData.ForwardDefensePoint = wallData.Door;
                    }
                }

                TargetingData.SelfMainBasePoint = new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };

                var mainVector = Vector2.Normalize(TargetingData.SelfMainBasePoint.ToVector2() - TargetingData.EnemyMainBasePoint.ToVector2()) * 22.0f;
                TargetingData.NaturalFrontScoutPoint = (BaseData.EnemyNaturalBase.Location.ToVector2() + mainVector).ToPoint2D();

                var naturalBaseLocation = BaseData.BaseLocations.Skip(1).Take(1).FirstOrDefault();
                if (naturalBaseLocation != null)
                {
                    TargetingData.NaturalBasePoint = naturalBaseLocation.Location;
                }


                MapData.WallData = WallDataService.GetWallData(gameInfo.MapName);
                MapData.PathData = BaseToBasePathingService.GetBaseToBasePathingData(gameInfo.MapName);

                return;
            }
        }

        

        /// <summary>
        /// Stores enemy army center in TargetingData.EnemyArmyCenter.
        /// </summary>
        private void UpdateEnemyArmyCenter()
        {
            int count = 0;
            TargetingData.EnemyArmyCenter = Vector2.Zero;

            foreach (var enemy in ActiveUnitData.EnemyUnits)
            {
                if (enemy.Value.UnitClassifications.Contains(UnitClassification.ArmyUnit))
                {
                    TargetingData.EnemyArmyCenter += enemy.Value.Position;
                    count++;
                }
            }

            if (count == 0)
            {
                TargetingData.EnemyArmyCenter = BaseData.EnemyBaseLocations.First().Location.ToVector2();
            }
            else
            {
                TargetingData.EnemyArmyCenter = TargetingData.EnemyArmyCenter / count;
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            UpdateDefensePoint((int)observation.Observation.GameLoop);
            UpdateChokePoints((int)observation.Observation.GameLoop);
            UpdateWall((int)observation.Observation.GameLoop);
            UpdateEnemyArmyCenter();

            DebugService.DrawSphere(new Point { X = TargetingData.MainDefensePoint.X, Y = TargetingData.MainDefensePoint.Y, Z = 12 }, 2, new Color { R = 0, G = 255, B = 0 });
            DebugService.DrawSphere(new Point { X = TargetingData.ForwardDefensePoint.X, Y = TargetingData.ForwardDefensePoint.Y, Z = 12 }, 2, new Color { R = 0, G = 0, B = 255 });
            DebugService.DrawSphere(new Point { X = TargetingData.AttackPoint.X, Y = TargetingData.AttackPoint.Y, Z = 12 }, 2, new Color { R = 255, G = 0, B = 0 });

            if (TargetingData.ForwardDefenseWallOffPoints != null)
            {
                var wallPoints = TargetingData.ForwardDefenseWallOffPoints;
                var wallCenter = new Vector2(wallPoints.Sum(p => p.X) / wallPoints.Count(), wallPoints.Sum(p => p.Y) / wallPoints.Count());
                DebugService.DrawSphere(new Point { X = wallCenter.X, Y = wallCenter.Y, Z = 12 }, 1, new Color { R = 1, G = 250, B = 1 });
            }

            foreach (var chokePoint in TargetingData.ChokePoints.Good)
            {
                DebugService.DrawSphere(new Point { X = chokePoint.Center.X, Y = chokePoint.Center.Y, Z = 12 }, 1, new Color { R = 250, G = 250, B = 250 });
            }

            return null;
        }

        void UpdateWall(int frame)
        {
            if (frame > LastUpdateFrame + 50)
            {
                TargetingData.WallBuildings.Clear();

                var buildings = ActiveUnitData.SelfUnits.Values.Where(u => u.Attributes.Contains(SC2APIProtocol.Attribute.Structure));

                if (EnemyData.SelfRace == Race.Protoss && MapData.WallData != null)
                {
                    var wallData = MapData.WallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.SelfMainBasePoint.X && b.BasePosition.Y == TargetingData.SelfMainBasePoint.Y);
                    if (wallData != null)
                    {
                        AddWalls(buildings, wallData);
                    }
                    wallData = MapData.WallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.NaturalBasePoint.X && b.BasePosition.Y == TargetingData.NaturalBasePoint.Y);
                    if (wallData != null)
                    {
                        AddWalls(buildings, wallData);
                    }
                }
                else if (EnemyData.SelfRace == Race.Terran && MapData.WallData != null)
                {
                    var wallData = MapData.WallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.SelfMainBasePoint.X && b.BasePosition.Y == TargetingData.SelfMainBasePoint.Y);
                    if (wallData != null)
                    {
                        AddWalls(buildings, wallData);
                    }
                    wallData = MapData.WallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.NaturalBasePoint.X && b.BasePosition.Y == TargetingData.NaturalBasePoint.Y);
                    if (wallData != null)
                    {
                        AddWalls(buildings, wallData);
                    }
                }

                LastUpdateFrame = frame;
            }
        }

        private void AddWalls(IEnumerable<UnitCalculation> buildings, WallData wallData)
        {
            if (EnemyData.SelfRace == Race.Protoss)
            {
                if (wallData.WallSegments != null)
                {
                    TargetingData.WallBuildings.AddRange(buildings.Where(b => wallData.WallSegments.Any(w => w.Position.X == b.Position.X && w.Position.Y == b.Position.Y)));
                }
                if (wallData.Pylons != null)
                {
                    TargetingData.WallBuildings.AddRange(buildings.Where(b => b.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && wallData.Pylons.Any(w => w.X == b.Position.X && w.Y == b.Position.Y)));
                }
            }
            else if (EnemyData.SelfRace == Race.Terran)
            {
                if (wallData.Bunkers != null)
                {
                    TargetingData.WallBuildings.AddRange(buildings.Where(b => b.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && wallData.Bunkers.Any(w => w.X == b.Position.X && w.Y == b.Position.Y)));
                }
                if (wallData.Depots != null)
                {
                    TargetingData.WallBuildings.AddRange(buildings.Where(b => (b.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOT || b.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED) && wallData.Depots.Any(w => w.X == b.Position.X && w.Y == b.Position.Y)));
                }
                if (wallData.Production != null)
                {
                    TargetingData.WallBuildings.AddRange(buildings.Where(b => wallData.Production.Any(w => w.X == b.Position.X && w.Y == b.Position.Y)));
                }
                if (wallData.ProductionWithAddon != null)
                {
                    TargetingData.WallBuildings.AddRange(buildings.Where(b => wallData.ProductionWithAddon.Any(w => w.X == b.Position.X && w.Y == b.Position.Y)));
                }
            }
        }

        void UpdateChokePoints(int frame)
        {
            if (frame == 0)
            {
                PreviousAttackPoint = TargetingData.AttackPoint;
                PreviousDefensePoint = TargetingData.ForwardDefensePoint;
                TargetingData.ChokePoints = ChokePointsService.GetChokePoints(TargetingData.ForwardDefensePoint, TargetingData.AttackPoint, frame);
            }
        }

        void UpdateDefensePoint(int frame)
        {
            if (baseCount != BaseData.SelfBases.Count())
            {
                var resourceCenters = ActiveUnitData.SelfUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter) && !u.Unit.IsFlying);
                var ordered = BaseData.BaseLocations.Where(b => resourceCenters.Any(r => Vector2.DistanceSquared(r.Position, new Vector2(b.Location.X, b.Location.Y)) < 25)).OrderBy(b => GetDistance(b));
                //var ordered = BaseData.BaseLocations.Where(b => resourceCenters.Any(r => Vector2.DistanceSquared(r.Position, new Vector2(b.Location.X, b.Location.Y)) < 25)).OrderBy(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y)));
                var closestBase = ordered.FirstOrDefault();
                if (TargetingData.WallOffBasePosition == WallOffBasePosition.Natural && resourceCenters.Count() <= 2)
                {
                    closestBase = BaseData.BaseLocations.FirstOrDefault(b => b.Location.X == TargetingData.NaturalBasePoint.X && b.Location.Y == TargetingData.NaturalBasePoint.Y);
                }
                if (closestBase != null)
                {
                    TargetingData.MainDefensePoint = closestBase.MineralLineLocation;
                    var chokePoint = ChokePointService.FindDefensiveChokePoint(new Point2D { X = closestBase.Location.X + 4, Y = closestBase.Location.Y + 4 }, TargetingData.AttackPoint, frame);

                    if (chokePoint != null)
                    {
                        if (((TargetingData.NaturalBasePoint != null && closestBase.Location.X == TargetingData.NaturalBasePoint.X && closestBase.Location.Y == TargetingData.NaturalBasePoint.Y) || (TargetingData.SelfMainBasePoint != null && closestBase.Location.X == TargetingData.SelfMainBasePoint.X && closestBase.Location.Y == TargetingData.SelfMainBasePoint.Y)))
                        {
                            TargetingData.ForwardDefensePoint = chokePoint;
                        }
                        else
                        {
                            var angle = Math.Atan2(closestBase.Location.Y - TargetingData.EnemyMainBasePoint.Y, TargetingData.EnemyMainBasePoint.X - closestBase.Location.X);
                            TargetingData.ForwardDefensePoint = new Point2D { X = closestBase.Location.X + (float)(6 * Math.Cos(angle)), Y = closestBase.Location.Y - (float)(6 * Math.Sin(angle)) };
                        }
                        var chokePoints = ChokePointService.GetEntireChokePoint(chokePoint);
                        if (chokePoints != null)
                        {
                            var wallPoints = ChokePointService.GetWallOffPoints(chokePoints);
                            if (wallPoints != null)
                            {
                                TargetingData.ForwardDefenseWallOffPoints = wallPoints;
                            }
                        }
                    }
                    else
                    {
                        var angle = Math.Atan2(closestBase.Location.Y - TargetingData.EnemyMainBasePoint.Y, TargetingData.EnemyMainBasePoint.X - closestBase.Location.X);
                        TargetingData.ForwardDefensePoint = new Point2D { X = closestBase.Location.X + (float)(6 * Math.Cos(angle)), Y = closestBase.Location.Y - (float)(6 * Math.Sin(angle)) };
                    }

                    if (MapData.WallData != null)
                    {
                        var wallData = MapData.WallData.FirstOrDefault(b => b.BasePosition.X == closestBase.Location.X && b.BasePosition.Y == closestBase.Location.Y);
                        if (wallData != null && wallData.Door != null)
                        {
                            var angle = Math.Atan2(wallData.Door.Y - TargetingData.SelfMainBasePoint.Y, TargetingData.SelfMainBasePoint.X - wallData.Door.X);
                            TargetingData.ForwardDefensePoint = new Point2D { X = wallData.Door.X + (float)(2 * Math.Cos(angle)), Y = wallData.Door.Y - (float)(2 * Math.Sin(angle)) };
                        }
                    }
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

        float GetDistance(BaseLocation baseLocation)
        {
            // TODO: make sure this works and fixes the Stargazers trying to defend the pocket instead of front base
            if (MapData.PathData.Count > 1)
            {
                var path = AttackPathingService.GetNearestPath(baseLocation.Location.ToVector2(), TargetingData.AttackPoint.ToVector2());
                if (path?.Path == null || path.Path.Count < 2)
                {
                    path = AttackPathingService.GetNearestPath(baseLocation.Location.ToVector2(), TargetingData.EnemyMainBasePoint.ToVector2());
                    if (path?.Path == null || path.Path.Count < 2)
                    {
                        return 10000;
                    }
                }
                return path.Path.Count;
            }
            return Vector2.DistanceSquared(baseLocation.Location.ToVector2(), TargetingData.AttackPoint.ToVector2());
        }
    }
}
