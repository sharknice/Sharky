using SC2APIProtocol;
using Sharky.MicroControllers;
using Sharky.MicroTasks.Attack;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class DefenseSquadTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        TargetingData TargetingData;

        DefenseService DefenseService;

        IMicroController MicroController;

        ArmySplitter ArmySplitter;

        float lastFrameTime;

        public bool OnlyDefendMain { get; set; }

        List<UnitCommander> WorkerDefenders { get; set; }

        public List<DesiredUnitsClaim> DesiredUnitsClaims { get; set; }

        public DefenseSquadTask(ActiveUnitData activeUnitData, TargetingData targetingData, 
            DefenseService defenseService, 
            IMicroController microController,
            ArmySplitter armySplitter,
            List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = true)
        {
            ActiveUnitData = activeUnitData;
            TargetingData = targetingData;

            DefenseService = defenseService;

            MicroController = microController;

            ArmySplitter = armySplitter;

            DesiredUnitsClaims = desiredUnitsClaims;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
            WorkerDefenders = new List<UnitCommander>();

            OnlyDefendMain = false;

            Enabled = true;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    var unitType = commander.Value.UnitCalculation.Unit.UnitType;
                    foreach (var desiredUnitClaim in DesiredUnitsClaims)
                    {
                        if ((uint)desiredUnitClaim.UnitType == unitType && !commander.Value.UnitCalculation.Unit.IsHallucination && UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType) < desiredUnitClaim.Count)
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Defend;
                            UnitCommanders.Add(commander.Value);
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (lastFrameTime > 5)
            {
                lastFrameTime = 0;
                return actions;
            }
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var structures = ActiveUnitData.SelfUnits.Where(u => u.Value.Attributes.Contains(Attribute.Structure));
            if (OnlyDefendMain)
            {
                var vector = new Vector2(TargetingData.MainDefensePoint.X, TargetingData.MainDefensePoint.Y);
                structures = structures.Where(u => Vector2.DistanceSquared(u.Value.Position, vector) < 400);
            }
            var attackingEnemies = structures.SelectMany(u => u.Value.NearbyEnemies).Distinct().Where(e => ActiveUnitData.EnemyUnits.ContainsKey(e.Unit.Tag));
            if (attackingEnemies.Count() > 0)
            {
                if (UnitCommanders.Count() == 0)
                {
                    actions.AddRange(DefendWithWorkers(attackingEnemies, frame));
                }
                else
                {
                    StopDefendingWithWorkers();
                }

                foreach (var commander in UnitCommanders)
                {
                    if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat)
                    {
                        commander.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.Attack;
                    }
                }
                actions.AddRange(ArmySplitter.SplitArmy(frame, attackingEnemies, TargetingData.MainDefensePoint, UnitCommanders, true));
                stopwatch.Stop();
                lastFrameTime = stopwatch.ElapsedMilliseconds;
                return actions;
            }
            else
            {
                var defensePoint = TargetingData.ForwardDefensePoint;
                if (OnlyDefendMain)
                {
                    defensePoint = TargetingData.MainDefensePoint;
                }
                actions = MicroController.Retreat(UnitCommanders, defensePoint, null, frame);
            }
            StopDefendingWithWorkers();
            stopwatch.Stop();
            lastFrameTime = stopwatch.ElapsedMilliseconds;
            return actions;
        }

        private void StopDefendingWithWorkers()
        {
            if (WorkerDefenders.Count() > 0)
            {
                foreach (var defender in WorkerDefenders)
                {
                    defender.UnitRole = UnitRole.None;
                }
                WorkerDefenders.Clear();
            }
        }

        private List<Action> DefendWithWorkers(IEnumerable<UnitCalculation> attackingEnemies, int frame)
        {
            var bunkersInProgress = attackingEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && (e.Unit.BuildProgress < 1 || e.Unit.Health < 100));
            if (bunkersInProgress.Count() > 0)
            {
                var bunker = bunkersInProgress.OrderByDescending(u => u.Unit.BuildProgress).FirstOrDefault();
                // attack with 8 workers
                if (WorkerDefenders.Count() == 0)
                {
                    var closestWokrers = ActiveUnitData.Commanders.Where(u => u.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && u.Value.UnitRole == UnitRole.Minerals).OrderBy(d => Vector2.DistanceSquared(d.Value.UnitCalculation.Position, bunker.Position)).Take(7 + bunker.NearbyAllies.Count());
                    WorkerDefenders.AddRange(closestWokrers.Select(c => c.Value));
                    foreach (var worker in WorkerDefenders)
                    {
                        worker.UnitRole = UnitRole.Attack;
                    }
                }
                foreach (var worker in WorkerDefenders.Where(w => w.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || w.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat))
                {
                    worker.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.KillBunker;
                }

                return MicroController.Attack(WorkerDefenders, new Point2D { X = bunker.Position.X, Y = bunker.Position.Y }, TargetingData.ForwardDefensePoint, TargetingData.MainDefensePoint, frame);
            }

            return new List<SC2APIProtocol.Action>();
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                WorkerDefenders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
        }
    }
}
