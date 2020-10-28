using System.Collections.Concurrent;

namespace Sharky.Managers
{
    public interface IUnitManager : IManager
    {
        ConcurrentDictionary<ulong, UnitCommander> Commanders { get; }
    }
}
