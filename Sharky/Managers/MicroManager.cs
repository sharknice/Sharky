using SC2APIProtocol;
using Sharky.MicroTasks;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class MicroManager : SharkyManager
    {      
        UnitManager UnitManager;
        List<IMicroTask> MicroTasks;

        public MicroManager(UnitManager unitManager, List<IMicroTask> microTasks)
        {
            UnitManager = unitManager;
            MicroTasks = microTasks;
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            foreach (var microTask in MicroTasks)
            {
                microTask.ClaimUnits(UnitManager.Commanders); // determine which MicroTask units should be a part of, save it on the UnitCommander
            }

            var actions = new List<Action>();
            foreach (var microTask in MicroTasks)
            {
                actions.AddRange(microTask.PerformActions());
            }
            return actions;
        }
    }
}
