using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sharky.MicroTasks
{
    public abstract class MicroTask : IMicroTask
    {
        public List<UnitCommander> UnitCommanders { get; set; }
        public float Priority { get; set; }

        public bool Enabled { get; protected set; }

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

        public virtual void Enable()
        {
            Enabled = true;
        }

        public virtual void Disable()
        {
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
                commander.UnitRole = UnitRole.None;
            }
            UnitCommanders = new List<UnitCommander>();

            Enabled = false;
        }
    }
}
