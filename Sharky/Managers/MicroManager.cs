using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers
{
    public class MicroManager : SharkyManager
    {      
        ActiveUnitData ActiveUnitData;
        MicroTaskData MicroTaskData;

        public MicroManager(ActiveUnitData activeUnitData, MicroTaskData microTaskData)
        {
            ActiveUnitData = activeUnitData;
            MicroTaskData = microTaskData;
            NeverSkip = true;
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            var actions = new List<Action>();
            foreach (var microTask in MicroTaskData.MicroTasks.Values.Where(m => m.Enabled).OrderBy(m => m.Priority))
            {
                foreach (var tag in ActiveUnitData.DeadUnits)
                {
                    microTask.UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                }

                microTask.ClaimUnits(ActiveUnitData.Commanders);
                if (!SkipFrame)
                {
                    actions.AddRange(microTask.PerformActions(frame));
                }
            }
            if (SkipFrame)
            {
                SkipFrame = false;
            }
            return actions;
        }
    }
}
