using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sharky.MicroTasks
{
    public abstract class MicroTask : IMicroTask
    {
        public List<UnitCommander> UnitCommanders { get; set; }
        public float Priority { get; set; }

        public virtual void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            throw new NotImplementedException();
        }

        public virtual void ResetClaimedUnits()
        {
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
            }
            UnitCommanders = new List<UnitCommander>();
        }
    }
}
