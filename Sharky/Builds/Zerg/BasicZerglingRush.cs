using SC2APIProtocol;
using Sharky.Managers;
using Sharky.MicroTasks;

namespace Sharky.Builds.Zerg
{
    public class BasicZerglingRush : ZergSharkyBuild
    {
        MicroManager MicroManager;

        public BasicZerglingRush(BuildOptions buildOptions, MacroData macroData, IUnitManager unitManager, AttackData attackData, IChatManager chatManager, MicroManager microManager) : base(buildOptions, macroData, unitManager, attackData, chatManager)
        {
            MicroManager = microManager;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictWorkerCount = true;
            BuildOptions.StrictGasCount = true;
            BuildOptions.StrictSupplyCount = true;
            MacroData.DesiredGases = 0;

            AttackData.ArmyFoodAttack = 3;

            MacroData.DesiredUnitCounts[UnitTypes.ZERG_DRONE] = 10;
            MacroData.DesiredUnitCounts[UnitTypes.ZERG_OVERLORD] = 1;

            if (MicroManager.MicroTasks.ContainsKey("WorkerScoutTask"))
            {
                var workerScoutTask = (WorkerScoutTask)MicroManager.MicroTasks["WorkerScoutTask"];
                workerScoutTask.Disable();
            }
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

            if (UnitManager.Count(UnitTypes.ZERG_SPAWNINGPOOL) > 0)
            {
                MacroData.DesiredGases = 1;
                MacroData.DesiredUnitCounts[UnitTypes.ZERG_OVERLORD] = 2;
            }

            if (UnitManager.Completed(UnitTypes.ZERG_SPAWNINGPOOL) > 0)
            {
                MacroData.DesiredUpgrades[Upgrades.ZERGLINGMOVEMENTSPEED] = true;
                MacroData.DesiredUnitCounts[UnitTypes.ZERG_ZERGLING] = 30;
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
                BuildOptions.StrictWorkerCount = false;
                MacroData.DesiredUnitCounts[UnitTypes.ZERG_QUEEN] = 2;
            }
        }

        public override bool Transition(int frame)
        {
            return MacroData.FoodUsed > 50 && UnitManager.EquivalentTypeCompleted(UnitTypes.ZERG_HATCHERY) > 1;
        }
    }
}
