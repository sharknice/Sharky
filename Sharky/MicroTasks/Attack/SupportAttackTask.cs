using Sharky.Chat;
using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sharky.MicroTasks.Proxy
{
    public class SupportAttackTask : MicroTask
    {
        AttackData AttackData;
        TargetingData TargetingData;
        IMicroController MicroController;
        DebugService DebugService;
        ChatService ChatService;

        float lastFrameTime;

        public List<UnitTypes> MainAttackers { get; set; }

        public SupportAttackTask(AttackData attackData, TargetingData targetingData, IMicroController microController, DebugService debugService, ChatService chatService, List<UnitTypes> mainAttackerTypes, float priority, bool enabled = true)
        {
            AttackData = attackData;
            TargetingData = targetingData;
            MicroController = microController;
            DebugService = debugService;
            ChatService = chatService;

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
            var supportUnits = UnitCommanders.Where(c => !MainAttackers.Contains((UnitTypes)c.UnitCalculation.Unit.UnitType));

            if (mainUnits.Count() > 0)
            {
                if (AttackData.Attacking)
                {
                    actions.AddRange(MicroController.Attack(mainUnits, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, null, frame));
                }
                else
                {
                    actions.AddRange(MicroController.Retreat(mainUnits, TargetingData.ForwardDefensePoint, null, frame));
                }
                actions.AddRange(MicroController.Support(supportUnits, mainUnits, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, null, frame));
            }
            else
            {
                if (AttackData.Attacking)
                {
                    actions.AddRange(MicroController.Attack(supportUnits, TargetingData.AttackPoint, TargetingData.ForwardDefensePoint, null, frame));
                }
                else
                {
                    actions.AddRange(MicroController.Retreat(supportUnits, TargetingData.ForwardDefensePoint, null, frame));
                }
            }

            stopwatch.Stop();
            lastFrameTime = stopwatch.ElapsedMilliseconds;
            return actions;
        }
    }
}
