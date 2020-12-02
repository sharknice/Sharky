using SC2APIProtocol;
using Sharky.MicroTasks;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers
{
    public class MicroManager : SharkyManager
    {      
        IUnitManager UnitManager;

        public Dictionary<string, IMicroTask> MicroTasks;

        public MicroManager(IUnitManager unitManager, Dictionary<string, IMicroTask> microTasks)
        {
            UnitManager = unitManager;
            MicroTasks = microTasks;
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            var actions = new List<Action>();
            foreach (var microTask in MicroTasks.Values.Where(m => m.Enabled).OrderBy(m => m.Priority))
            {
                foreach (var tag in UnitManager.DeadUnits)
                {
                    microTask.UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                }

                microTask.ClaimUnits(UnitManager.Commanders);
                actions.AddRange(microTask.PerformActions(frame));
            }
            return actions;
        }
    }
}
