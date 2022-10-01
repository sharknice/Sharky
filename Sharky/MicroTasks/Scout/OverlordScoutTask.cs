using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.Pathing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    /// <summary>
    /// Primary overlord scouting enemy natural and hiding in safe spot
    /// </summary>
    public class OverlordScoutTask : MicroTask
    {
        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        MapDataService MapDataService;
        DebugService DebugService;
        BaseData BaseData;
        AreaService AreaService;
        FrameToTimeConverter FrameToTimeConverter;
        UnitDataService UnitDataService;
        ActiveUnitData ActiveUnitData;
        EnemyData EnemyData;

        Point2D CurrentScoutTargetPoint;
        Point2D OverlordSafeSpot;

        int lastNatCheckFrame = 0;

        public OverlordScoutTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            MapDataService = defaultSharkyBot.MapDataService;
            DebugService = defaultSharkyBot.DebugService;
            BaseData = defaultSharkyBot.BaseData;
            AreaService = defaultSharkyBot.AreaService;
            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;
            UnitDataService = defaultSharkyBot.UnitDataService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            EnemyData = defaultSharkyBot.EnemyData;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace != Race.Zerg)
                return;

            // Remove morphed overlords
            foreach (var commander in UnitCommanders.Where(commander => commander.UnitCalculation.Unit.UnitType != (int)UnitTypes.ZERG_OVERLORD))
            {
                commander.UnitRole = UnitRole.None;
                commander.Claimed = false;
                Enabled = false;
            }
            UnitCommanders.RemoveAll(commander => commander.UnitRole != UnitRole.Scout);

            if (!Enabled)
            {
                return;
            }

            if (UnitCommanders.Count == 0)
            {
                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (int)UnitTypes.ZERG_OVERLORD)
                    {
                        commander.Value.Claimed = true;
                        commander.Value.UnitRole = UnitRole.Scout;
                        UnitCommanders.Add(commander.Value);

                        return;
                    }
                }
            }
        }

        private void ScoutEnemyNaturalAndHide(int frame)
        {
            if (CurrentScoutTargetPoint == null)
            {
                CurrentScoutTargetPoint = BaseData.EnemyNaturalBase.Location;
            }

            if (OverlordSafeSpot == null)
            {
                OverlordSafeSpot = GetEnemyNaturalOverlordSpot() ?? TargetingData.ForwardDefensePoint;
            }

            var scoutUnit = UnitCommanders.FirstOrDefault()?.UnitCalculation.Unit;

            if (scoutUnit != null)
            {
                bool enemyStructures = ActiveUnitData.EnemyUnits.Values.Any(x => x.UnitTypeData.Attributes.Contains(SC2APIProtocol.Attribute.Structure));

                // If flying to enemy natural and see no buildings, peek to main
                if (EnemyData.EnemyRace == Race.Protoss && CurrentScoutTargetPoint == BaseData.EnemyNaturalBase.Location && MapDataService.SelfVisible(BaseData.EnemyNaturalBase.Location) && !enemyStructures)
                {
                    CurrentScoutTargetPoint = TargetingData.EnemyMainBasePoint;
                    lastNatCheckFrame = frame;
                }
                else if (CurrentScoutTargetPoint == TargetingData.EnemyMainBasePoint)
                {
                    if (enemyStructures || scoutUnit.Health < scoutUnit.HealthMax || MapDataService.SelfVisible(TargetingData.EnemyMainBasePoint) || UnitCommanders.First().UnitCalculation.EnemiesInRangeOf.Any())
                    {
                        CurrentScoutTargetPoint = OverlordSafeSpot;
                        lastNatCheckFrame = frame;
                    }
                }
                else if (CurrentScoutTargetPoint == BaseData.EnemyNaturalBase.Location)
                {
                    // If we see enemy natural or are attacked, get to safety
                    if (scoutUnit.Health < scoutUnit.HealthMax || MapDataService.SelfVisible(BaseData.EnemyNaturalBase.Location) || UnitCommanders.First().UnitCalculation.EnemiesInRangeOf.Any())
                    {
                        CurrentScoutTargetPoint = OverlordSafeSpot;
                        lastNatCheckFrame = frame;
                    }
                }
                else if (CurrentScoutTargetPoint == OverlordSafeSpot)
                {
                    if (frame - lastNatCheckFrame > 400)
                        CurrentScoutTargetPoint = BaseData.EnemyNaturalBase.Location;
                }
            }
        }

        private List<Point2D> FindHighArea(Point2D startingPoint)
        {
            int height = MapDataService.MapHeight(startingPoint);
            var highArea = new List<Point2D>();
            var openSet = new Queue<Point2D>();
            var closedSet = new HashSet<Point2D>();
            openSet.Enqueue(startingPoint);
            int[] deltas = new int[] { 0, 1, 0, -1, 0 };

            while (openSet.Any())
            {
                var point = openSet.Dequeue();
                closedSet.Add(point);

                for (int i = 0; i < 4; i++)
                {
                    var newPoint = new Point2D().Create(point.X + deltas[i], point.Y + deltas[i + 1]);
                    if (MapDataService.MapHeight(newPoint) == height && !closedSet.Contains(newPoint))
                        openSet.Enqueue(newPoint);
                }

                highArea.Add(point);
            }

            return highArea;
        }

        private Point2D GetEnemyNaturalOverlordSpot()
        {
            var naturalBaseHeight = MapDataService.MapHeight(BaseData.EnemyNaturalBase.Location);

            var mainBasePoints = AreaService.GetTargetArea(TargetingData.EnemyMainBasePoint);

            // Get all cells that are higher then natural expansion and are in certain distance from main base area
            var pillarPointNearEnemyNatural = MapDataService.GetCells(BaseData.EnemyNaturalBase.Location.X, BaseData.EnemyNaturalBase.Location.Y, 25)
                .Where(cell => cell.TerrainHeight > naturalBaseHeight)
                .Where(cell => MinDist(new Point2D().Create(cell.X, cell.Y), mainBasePoints) >= 7)
                .OrderByDescending(cell => Vector2.Distance(BaseData.EnemyNaturalBase.Location.ToVector2(), new Vector2(cell.X, cell.Y))).FirstOrDefault();

            if (pillarPointNearEnemyNatural != null)
            {
                var pillarPoint = new Point2D().Create(pillarPointNearEnemyNatural.X, pillarPointNearEnemyNatural.Y);
                var pillarAreaPoints = FindHighArea(pillarPoint);
                //debugPoints.Add(pillarPoint);
                //debugPoints.AddRange(pillarAreaPoints);
                //debugPoints.Add(GetAreaCenter(pillarAreaPoints.Select(x => new Point2D().Create(x.X, x.Y))));
                return GetAreaCenter(pillarAreaPoints.Select(x => new Point2D().Create(x.X, x.Y)));
            }

            return null;
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (EnemyData.SelfRace != Race.Zerg)
                return commands;

            //foreach (var p in debugPoints)
            //    DebugService.DrawSphere(new Point() { X = p.X, Y = p.Y, Z = 11 }, 0.5f, new Color() { R = 255, G = 127, B = 31});

            ScoutEnemyNaturalAndHide(frame);
            if (UnitCommanders.Any() && CurrentScoutTargetPoint != null)
            {
                var action = UnitCommanders.First().Order(frame, Abilities.MOVE, CurrentScoutTargetPoint);
                if (action != null)
                {
                    commands.AddRange(action);
                }
            }

            return commands;
        }

        private float MinDist(Point2D pos, List<Point2D> area)
        {
            if (!area.Any())
                return 0;

            float minDistSqr = Vector2.DistanceSquared(new Vector2(pos.X, pos.Y), new Vector2(area[0].X, area[0].Y));

            foreach (var p in area)
            {
                float distSqr = Vector2.DistanceSquared(new Vector2(pos.X, pos.Y), new Vector2(p.X, p.Y));
                if (distSqr < minDistSqr)
                    minDistSqr = distSqr;
            }

            return (float)Math.Sqrt(minDistSqr);
        }

        private Point2D GetAreaCenter(IEnumerable<Point2D> points)
        {
            if (!points.Any())
                return new Point2D() { X = 0, Y = 0 };

            float sumX = 0;
            float sumY = 0;
            foreach (var p in points)
            {
                sumX += p.X;
                sumY += p.Y;
            }

            return new Point2D() { X = sumX / points.Count(), Y = sumY / points.Count() };
        }
    }
}
