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
        }

        public override bool NeverSkip { get => true; }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            var actions = new List<Action>();
            foreach (var microTask in MicroTaskData.MicroTasks.Values.Where(m => m.Enabled).OrderBy(m => m.Priority))
            {
                var begin = System.DateTime.UtcNow;
                microTask.RemoveDeadUnits(ActiveUnitData.DeadUnits);

                microTask.ClaimUnits(ActiveUnitData.Commanders);
                if (!SkipFrame)
                {
                    actions.AddRange(microTask.PerformActions(frame));
                }
                var end = System.DateTime.UtcNow;
                var time = (end - begin).TotalMilliseconds;

                if (time > 100)
                {
                    System.Console.WriteLine($"{observation.Observation.GameLoop} {microTask.GetType().Name} {time}");
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
