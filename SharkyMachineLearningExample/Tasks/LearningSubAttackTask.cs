using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.MicroTasks.Attack;
using Sharky.Pathing;
using Sharky;
using SharkyMachineLearningExample.Observation;
using SharkyMachineLearningExample.Action;

namespace SharkyMachineLearningExample.Tasks
{
    public class LearningSubAttackTask : AttackSubTask
    {
        protected TargetingService TargetingService;
        protected MapDataService MapDataService;
        MacroData MacroData;
        ActiveUnitData ActiveUnitData;

        ObservationService ObservationService;
        ActionService ActionService;

        int StartFrame = -1000;

        List<Unit> StartingEnemyUnits;
        List<Unit> StartingFriendlyUnits;

        List<Unit> CurrentEnemyUnits;
        List<Unit> CurrentFriendlyUnits;

        public LearningSubAttackTask(DefaultSharkyBot defaultSharkyBot, IAttackTask parentTask, float priority, bool enabled = false)
        {
            ParentTask = parentTask;

            MicroController = defaultSharkyBot.MicroController;

            TargetingService = defaultSharkyBot.TargetingService;
            MapDataService = defaultSharkyBot.MapDataService;

            TargetingData = defaultSharkyBot.TargetingData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;

            MacroData = defaultSharkyBot.MacroData;

            Priority = priority;
            Enabled = enabled;

            UnitCommanders = new List<UnitCommander>();

            ObservationService = new ObservationService();
            ActionService = new ActionService();
        }

        public override void Enable()
        {
            StartFrame = MacroData.Frame;
            base.Enable();

            StartingFriendlyUnits = ActiveUnitData.Commanders.Values.Select(c => c.UnitCalculation.Unit).ToList();
            StartingEnemyUnits = ActiveUnitData.EnemyUnits.Values.Select(c => c.Unit).ToList();

            CurrentFriendlyUnits = StartingFriendlyUnits;
            CurrentEnemyUnits = StartingEnemyUnits;

            // TODO: start a new training session for the agent
        }

        public override IEnumerable<SC2APIProtocol.Action> Attack(Point2D attackPoint, Point2D defensivePoint, Point2D armyPoint, int frame)
        {
            if (!UnitCommanders.Any()) { return new List<SC2APIProtocol.Action>(); }

            if (frame <= StartFrame + 2)
            {
                return ClearOrders(frame);
            }

            var encodedActions = GetActionsFromReinforcementLearning();
            var actions = ActionService.GetSC2Actions(encodedActions);
            return actions;
        }

        float[] GetActionsFromReinforcementLearning()
        {
            float[] stateSpace = GetStateSpace();
            float[] encodedActions = MockActions(); // TODO: get this from the agent
            float reward = GetCurrentReward();
            // TODO: update the agent with the reward

            return encodedActions;
        }

        void EndTrainingSession(float reward)
        {
            // TODO: set agent award and end training session
        }

        float GetCurrentReward()
        {
            var friendlyUnits = ActiveUnitData.Commanders.Values.Where(startUnit => StartingFriendlyUnits.Any(endUnit => endUnit.Tag == startUnit.UnitCalculation.Unit.Tag)).Select(e => e.UnitCalculation.Unit).ToList();
            var enemyUnits = ActiveUnitData.EnemyUnits.Values.Where(startUnit => StartingEnemyUnits.Any(endUnit => endUnit.Tag == startUnit.Unit.Tag)).Select(e => e.Unit).ToList();

            return friendlyUnits.Sum(u => u.Health + u.Shield) - enemyUnits.Sum(u => u.Health + u.Shield);
        }

        public void Won(List<Unit> roundStartUnits, List<Unit> roundEndUnits)
        {
            var award = roundStartUnits.Count(startUnit => roundEndUnits.Any(endUnit => endUnit.Tag == startUnit.Tag)) / (float)roundStartUnits.Count;
            EndTrainingSession(award);
        }

