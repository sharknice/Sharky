using SC2APIProtocol;
using Sharky.Chat;
using Sharky.DefaultBot;

namespace Sharky.Builds.Zerg
{
    public class BasicZerglingRush : ZergSharkyBuild
    {
        public BasicZerglingRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
        }

        public BasicZerglingRush(BuildOptions buildOptions, 
            MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData,
            ChatService chatService, MicroTaskData microTaskData, UnitCountService unitCountService) 
            : base(buildOptions, macroData, activeUnitData, attackData, microTaskData, chatService, unitCountService)
        {
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictWorkerCount = true;
            BuildOptions.StrictGasCount = true;
            BuildOptions.StrictSupplyCount = true;
            MacroData.DesiredGases = 0;

            MacroData.DesiredUnitCounts[UnitTypes.ZERG_DRONE] = 10;
            MacroData.DesiredUnitCounts[UnitTypes.ZERG_OVERLORD] = 1;
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroData.FoodUsed >= 12)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.ZERG_SPAWNINGPOOL] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.ZERG_SPAWNINGPOOL] = 1;
                }
            }

            if (UnitCountService.Count(UnitTypes.ZERG_SPAWNINGPOOL) > 0)
            {
                MacroData.DesiredGases = 1;
                MacroData.DesiredUnitCounts[UnitTypes.ZERG_OVERLORD] = 2;
            }

            if (UnitCountService.Completed(UnitTypes.ZERG_SPAWNINGPOOL) > 0)
            {
                MacroData.DesiredUpgrades[Upgrades.ZERGLINGMOVEMENTSPEED] = true;
                MacroData.DesiredUnitCounts[UnitTypes.ZERG_ZERGLING] = 100;
            }

            if (MacroData.FoodUsed > 20)
            {
                BuildOptions.StrictSupplyCount = false;
            }

            if (MacroData.FoodUsed >= 40 || MacroData.Minerals > 400)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY] = 2;
                }
                MacroData.DesiredUnitCounts[UnitTypes.ZERG_QUEEN] = 2;
            }
        }

        public override bool Transition(int frame)
        {
            return MacroData.FoodUsed > 50 && UnitCountService.EquivalentTypeCompleted(UnitTypes.ZERG_HATCHERY) > 1;
        }
    }
}
