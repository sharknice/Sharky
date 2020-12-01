using System.Collections.Generic;

namespace Sharky.Builds.BuildChoosing
{
    public interface ICounterTransitioner
    {
        List<string> DefaultCounterTransition(int frame);
    }
}
