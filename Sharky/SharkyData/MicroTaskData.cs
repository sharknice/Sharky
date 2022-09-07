using Sharky.MicroTasks;
using System.Collections.Generic;

namespace Sharky
{
    public class MicroTaskData : Dictionary<string, IMicroTask>
    {
        /// <summary>
        /// Removes the unit from all unit commanders of all microtasks.
        /// Does not change unit role or claimed.
        /// </summary>
        /// <param name="commander"></param>
        public void StealCommanderFromAllTasks(UnitCommander commander)
        {
            foreach (var microTask in this)
            {
                microTask.Value.StealUnit(commander);
            }
        }
    }
}
