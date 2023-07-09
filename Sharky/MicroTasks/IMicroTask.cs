using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.MicroTasks
{
    public interface IMicroTask
    {
        float Priority { get; set; }
        List<UnitCommander> UnitCommanders { get; set; }
        void ClaimUnits(Dictionary<ulong, UnitCommander> commanders);
        IEnumerable<Action> PerformActions(int frame);
        void ResetClaimedUnits();
        List<UnitCommander> ResetNonEssentialClaims();
        void Enable();
        void Disable();
        bool Enabled { get; }
        double LongestFrame { get; set; }
        double TotalFrameTime { get; set; }

        void RemoveDeadUnits(List<ulong> deadUnits);
        void StealUnit(UnitCommander commander);
        void PrintReport(int frame);
        void DebugUnits(DebugService debugService);
    }
}
