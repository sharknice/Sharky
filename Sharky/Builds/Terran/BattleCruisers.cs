using SC2APIProtocol;
using Sharky.Managers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharky.Builds.Terran
{
    public class BattleCruisers : TerranSharkyBuild
    {
        public BattleCruisers(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, IChatManager chatManager) : base(buildOptions, macroData, unitManager, attackData, chatManager)
        {

        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            AttackData.ArmyFoodAttack = 20;
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroData.FoodUsed >= 15)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] < 5)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] = 5;
                }
            }

            if (MacroData.FoodUsed >= 50)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] = 2;
                }
            }
        }
    }
}
