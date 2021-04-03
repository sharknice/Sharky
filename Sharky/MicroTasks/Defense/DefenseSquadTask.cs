using SC2APIProtocol;
using Sharky.Managers;
using Sharky.MicroControllers;
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

        float lastFrameTime;

        public List<DesiredUnitsClaim> DesiredUnitsClaims { get; set; }

        public DefenseSquadTask(ActiveUnitData activeUnitData, TargetingData targetingData, DefenseService defenseService, IMicroController microController, List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = true)
        {
            ActiveUnitData = activeUnitData;
            TargetingData = targetingData;
            DefenseService = defenseService;
            MicroController = microController;

            DesiredUnitsClaims = desiredUnitsClaims;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();

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
                        if ((uint)desiredUnitClaim.UnitType == unitType && UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType) < desiredUnitClaim.Count)
                        {
                            commander.Value.Claimed = true;
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

            var attackingEnemies = ActiveUnitData.SelfUnits.Where(u => u.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter) || u.Value.UnitClassifications.Contains(UnitClassification.ProductionStructure)).SelectMany(u => u.Value.NearbyEnemies).Distinct().Where(e => ActiveUnitData.EnemyUnits.ContainsKey(e.Unit.Tag));
            if (attackingEnemies.Count() > 0)
            {
                foreach (var commander in UnitCommanders)
                {
                    if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat)
                    {
                        commander.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.Attack;
                    }
                }
                actions = SplitDefenders(frame, attackingEnemies);
                stopwatch.Stop();
                lastFrameTime = stopwatch.ElapsedMilliseconds;
                return actions;
            }
            else
            {
                actions = MicroController.Attack(UnitCommanders, TargetingData.MainDefensePoint, TargetingData.ForwardDefensePoint, TargetingData.MainDefensePoint, frame);
            }
            stopwatch.Stop();
            lastFrameTime = stopwatch.ElapsedMilliseconds;
            return actions;

            return new List<SC2APIProtocol.Action>();
        }

        private List<Action> SplitDefenders(int frame, IEnumerable<UnitCalculation> attackingEnemies)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var enemyGroups = DefenseService.GetEnemyGroups(attackingEnemies);
            var availableCommanders = UnitCommanders.ToList();
            foreach (var enemyGroup in enemyGroups)
            {
                var selfGroup = DefenseService.GetDefenseGroup(enemyGroup, availableCommanders);
                if (selfGroup.Count() > 0)
                {
                    availableCommanders.RemoveAll(a => selfGroup.Any(s => a.UnitCalculation.Unit.Tag == s.UnitCalculation.Unit.Tag));

                    var defensePoint = new Point2D { X = enemyGroup.FirstOrDefault().Unit.Pos.X, Y = enemyGroup.FirstOrDefault().Unit.Pos.Y };
                    actions.AddRange(MicroController.Attack(selfGroup, defensePoint, TargetingData.MainDefensePoint, null, frame));
                }
            }

            if (availableCommanders.Count() > 0)
            {
                var enemyGroup = enemyGroups.FirstOrDefault();
                if (enemyGroup != null)
                {
                    var defensePoint = new Point2D { X = enemyGroup.FirstOrDefault().Unit.Pos.X, Y = enemyGroup.FirstOrDefault().Unit.Pos.Y };
                    actions.AddRange(MicroController.Attack(availableCommanders, defensePoint, TargetingData.MainDefensePoint, null, frame));
                }
            }

            return actions;
        }
    }
}
