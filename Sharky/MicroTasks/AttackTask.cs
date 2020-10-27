using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sharky.MicroTasks
{
    public class AttackTask : IMicroTask
    {
        public List<UnitCommander> UnitCommanders { get; set; }

        IMicroController MicroController;

        public AttackTask(IMicroController microController)
        {
            MicroController = microController;
            UnitCommanders = new List<UnitCommander>();
        }

        public void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    commander.Value.Claimed = true;
                    UnitCommanders.Add(commander.Value);
                }
            }
        }

        public IEnumerable<SC2APIProtocol.Action> PerformActions()
        {
            return MicroController.Attack(UnitCommanders, new SC2APIProtocol.Point2D { X = 10, Y = 10 }, new SC2APIProtocol.Point2D { X = 10, Y = 10 });
        }
    }
}
