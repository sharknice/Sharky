using SC2APIProtocol;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sharky.MicroTasks
{
    public interface IMicroTask
    {
        float Priority { get; }
        List<UnitCommander> UnitCommanders { get; set; }
        void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders);
        IEnumerable<Action> PerformActions(int frame);
    }
}
