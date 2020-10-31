using SC2APIProtocol;
using Sharky.MicroTasks;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class MicroManager : SharkyManager
    {      
        IUnitManager UnitManager;
        List<IMicroTask> MicroTasks;

        public MicroManager(IUnitManager unitManager, List<IMicroTask> microTasks)
        {
            UnitManager = unitManager;
            MicroTasks = microTasks;
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            foreach (var microTask in MicroTasks)
            {
                microTask.ClaimUnits(UnitManager.Commanders); // determine which MicroTask units should be a part of, save it on the UnitCommander
            }

            var actions = new List<Action>();
            foreach (var microTask in MicroTasks)
            {
                actions.AddRange(microTask.PerformActions(frame));
            }
            return actions;
        }
    }
}