        public void Lost(List<Unit> roundStartEnemyUnits, List<Unit> roundEndEnemyUnits)
        {
            var percentLeft = roundStartEnemyUnits.Count(startUnit => roundEndEnemyUnits.Any(endUnit => endUnit.Tag == startUnit.Tag)) / (float)roundStartEnemyUnits.Count;
            var award = -1f * percentLeft;
            EndTrainingSession(award);
        }

        public void Tied()
        {
            var award = 0f;
            EndTrainingSession(award);
        }

        void UpdateShortTermReward()
        {
            // TODO: if agent step > 0 update the reward

            var totalHealth = StartingFriendlyUnits.Sum(u => u.Health + u.Shield) + StartingEnemyUnits.Sum(u => u.Health + u.Shield);

            var friendlyUnits = ActiveUnitData.Commanders.Values.Where(startUnit => StartingFriendlyUnits.Any(endUnit => endUnit.Tag == startUnit.UnitCalculation.Unit.Tag)).Select(e => e.UnitCalculation.Unit).ToList();
            var enemyUnits = ActiveUnitData.EnemyUnits.Values.Where(startUnit => StartingEnemyUnits.Any(endUnit => endUnit.Tag == startUnit.Unit.Tag)).Select(e => e.Unit).ToList();

            var friendlyHealthChange = friendlyUnits.Sum(u => u.Health + u.Shield) - CurrentFriendlyUnits.Sum(u => u.Health + u.Shield);
            var enemyHealthChange = enemyUnits.Sum(u => u.Health + u.Shield) - CurrentEnemyUnits.Sum(u => u.Health + u.Shield);

            var change = friendlyHealthChange - enemyHealthChange;

            var reward = change / totalHealth;  // once this gets more advanced will want to penalize losing units and losing health, losing shields is fine, value units by their cost
            // TODO: set the short term reward value?

            CurrentFriendlyUnits = friendlyUnits;
            CurrentEnemyUnits = enemyUnits;
        }

        float[] MockActions()
        {
            var enemyTag = ActiveUnitData.EnemyUnits.Keys.FirstOrDefault();
            List<UnitAction> actions = new List<UnitAction>();
            foreach (var commander in UnitCommanders)
            {
                actions.Add(new UnitAction { UnitTag = commander.UnitCalculation.Unit.Tag, Type = ActionType.AttackUnit, TargetUnitTag = enemyTag });
            }

            return ActionService.EncodeMultiUnitAction(new ActionService.MultiUnitAction { UnitActions = actions });
        }

        private float[] GetStateSpace()
        {
            var enemyUnits = ActiveUnitData.EnemyUnits.Values.Where(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_STALKER);
            var stateSpace = ObservationService.GetFlattenedObservation(UnitCommanders, enemyUnits, MapDataService.MapData);
            return stateSpace;
        }

        private IEnumerable<SC2APIProtocol.Action> ClearOrders(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            foreach (var commander in UnitCommanders)
            {
                actions.AddRange(commander.Order(frame, Abilities.STOP));
            }
            return actions;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    ClaimUnit(commander);
                }
            }
        }

        public override void ClaimUnitsFromParent(IEnumerable<UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                ClaimUnitFromParent(commander);
            }
        }

        protected virtual void ClaimUnit(KeyValuePair<ulong, UnitCommander> commander)
        {
            commander.Value.Claimed = true;
            commander.Value.UnitRole = UnitRole.Attack;

            UnitCommanders.Add(commander.Value);
            ParentTask.UnitCommanders.Add(commander.Value);
        }

        protected virtual void ClaimUnitFromParent(UnitCommander commander)
        {
            if (UnitCommanders.Any(c => c.UnitCalculation.Unit.Tag == commander.UnitCalculation.Unit.Tag))
            {
                return;
            }
            commander.Claimed = true;
            commander.UnitRole = UnitRole.Attack;
            UnitCommanders.Add(commander);
            ParentTask.GiveCommanderToChild(commander);
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            var count = 0;
            foreach (var tag in deadUnits)
            {
                count = UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
        }
    }
}
