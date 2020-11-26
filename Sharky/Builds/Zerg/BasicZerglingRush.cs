using SC2APIProtocol;
using Sharky.Managers;

namespace Sharky.Builds.Zerg
{
    public class BasicZerglingRush : ZergSharkyBuild
    {
        public BasicZerglingRush(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, IChatManager chatManager) : base(buildOptions, macroData, unitManager, attackData, chatManager)
        {

        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            MacroData.DesiredGases = 0;

            AttackData.ArmyFoodAttack = 5;

            MacroData.DesiredUnitCounts[UnitTypes.ZERG_ZERGLING] = 100;
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

            if (MacroData.FoodUsed >= 50)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY] = 2;
                }
            }
        }

        public override bool Transition(int frame)
        {
            return MacroData.FoodUsed > 50 && UnitManager.EquivalentTypeCompleted(UnitTypes.ZERG_HATCHERY) > 1;
        }
    }
}
