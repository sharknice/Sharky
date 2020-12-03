using SC2APIProtocol;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public interface IUnitManager : IManager
    {
        ConcurrentDictionary<ulong, UnitCommander> Commanders { get; }
        ConcurrentDictionary<ulong, UnitCalculation> EnemyUnits { get; }
        ConcurrentDictionary<ulong, UnitCalculation> SelfUnits { get; }
        ConcurrentDictionary<ulong, UnitCalculation> NeutralUnits { get; }
        List<ulong> DeadUnits { get; }
        int Count(UnitTypes unitType);
        int EnemyCount(UnitTypes unitType);
        int Completed(UnitTypes unitType);
        int EquivalentTypeCount(UnitTypes unitType);
        int EquivalentTypeCompleted(UnitTypes unitType);
        int UnitsInProgressCount(UnitTypes unitType);
        bool CanDamage(IEnumerable<Weapon> weapons, Unit unit);
    }
}
