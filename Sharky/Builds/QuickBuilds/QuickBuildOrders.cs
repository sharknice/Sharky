namespace Sharky.Builds.QuickBuilds
{
    /// <summary>
    /// Allows to define build steps in easy way. The units/upgrades are built step by step once enough workers are produced.
    /// First parameter in step is food supply used, when the unit/upgrade can be started.
    /// Second parameter is UnitType or Upgrade type or QuickAction.
    /// Third parameter is count of additional units to be created (ignored for upgrades).
    /// </summary>
    public class QuickBuildOrders : List<(int, object, int)>
    {
        /// <summary>
        /// Index of current step.
        /// </summary>
        public int CurrentStepIndex { get; private set; } = 0;

        /// <summary>
        /// Returns current step.
        /// </summary>
        public (int, dynamic, int)? CurrentStep => CurrentStepIndex < Count ? this[CurrentStepIndex] : null;

        /// <summary>
        /// Moves to next build step
        /// </summary>
        /// <returns></returns>
        public void Advance() 
        { 
            CurrentStepIndex++;
        }

        public void Reset() => CurrentStepIndex = 0;

        /// <summary>
        /// Returns true if this build has finished.
        /// </summary>
        /// <returns></returns>
        public bool IsFinished => CurrentStepIndex >= Count;
    }
}
