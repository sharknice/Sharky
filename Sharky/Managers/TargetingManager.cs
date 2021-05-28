using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
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

        ChokePointService ChokePointService;
        ChokePointsService ChokePointsService;
        DebugService DebugService;

        int baseCount;

        Point2D PreviousAttackPoint;
        Point2D PreviousDefensePoint;
        int LastUpdateFrame;

        public TargetingManager(SharkyUnitData sharkyUnitData, BaseData baseData, MacroData macroData, TargetingData targetingData, MapData mapData,
            ChokePointService chokePointService, ChokePointsService chokePointsService, DebugService debugService)
        {
            SharkyUnitData = sharkyUnitData;
            BaseData = baseData;
            MacroData = macroData;
            TargetingData = targetingData;
            MapData = mapData;

            ChokePointService = chokePointService;
            ChokePointsService = chokePointsService;
            DebugService = debugService;

            baseCount = 0;
            LastUpdateFrame = -10000;
            TargetingData.ChokePoints = new ChokePoints();
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

                var wallData = MapData.PartialWallData.FirstOrDefault(b => b.BasePosition.X == unit.Pos.X && b.BasePosition.Y == unit.Pos.Y);
                if (wallData != null && wallData.Door != null) 
                {
                    TargetingData.ForwardDefensePoint = wallData.Door;
                }

                TargetingData.SelfMainBasePoint = new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
                return;
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            UpdateDefensePoint((int)observation.Observation.GameLoop);
            UpdateChokePoints((int)observation.Observation.GameLoop);

            DebugService.DrawSphere(new Point { X = TargetingData.MainDefensePoint.X, Y = TargetingData.MainDefensePoint.Y, Z = 12 }, 2, new Color { R = 0, G = 255, B = 0 });
            DebugService.DrawSphere(new Point { X = TargetingData.ForwardDefensePoint.X, Y = TargetingData.ForwardDefensePoint.Y, Z = 12 }, 2, new Color { R = 0, G = 0, B = 255 });
            DebugService.DrawSphere(new Point { X = TargetingData.AttackPoint.X, Y = TargetingData.AttackPoint.Y, Z = 12 }, 2, new Color { R = 255, G = 0, B = 0 });

            if (TargetingData.ForwardDefenseWallOffPoints != null)
            {
                foreach (var point in TargetingData.ForwardDefenseWallOffPoints)
                {
                    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 }, 1, new Color { R = 100, G = 100, B = 255 });
                }
            }

            return null;
        }

        void UpdateChokePoints(int frame)
        {
            if (frame == 0) //if (frame - LastUpdateFrame > 1500 && (PreviousAttackPoint != TargetingData.AttackPoint || PreviousDefensePoint != TargetingData.ForwardDefensePoint))
            {
                PreviousAttackPoint = TargetingData.AttackPoint;
                PreviousDefensePoint = TargetingData.ForwardDefensePoint;
                TargetingData.ChokePoints = ChokePointsService.GetChokePoints(TargetingData.ForwardDefensePoint, TargetingData.AttackPoint, frame);
                LastUpdateFrame = frame;
            }

            foreach (var chokePoint in TargetingData.ChokePoints.Good)
            {
                DebugService.DrawSphere(new Point { X = chokePoint.Center.X, Y = chokePoint.Center.Y, Z = 12 }, 4, new Color { R = 0, G = 255, B = 0 });
            }
            foreach (var chokePoint in TargetingData.ChokePoints.Neutral)
            {
                DebugService.DrawSphere(new Point { X = chokePoint.Center.X, Y = chokePoint.Center.Y, Z = 12 }, 4, new Color { R = 0, G = 0, B = 255 });
            }
            foreach (var chokePoint in TargetingData.ChokePoints.Bad)
            {
                DebugService.DrawSphere(new Point { X = chokePoint.Center.X, Y = chokePoint.Center.Y, Z = 12 }, 4, new Color { R = 255, G = 0, B = 0 });
            }
        }

        void UpdateDefensePoint(int frame)
        {
            if (baseCount != BaseData.SelfBases.Count())
            {
                var ordered = BaseData.SelfBases.OrderBy(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y)));
                var closestBase = ordered.FirstOrDefault();
                if (closestBase != null)
                {
                    TargetingData.MainDefensePoint = closestBase.MineralLineLocation;
                    var chokePoint = ChokePointService.FindDefensiveChokePoint(new Point2D { X = closestBase.Location.X + 4, Y = closestBase.Location.Y + 4 }, TargetingData.AttackPoint, frame);
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
                        var angle = Math.Atan2(closestBase.Location.Y - TargetingData.EnemyMainBasePoint.Y, TargetingData.EnemyMainBasePoint.X - closestBase.Location.X);
                        TargetingData.ForwardDefensePoint = new Point2D { X = closestBase.Location.X + (float)(6 * Math.Cos(angle)), Y = closestBase.Location.Y - (float)(6 * Math.Sin(angle)) };
                    }
                    var wallData = MapData.PartialWallData.FirstOrDefault(b => b.BasePosition.X == closestBase.Location.X && b.BasePosition.Y == closestBase.Location.Y);
                    if (wallData != null && wallData.Door != null)
                    {
                        TargetingData.ForwardDefensePoint = wallData.Door;
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
    }
}
