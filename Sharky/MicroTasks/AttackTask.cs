using Sharky.Managers;
using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sharky.MicroTasks
{
    public class AttackTask : IMicroTask
    {
        public List<UnitCommander> UnitCommanders { get; set; }

        IMicroController MicroController;
        ITargetingManager TargetingManager;

        public AttackTask(IMicroController microController, ITargetingManager targetingManager)
        {
            MicroController = microController;
            TargetingManager = targetingManager;

            UnitCommanders = new List<UnitCommander>();
        }

        public void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
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

        public IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            return MicroController.Attack(UnitCommanders, TargetingManager.AttackPoint, TargetingManager.DefensePoint, frame);
        }
    }
}
