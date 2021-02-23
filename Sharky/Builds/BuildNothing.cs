using Sharky.Chat;

namespace Sharky.Builds
{
    public class BuildNothing : SharkyBuild
    {
        public BuildNothing(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, ChatService chatService, UnitCountService unitCountService) : base(buildOptions, macroData, activeUnitData, attackData, chatService, unitCountService)
        {

        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = false;
            BuildOptions.StrictSupplyCount = true;
            BuildOptions.StrictWorkerCount = false;
        }
    }
}
