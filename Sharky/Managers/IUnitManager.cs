using System.Collections.Concurrent;

namespace Sharky.Managers
{
    public interface IUnitManager : IManager
    {
        ConcurrentDictionary<ulong, UnitCommander> Commanders { get; }
        ConcurrentDictionary<ulong, UnitCalculation> EnemyUnits { get; }
        ConcurrentDictionary<ulong, UnitCalculation> SelfUnits { get; }
        ConcurrentDictionary<ulong, UnitCalculation> NeutralUnits { get; }
        int Count(UnitTypes unitType);
        int Completed(UnitTypes unitType);
    }
}
