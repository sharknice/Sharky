using Sharky;
using Sharky.Chat;
using Sharky.MicroControllers;
using Sharky.MicroTasks.Attack;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Proxy
{
    public class SupportAttackTask : MicroTask
    {
        AttackData AttackData;
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;
        MicroTaskData MicroTaskData;

        IMicroController MicroController;

        DebugService DebugService;
        ChatService ChatService;
        TargetingService TargetingService;
        DefenseService DefenseService;
        DistractionSquadService DistractionSquadService;
        EnemyCleanupService EnemyCleanupService;

        ArmySplitter ArmySplitter;

        float lastFrameTime;

        public List<UnitTypes> MainAttackers { get; set; }

        public SupportAttackTask(AttackData attackData, TargetingData targetingData, ActiveUnitData activeUnitData, MicroTaskData microTaskData, 
            IMicroController microController, 
            DebugService debugService, ChatService chatService, TargetingService targetingService, DefenseService defenseService, DistractionSquadService distractionSquadService, EnemyCleanupService enemyCleanupService,
            ArmySplitter armySplitter,
            List<UnitTypes> mainAttackerTypes, 
            float priority, bool enabled = true)
        {
            AttackData = attackData;
            TargetingData = targetingData;
            ActiveUnitData = activeUnitData;
            MicroTaskData = microTaskData;

            MicroController = microController;

            DebugService = debugService;
            ChatService = chatService;
            TargetingService = targetingService;
            DefenseService = defenseService;
            DistractionSquadService = distractionSquadService;
            EnemyCleanupService = enemyCleanupService;

            ArmySplitter = armySplitter;

            MainAttackers = mainAttackerTypes;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.ArmyUnit))
                {
                    commander.Value.Claimed = true;
                    UnitCommanders.Add(commander.Value);
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

            var mainUnits = UnitCommanders.Where(c => MainAttackers.Contains((UnitTypes)c.UnitCalculation.Unit.UnitType));

            var otherUnits = UnitCommanders.Where(c => !MainAttackers.Contains((UnitTypes)c.UnitCalculation.Unit.UnitType));
            DistractionSquadService.UpdateDistractionSquad(otherUnits);

            DistractionSquadService.Enabled = UnitCommanders.Count() > 25;

            IEnumerable<UnitCommander> supportUnits;
            if (DistractionSquadService.DistractionSquadState == DistractionSquadState.NotDistracting)
            {
                supportUnits = otherUnits;
            }
            else
            {
                supportUnits = otherUnits.Where(c => !DistractionSquadService.DistractionSquad.Any(d => d.UnitCalculation.Unit.Tag == c.UnitCalculation.Unit.Tag));
            }

            var hiddenBase = TargetingData.HiddenEnemyBase;
            if (mainUnits.Count() > 0)
            {
                AttackData.ArmyPoint = TargetingService.GetArmyPoint(mainUnits);
            }
            else
            {
                AttackData.ArmyPoint = TargetingService.GetArmyPoint(supportUnits);
            }
            TargetingData.AttackPoint = TargetingService.UpdateAttackPoint(AttackData.ArmyPoint, TargetingData.AttackPoint);

            var attackingEnemies = ActiveUnitData.SelfUnits.Where(u => u.Value.UnitClassifications.Contains(UnitClassification.ResourceCenter) || u.Value.UnitClassifications.Contains(UnitClassification.ProductionStructure) || u.Value.UnitClassifications.Contains(UnitClassification.DefensiveStructure)).SelectMany(u => u.Value.NearbyEnemies).Distinct();
            if (attackingEnemies.Count() > 0)
            {
                var armyPoint = new Vector2(AttackData.ArmyPoint.X, AttackData.ArmyPoint.Y);
                var distanceToAttackPoint = Vector2.DistanceSquared(armyPoint, new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y));
                var closerEnemies = attackingEnemies;
                if (AttackData.Attacking)
                {
                    closerEnemies = attackingEnemies.Where(e => Vector2.DistanceSquared(e.Position, armyPoint) < distanceToAttackPoint);
                }
                if (closerEnemies.Count() > 0)
                {
                    actions = ArmySplitter.SplitArmy(frame, closerEnemies, TargetingData.AttackPoint, mainUnits.Concat(supportUnits), false);
                    stopwatch.Stop();
                    lastFrameTime = stopwatch.ElapsedMilliseconds;
                    return actions;
                }
            }

            if (!hiddenBase && TargetingData.HiddenEnemyBase)
            {
                ResetClaimedUnits();
                if (MicroTaskData.MicroTasks.ContainsKey("FindHiddenBaseTask"))
                {
                    MicroTaskData.MicroTasks["FindHiddenBaseTask"].Enable();
                }
            }
            else if (hiddenBase && !TargetingData.HiddenEnemyBase)
            {
                if (MicroTaskData.MicroTasks.ContainsKey("FindHiddenBaseTask"))
                {
                    MicroTaskData.MicroTasks["FindHiddenBaseTask"].Disable();
                }
            }

            actions.AddRange(DistractionSquadService.TakeAction(frame));

            if (mainUnits.Count() > 0)
            {
                if (AttackData.Attacking)
                {
                    actions.AddRange(MicroController.Attack(mainUnits, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                    actions.AddRange(MicroController.Support(supportUnits, mainUnits, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
                }
                else
                {
                    var cleanupActions = EnemyCleanupService.CleanupEnemies(mainUnits.Concat(supportUnits), TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame);
                    if (cleanupActions != null)
                    {
                        actions.AddRange(cleanupActions);
                    }
                    else
                    {
                        actions.AddRange(MicroController.Retreat(mainUnits, TargetingData.ForwardDefensePoint, null, frame));
                        actions.AddRange(MicroController.Retreat(supportUnits, TargetingData.ForwardDefensePoint, null, frame));
                    }
                }
            }
            else
            {
                if (AttackData.Attacking)
                {
                    actions.AddRange(MicroController.Attack(supportUnits, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, AttackData.ArmyPoint, frame));
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
                }
            }

            stopwatch.Stop();
            lastFrameTime = stopwatch.ElapsedMilliseconds;
            return actions;
        }
    }
}
