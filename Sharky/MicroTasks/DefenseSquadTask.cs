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
        UnitManager UnitManager;
        TargetingManager TargetingManager;
        DefenseService DefenseService;
        IMicroController MicroController;
        bool Enabled { get; set; }

        float lastFrameTime;

        public List<DesiredUnitsClaim> DesiredUnitsClaims { get; set; }

        public DefenseSquadTask(UnitManager unitManager, TargetingManager targetingManager, DefenseService defenseService, IMicroController microController, List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = true)
        {
            UnitManager = unitManager;
            TargetingManager = targetingManager;
            DefenseService = defenseService;
            MicroController = microController;

            DesiredUnitsClaims = desiredUnitsClaims;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
        }

        public void Enable()
        {
            Enabled = true;
        }

        public void Disable()
        {
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
            }
            UnitCommanders = new List<UnitCommander>();

            Enabled = false;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (Enabled)
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
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            if (Enabled)
            {
                var actions = new List<SC2APIProtocol.Action>();

                if (lastFrameTime > 5)
                {
                    lastFrameTime = 0;
                    return actions;
                }
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var attackingEnemies = UnitManager.SelfUnits.Where(u => u.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter) || u.Value.UnitClassifications.Contains(UnitClassification.ProductionStructure)).SelectMany(u => u.Value.NearbyEnemies).Distinct();
                if (attackingEnemies.Count() > 0)
                {
                    actions = SplitDefenders(frame, attackingEnemies);
                    stopwatch.Stop();
                    lastFrameTime = stopwatch.ElapsedMilliseconds;
                    return actions;
                }
                actions = MicroController.Retreat(UnitCommanders, TargetingManager.DefensePoint, TargetingManager.DefensePoint, frame);
                stopwatch.Stop();
                lastFrameTime = stopwatch.ElapsedMilliseconds;
                return actions;
            }

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

                    var groupVectors = selfGroup.Select(u => new Vector2(u.UnitCalculation.Unit.Pos.X, u.UnitCalculation.Unit.Pos.Y));
                    var groupPoint = new Point2D { X = groupVectors.Average(v => v.X), Y = groupVectors.Average(v => v.Y) };
                    var defensePoint = new Point2D { X = enemyGroup.FirstOrDefault().Unit.Pos.X, Y = enemyGroup.FirstOrDefault().Unit.Pos.Y };
                    actions.AddRange(MicroController.Attack(selfGroup, defensePoint, TargetingManager.DefensePoint, groupPoint, frame));
                }
            }

            if (availableCommanders.Count() > 0)
            {
                var groupVectors = availableCommanders.Select(u => new Vector2(u.UnitCalculation.Unit.Pos.X, u.UnitCalculation.Unit.Pos.Y));
                var groupPoint = new Point2D { X = groupVectors.Average(v => v.X), Y = groupVectors.Average(v => v.Y) };
                actions.AddRange(MicroController.Attack(availableCommanders, new Point2D { X = attackingEnemies.FirstOrDefault().Unit.Pos.X, Y = attackingEnemies.FirstOrDefault().Unit.Pos.Y }, TargetingManager.DefensePoint, groupPoint, frame));
            }

            return actions;
        }
    }
}
