using SC2APIProtocol;
using Sharky.Builds.BuildChoosing;
using Sharky.Managers;
using Sharky.Managers.Protoss;

namespace Sharky.Builds.Protoss
{
    public class Twilight : ProtossSharkyBuild
    {

        public Twilight(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, IChatManager chatManager, NexusManager nexusManager, ICounterTransitioner counterTransitioner) : base(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager, counterTransitioner)
        {
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
            }
            if (UnitManager.Completed(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
            }

            if (UnitManager.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_TWILIGHTCOUNCIL] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_TWILIGHTCOUNCIL] = 1;
                }
            }
        }

        public override bool Transition(int frame)
        {
            return UnitManager.Completed(UnitTypes.PROTOSS_TWILIGHTCOUNCIL) > 0;
        }
    }
}
