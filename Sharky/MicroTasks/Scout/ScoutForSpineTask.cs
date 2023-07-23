using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.EnemyStrategies.Zerg;
using Sharky.Extensions;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    /// <summary>
    /// Scout for 12pool spine crawler rush
    /// </summary>
    public class ScoutForSpineTask : MicroTask
    {
        TargetingData TargetingData;
        EnemyData EnemyData;
        MicroTaskData MicroTaskData;
        MapData MapData;
        UnitCountService UnitCountService;
        DebugService DebugService;
        BaseData BaseData;

        List<Point2D> ScoutPoints = null;

        public ScoutForSpineTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            EnemyData = defaultSharkyBot.EnemyData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            MapData = defaultSharkyBot.MapData;
            UnitCountService = defaultSharkyBot.UnitCountService;
            DebugService = defaultSharkyBot.DebugService;
            BaseData = defaultSharkyBot.BaseData;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.EnemyRace != Race.Zerg || EnemyData.SelfRace != Race.Zerg)
                return;

            if (UnitCommanders.Any())
                return;

            if (!EnemyData.EnemyStrategies[nameof(ZerglingDroneRush)].Detected && ScoutPoints == null)
                return;

            foreach (var commander in commanders)
            {
                if ((!commander.Value.Claimed || commander.Value.UnitRole == UnitRole.Minerals || commander.Value.UnitRole == UnitRole.None) && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker))
                {
                    MicroTaskData["MiningTask"].StealUnit(commander.Value);

                    commander.Value.Claimed = true;
                    commander.Value.UnitRole = UnitRole.Scout;
                    UnitCommanders.Add(commander.Value);
                    return;
                }
            }
        }

        private void GetScoutPoints(Vector2 position, int frame)
        {
            ScoutPoints = new List<Point2D>();
            // Check ramp
            if (TargetingData.ChokePoints.Good.Any())
                ScoutPoints.Add(TargetingData.ChokePoints.Good.First().Center.ToPoint2D());

            const int radius = 16;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    int cellX = (int)(x + position.X);
                    int cellY = (int)(y + position.Y);
                    if (cellX >= 0 && cellY >= 0 && cellX < MapData.MapWidth && cellY < MapData.MapHeight && MapData.Map[cellX,cellY].HasCreep && (frame - MapData.Map[cellX,cellY].LastFrameVisibility > 22.4f * 5.0f))
                    {
                        ScoutPoints.Add(new Point2D().Create(cellX, cellY));
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (EnemyData.EnemyRace != Race.Zerg)
                return commands;

            // The spine crawler wont start building sooner than about 1:15, so wait a bit and scout
            if (frame < 22.4f * 82)
                return commands;

            if (EnemyData.EnemyRace == Race.Zerg && frame > 22.4f * 80 && Enabled && ScoutPoints == null)
            {
                GetScoutPoints(BaseData.SelfBases.First().Location.ToVector2(), frame);
            }

            foreach (var commander in UnitCommanders)
            {
                if (ScoutPoints == null)
                {
                    GetScoutPoints(BaseData.SelfBases.First().Location.ToVector2(), frame);
                }

                if (!ScoutPoints.Any() || commander.UnitCalculation.EnemiesInRangeOf.Any())
                {
                    Disable();
                    return commands;
                }

                foreach (var p in ScoutPoints)
                    DebugService.DrawSphere(new Point { X = p.X, Y = p.Y, Z = 12 }, 0.5f, new Color() { R = 255, B = 0 });

                Point2D scoutPos = ScoutPoints.Last();

                var action = commander.Order(frame, Abilities.ATTACK, scoutPos);
                if (action != null)
                {
                    commands.AddRange(action);
                }

                if (MapData.Map[(int)scoutPos.X,(int)scoutPos.Y].InSelfVision)
                {
                    ScoutPoints.Remove(scoutPos);
                    ScoutPoints.OrderBy(x => Vector2.DistanceSquared(x.ToVector2(), commander.UnitCalculation.Position));
                }

                if (UnitCountService.EnemyCount(UnitTypes.ZERG_SPINECRAWLER) > 0)
                {
                    Disable();
                }
            }

            return commands;
        }
    }
}
