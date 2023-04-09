using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.MicroControllers;
using Sharky.MicroTasks.Scout;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Attack
{
    public class AdvancedAttackTask : MicroTask, IAttackTask
    {
        AttackData AttackData;
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;
        MicroTaskData MicroTaskData;
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;

        IMicroController MicroController;

        TargetingService TargetingService;
        EnemyCleanupService EnemyCleanupService;
        UnitCountService UnitCountService;

        ArmySplitter DefenseArmySplitter;
        ArmySplitter AttackArmySplitter;

        public List<UnitTypes> MainAttackers { get; set; }
        public List<UnitCommander> MainUnits { get; set; }
        public List<UnitCommander> SupportUnits { get; set; }

        public Dictionary<string, IAttackSubTask> SubTasks { get; set; }

        List<UnitCalculation> EnemyAttackers { get; set; }

        public AdvancedAttackTask(DefaultSharkyBot defaultSharkyBot, EnemyCleanupService enemyCleanupService, List<UnitTypes> mainAttackerTypes, float priority, bool enabled = true)
        {
            AttackData = defaultSharkyBot.AttackData;
            TargetingData = defaultSharkyBot.TargetingData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            MacroData = defaultSharkyBot.MacroData;
            MicroController = defaultSharkyBot.MicroController;
            TargetingService = defaultSharkyBot.TargetingService;
            UnitCountService = defaultSharkyBot.UnitCountService;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;

            EnemyCleanupService = enemyCleanupService;

            DefenseArmySplitter = new ArmySplitter(defaultSharkyBot);
            AttackArmySplitter = new ArmySplitter(defaultSharkyBot);

            MainAttackers = mainAttackerTypes;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
            MainUnits = new List<UnitCommander>();
            SupportUnits = new List<UnitCommander>();

            EnemyAttackers = new List<UnitCalculation>();

            SubTasks = new Dictionary<string, IAttackSubTask>();
        }

        public override void ResetClaimedUnits()
        {
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
            }
            UnitCommanders = new List<UnitCommander>();
            MainUnits = new List<UnitCommander>();
            SupportUnits = new List<UnitCommander>();
            foreach (var subtask in SubTasks.OrderBy(t => t.Value.Priority))
            {
                subtask.Value.ResetClaimedUnits();
            }
        }

        public override List<UnitCommander> ResetNonEssentialClaims()
        {
            var removals = UnitCommanders.Where(c => c.UnitRole != UnitRole.Leader && !SubTasks.Any(s => s.Value.UnitCommanders.Contains(c))).ToList();

            foreach (var removal in removals)
            {
                UnitCommanders.Remove(removal);
                MainUnits.Remove(removal);
                SupportUnits.Remove(removal);
                removal.UnitRole = UnitRole.None;
                removal.Claimed = false;
            }

            return removals;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
            {
                subTask.Value.ClaimUnits(commanders);
            }

            var desiredScvs = CalculateDesiredScvs();
            var claimedScvs = UnitCommanders.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV);
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    if (commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.ArmyUnit) || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED)
                    {
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                        if (MainAttackers.Count() == 0 && MainAttackers.Contains((UnitTypes)commander.Value.UnitCalculation.Unit.UnitType) && !commander.Value.UnitCalculation.Unit.IsHallucination) // TODO: need to move units over when they change from corruptor to broodlord
                        {
                            commander.Value.UnitRole = UnitRole.Leader;
                            MainUnits.Add(commander.Value);
                        }
                        else
                        {
                            SupportUnits.Add(commander.Value);
                        }
                    }
                    else if (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && claimedScvs < desiredScvs)
                    {
                        commander.Value.Claimed = true;
                        commander.Value.UnitRole = UnitRole.Support;
                        UnitCommanders.Add(commander.Value);
                        SupportUnits.Add(commander.Value);
                        claimedScvs++;
                    }
                }
            }

            ClaimBusyScvs(commanders, desiredScvs, claimedScvs);
            UnclaimScvs(desiredScvs, claimedScvs);

            foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
            {
                subTask.Value.ClaimUnitsFromParent(GetAvailableCommanders());
            }
        }

        void ClaimBusyScvs(ConcurrentDictionary<ulong, UnitCommander> commanders, int desiredScvs, int claimedScvs)
        {
            if (claimedScvs < desiredScvs)
            {
                var targetVector = new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y);
                foreach (var commander in commanders.Where(c => c.Value.Claimed && c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && (c.Value.UnitRole == UnitRole.Minerals || c.Value.UnitRole == UnitRole.None) && c.Value.UnitCalculation.Unit.BuffIds.Count() == 0).OrderBy(c => Vector2.DistanceSquared(c.Value.UnitCalculation.Position, targetVector)))
                {
                    commander.Value.Claimed = true;
                    commander.Value.UnitRole = UnitRole.Support;
                    UnitCommanders.Add(commander.Value);
                    claimedScvs++;
                    if (claimedScvs >= desiredScvs)
                    {
                        return;
                    }
                }
            }
        }

        void UnclaimScvs(int desiredScvs, int claimedScvs)
        {
            if (claimedScvs > desiredScvs || !AttackData.Attacking && !UnitCommanders.Any(c => c.UnitCalculation.Unit.Health < c.UnitCalculation.Unit.HealthMax && c.UnitCalculation.Attributes.Contains(Attribute.Mechanical)) || MacroData.Minerals == 0)
            {
                var scv = UnitCommanders.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV);
                if (scv != null)
                {
                    MainUnits.Remove(scv);
                    SupportUnits.Remove(scv);
                    UnitCommanders.Remove(scv);
                    scv.Claimed = false;
                    scv.UnitRole = UnitRole.None;
                }
            }
        }

        int CalculateDesiredScvs()
        {
            var totalScvs = UnitCountService.Count(UnitTypes.TERRAN_SCV);
            if (MacroData.Minerals > 50)
            {
                if (AttackData.Attacking)
                {
                    var desiredTotal = (UnitCommanders.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_THOR || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_THORAP || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BATTLECRUISER) * 3) +
                        UnitCommanders.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK || c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED);
                    if (totalScvs - desiredTotal < 22)
                    {
                        desiredTotal = totalScvs - 22;
                    }
                    if (desiredTotal > 15 && MacroData.FoodUsed < 185)
                    {
                        desiredTotal = 15;
                    }
                    return desiredTotal;
                }
                else
                {
                    var missingHealth = UnitCommanders.Where(c => c.UnitCalculation.Attributes.Contains(Attribute.Mechanical)).Sum(c => c.UnitCalculation.Unit.HealthMax - c.UnitCalculation.Unit.Health);
                    if (missingHealth > 0)
                    {
                        var desiredTotal = (missingHealth / 50f) + 1;
                        if (totalScvs - desiredTotal < 22)
                        {
                            desiredTotal = totalScvs - 22;
                        }
                        if (desiredTotal > 15 && MacroData.FoodUsed < 185)
                        {
                            desiredTotal = 15;
                        }
                        return (int)desiredTotal;
                    }
                }
            }

            return 0;
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var actions = new List<SC2APIProtocol.Action>();

            var hiddenBase = TargetingData.HiddenEnemyBase;
            if (MainUnits.Count() > 0)
            {
                AttackData.ArmyPoint = TargetingService.GetArmyPoint(MainUnits);
            }
            else
            {
                AttackData.ArmyPoint = TargetingService.GetArmyPoint(SupportUnits);
            }
            TargetingData.AttackPoint = TargetingService.UpdateAttackPoint(AttackData.ArmyPoint, TargetingData.AttackPoint);


            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                System.Console.WriteLine($"AdvancedAttackTask Points {stopwatch.ElapsedMilliseconds}");
            }
            stopwatch.Restart();

            var attackingEnemies = ActiveUnitData.EnemyUnits.Values.Where(e => e.NearbyEnemies.Any(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter) || u.UnitClassifications.Contains(UnitClassification.ProductionStructure) || u.UnitClassifications.Contains(UnitClassification.DefensiveStructure)) ||
                 (e.TargetPriorityCalculation.OverallWinnability < .5f && EnemyAttackers.Any(e => e.Unit.Tag == e.Unit.Tag)));
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                System.Console.WriteLine($"AdvancedAttackTask closerenemies queries {stopwatch.ElapsedMilliseconds}");
            }
            if (attackingEnemies.Count() > 0)
            {
                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    System.Console.WriteLine($"AdvancedAttackTask closerenemies count {stopwatch.ElapsedMilliseconds}");
                }
                var armyVector = new Vector2(AttackData.ArmyPoint.X, AttackData.ArmyPoint.Y);
                var distanceToAttackPoint = Vector2.DistanceSquared(armyVector, new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y));
                var closerEnemies = attackingEnemies;
                if (AttackData.Attacking)
                {
                    closerEnemies = attackingEnemies.Where(e => Vector2.DistanceSquared(e.Position, armyVector) < distanceToAttackPoint);
                }
                if (closerEnemies.Count() > 0)
                {
                    if (stopwatch.ElapsedMilliseconds > 100)
                    {
                        System.Console.WriteLine($"AdvancedAttackTask closerenemies second count {stopwatch.ElapsedMilliseconds}");
                    }
                    actions = DefenseArmySplitter.SplitArmy(frame, closerEnemies, TargetingData.AttackPoint, MainUnits.Concat(SupportUnits), false);
                    if (stopwatch.ElapsedMilliseconds > 100)
                    {
                        System.Console.WriteLine($"AdvancedAttackTask closerenemies splitarmy {stopwatch.ElapsedMilliseconds}");
                    }
                    foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                    {
                        var subActions = subTask.Value.SplitArmy(frame, closerEnemies, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint);
                        actions.AddRange(subActions);
                    }

                    stopwatch.Stop();
                    if (stopwatch.ElapsedMilliseconds > 100)
                    {
                        System.Console.WriteLine($"AdvancedAttackTask closerenemies {stopwatch.ElapsedMilliseconds}");
                    }
                    stopwatch.Restart();

                    RemoveTemporaryUnits();
                    EnemyAttackers = attackingEnemies.ToList();
                    return actions;
                }
                else
                {
                    var attackVector = new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y);
                    var closerSelfUnits = UnitCommanders.Where(u => attackingEnemies.Any(e => Vector2.DistanceSquared(u.UnitCalculation.Position, attackVector) > Vector2.DistanceSquared(u.UnitCalculation.Position, e.Position)));
                    if (closerSelfUnits.Count() > 0)
                    {
                        actions.AddRange(DefenseArmySplitter.SplitArmy(frame, attackingEnemies, TargetingData.AttackPoint, closerSelfUnits, false));
                    }
                }

                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    System.Console.WriteLine($"AdvancedAttackTask attackingenemies end {stopwatch.ElapsedMilliseconds}");
                }
                stopwatch.Restart();
            }
            EnemyAttackers = attackingEnemies.ToList();

            HandleHiddenBuildings(hiddenBase);

            if (MainUnits.Count() > 0)
            {
                OrderMainUnitsWithSupportUnits(frame, actions, MainUnits, SupportUnits);
            }
            else
            {
                OrderSupportUnitsWithoutMainUnits(frame, actions, SupportUnits);
            }

            RemoveTemporaryUnits();

            return actions;
        }

        private void RemoveTemporaryUnits()
        {
            var undeadTypes = SharkyUnitData.UndeadTypes.Where(t => t != UnitTypes.PROTOSS_ADEPTPHASESHIFT && t != UnitTypes.PROTOSS_DISRUPTORPHASED);
            UnitCommanders.RemoveAll(u => undeadTypes.Contains((UnitTypes)u.UnitCalculation.Unit.UnitType));
            SupportUnits.RemoveAll(u => undeadTypes.Contains((UnitTypes)u.UnitCalculation.Unit.UnitType));
            MainUnits.RemoveAll(u => undeadTypes.Contains((UnitTypes)u.UnitCalculation.Unit.UnitType));

            if (MainUnits.Count() == 0)
            {
                var mainUnit = SupportUnits.FirstOrDefault(commander => MainAttackers.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType) && !commander.UnitCalculation.Unit.IsHallucination);
                if (mainUnit != null)
                {
                    SupportUnits.Remove(mainUnit);
                    mainUnit.UnitRole = UnitRole.Leader;
                    MainUnits.Add(mainUnit);
                }
            }
        }

        private void OrderSupportUnitsWithoutMainUnits(int frame, List<Action> actions, IEnumerable<UnitCommander> supportUnits)
        {
            if (AttackData.Attacking)
            {
                if (TargetingData.AttackState == AttackState.Kill || !AttackData.UseAttackDataManager)
                {
                    var attackingEnemies = supportUnits.SelectMany(c => c.UnitCalculation.NearbyEnemies).Distinct();
                    if (attackingEnemies.Count() > 0)
                    {
                        var splitActions = AttackArmySplitter.SplitArmy(frame, attackingEnemies, TargetingData.AttackPoint, supportUnits, false);
                        actions.AddRange(splitActions);
                    }
                    else
                    {
                        actions.AddRange(MicroController.Attack(supportUnits, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    }
                    foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                    {
                        actions.AddRange(subTask.Value.Attack(TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    }
                }
                else if (TargetingData.AttackState == AttackState.Contain)
                {
                    actions.AddRange(MicroController.Retreat(supportUnits, TargetingData.AttackPoint, AttackData.ArmyPoint, frame));
                    foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                    {
                        actions.AddRange(subTask.Value.Retreat(TargetingData.AttackPoint, AttackData.ArmyPoint, frame));
                    }
                }
            }
            else
            {
                var cleanupActions = EnemyCleanupService.CleanupEnemies(supportUnits, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame);
                if (cleanupActions != null)
                {
                    actions.AddRange(cleanupActions);
                }
                else
                {
                    actions.AddRange(MicroController.Retreat(supportUnits, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                }
                foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                {
                    actions.AddRange(subTask.Value.Retreat(TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                }
            }
        }

        private void OrderMainUnitsWithSupportUnits(int frame, List<Action> actions, IEnumerable<UnitCommander> mainUnits, IEnumerable<UnitCommander> supportUnits)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var supportAttackPoint = TargetingData.AttackPoint;
            var mainUnit = mainUnits.FirstOrDefault();
            if (mainUnit != null)
            {
                supportAttackPoint = new Point2D { X = mainUnit.UnitCalculation.Position.X, Y = mainUnit.UnitCalculation.Position.Y };
            }

            if (AttackData.Attacking)
            {
                if (TargetingData.AttackState == AttackState.Kill || TargetingData.AttackState == AttackState.None)
                {
                    actions.AddRange(MicroController.Attack(mainUnits, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    actions.AddRange(MicroController.Support(supportUnits, mainUnits, supportAttackPoint, TargetingData.ForwardDefensePoint, supportAttackPoint, frame));
                    foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                    {
                        actions.AddRange(subTask.Value.Support(mainUnits, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    }
                }
                else
                {
                    actions.AddRange(MicroController.Retreat(mainUnits, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    actions.AddRange(MicroController.Support(supportUnits, mainUnits, supportAttackPoint, supportAttackPoint, supportAttackPoint, frame));
                    foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                    {
                        actions.AddRange(subTask.Value.Support(mainUnits, supportAttackPoint, TargetingData.ForwardDefensePoint, supportAttackPoint, frame));
                    }
                }
            }
            else
            {
                var cleanupActions = EnemyCleanupService.CleanupEnemies(mainUnits.Concat(supportUnits), TargetingData.ForwardDefensePoint, supportAttackPoint, frame);
                if (cleanupActions != null)
                {
                    actions.AddRange(cleanupActions);
                }
                else
                {
                    actions.AddRange(MicroController.Retreat(mainUnits, TargetingData.ForwardDefensePoint, null, frame));
                    actions.AddRange(MicroController.Retreat(supportUnits, supportAttackPoint, supportAttackPoint, frame));
                }
                foreach (var subTask in SubTasks.Where(t => t.Value.Enabled).OrderBy(t => t.Value.Priority))
                {
                    actions.AddRange(subTask.Value.SupportRetreat(mainUnits, supportAttackPoint, supportAttackPoint, null, frame));
                }
            }

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                System.Console.WriteLine($"AdvancedAttackTask OrderMainUnitsWithSupportUnits {stopwatch.ElapsedMilliseconds}");
            }
        }

        void HandleHiddenBuildings(bool hiddenBase)
        {
            if (!MicroTaskData.ContainsKey(typeof(FindHiddenBaseTask).Name))
            {
                return;
            }
            if (!hiddenBase && TargetingData.HiddenEnemyBase)
            {
                ResetClaimedUnits();
                MicroTaskData[typeof(FindHiddenBaseTask).Name].Enable();
            }
            else if (hiddenBase && !TargetingData.HiddenEnemyBase && ActiveUnitData.EnemyUnits.Any())
            {
                MicroTaskData[typeof(FindHiddenBaseTask).Name].Disable();
            }
            if (MicroTaskData[typeof(FindHiddenBaseTask).Name].Enabled && ActiveUnitData.EnemyUnits.Any(e => e.Value.Attributes.Contains(Attribute.Structure)))
            {
                MicroTaskData[typeof(FindHiddenBaseTask).Name].Disable();
            }
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                SupportUnits.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                MainUnits.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
            foreach (var subTask in SubTasks.Where(s => s.Value.Enabled))
            {
                subTask.Value.RemoveDeadUnits(deadUnits);
            }
        }

        public override void StealUnit(UnitCommander commander)
        {
            RemoveDeadUnits(new List<ulong> { commander.UnitCalculation.Unit.Tag });
        }

        public void GiveCommanderToChild(UnitCommander commander)
        {
            SupportUnits.RemoveAll(c => c.UnitCalculation.Unit.Tag == commander.UnitCalculation.Unit.Tag);
            MainUnits.RemoveAll(c => c.UnitCalculation.Unit.Tag == commander.UnitCalculation.Unit.Tag);
        }

        public IEnumerable<UnitCommander> GetAvailableCommanders()
        {
            return SupportUnits.Concat(MainUnits);
        }
    }
}
