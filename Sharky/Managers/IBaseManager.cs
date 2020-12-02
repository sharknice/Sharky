using System.Collections.Generic;

namespace Sharky.Managers
{
    public interface IBaseManager : IManager
    {
        List<BaseLocation> BaseLocations { get; }
        List<BaseLocation> SelfBases { get; }
        BaseLocation MainBase { get; }
    }
}
