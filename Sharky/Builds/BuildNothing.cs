using SC2APIProtocol;
using Sharky.Chat;
using Sharky.DefaultBot;
using System;
using System.Linq;

namespace Sharky.Builds
{
    public class BuildNothing : SharkyBuild
    {
        public BuildNothing(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
        }

        public BuildNothing(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, MicroTaskData microTaskData,
            ChatService chatService, UnitCountService unitCountService) 
            : base(buildOptions, macroData, activeUnitData, attackData, microTaskData, chatService, unitCountService)
        {
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            BuildOptions.StrictSupplyCount = true;
            BuildOptions.StrictWorkerCount = true;

            AttackData.CustomAttackFunction = true;
            AttackData.Attacking = false;
            AttackData.UseAttackDataManager = false;
        }

        public override void OnFrame(ResponseObservation observation)
        {
            var positions = "";
            var nexuss = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS);
            foreach (var nexus in nexuss)
            {
                positions += $"nexus {nexus.UnitCalculation.Position}, ";
            }

            var pylons = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON);
            foreach (var pylon in pylons)
            {
                positions += $"pylon {pylon.UnitCalculation.Position}, ";
            }

            var gateways = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_GATEWAY);
            foreach (var gatway in gateways)
            {
                positions += $"gateway {gatway.UnitCalculation.Position}, ";
            }

            var cybers = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_CYBERNETICSCORE);
            foreach (var cybercore in cybers)
            {
                positions += $"cybercore {cybercore.UnitCalculation.Position}, ";
            }

            if (MacroData.Minerals == 1000)
            {
                Console.WriteLine($"1000 minerals at {observation.Observation.GameLoop} frames");
            }
        }
    }
}
