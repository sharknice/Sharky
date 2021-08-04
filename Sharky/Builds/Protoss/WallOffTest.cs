using SC2APIProtocol;
using Sharky.Builds.BuildChoosing;
using Sharky.Chat;
using Sharky.MicroTasks;

namespace Sharky.Builds.Protoss
{
    public class WallOffTest : ProtossSharkyBuild
    {
        MicroTaskData MicroTaskData;

        IMicroTask WallOffTask;

        bool WallOffStarted;

        public WallOffTest(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, ChatService chatService, ChronoData chronoData, MicroTaskData microTaskData, 
            ICounterTransitioner counterTransitioner, UnitCountService unitCountService)
            : base(buildOptions, macroData, activeUnitData, attackData, chatService, chronoData, counterTransitioner, unitCountService, microTaskData)
        {
            MicroTaskData = microTaskData;

            WallOffStarted = false;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictSupplyCount = true;
            BuildOptions.StrictGasCount = true;

            WallOffTask = MicroTaskData.MicroTasks["WallOffTask"];
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (!WallOffStarted)
            {
                WallOffTask.Enable();
                WallOffStarted = true;
            }

            if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
            }
            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 2;
                }
            }
        }

        public override bool Transition(int frame)
        {
            return UnitCountService.Completed(UnitTypes.PROTOSS_STARGATE) > 0;
        }
    }
}
