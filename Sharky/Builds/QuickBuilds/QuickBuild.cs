using System.Collections.Generic;

namespace Sharky.Builds.QuickBuilds
{
    /// <summary>
    /// Allows to define build steps in easy way. The units/upgrades are built step by step once enough workers are produced.
    /// First parameter in step is worker count, when the unit/upgrade can be started, if the parameter is set to null or zero, then the worker count check is ignored.
    /// Second parameter is UnitType or Upgrade type.
    /// Third parameter is count of units to be created (ignored for upgrades).
    /// </summary>
    public class QuickBuild : List<(int?, object, int?)>
    {
        public int CurrentStepIndex { get; set; } = 0;

        public (int?, dynamic, int?)? CurrentStep => CurrentStepIndex < Count ? this[CurrentStepIndex] : null;
    }
}
