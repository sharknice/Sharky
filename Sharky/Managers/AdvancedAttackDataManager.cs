using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.MicroTasks;
using Sharky.MicroTasks.Attack;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class AdvancedAttackDataManager : AttackDataManager
    {
        AttackData AttackData;
        MacroData MacroData;
        AdvancedAttackService AdvancedAttackService;

        public AdvancedAttackDataManager(DefaultSharkyBot defaultSharkyBot, AdvancedAttackService advancedAttackService, IMicroTask attackTask) :
            base(defaultSharkyBot.AttackData, defaultSharkyBot.ActiveUnitData, attackTask, defaultSharkyBot.TargetPriorityService, defaultSharkyBot.TargetingData, defaultSharkyBot.MacroData, defaultSharkyBot.BaseData, defaultSharkyBot.DebugService)
        {
            AttackData = defaultSharkyBot.AttackData;
            MacroData = defaultSharkyBot.MacroData;
            AdvancedAttackService = advancedAttackService;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            AttackData.CustomAttackFunction = true;
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            if (AttackData.UseAttackDataManager)
            {
                AttackData.Attacking = AdvancedAttackService.Attack();
            }
            else if (!AttackData.CustomAttackFunction)
            {
                if (AttackData.Attacking)
                {
                    if (MacroData.FoodArmy < AttackData.ArmyFoodRetreat)
                    {
                        AttackData.Attacking = false;
                    }
                }
                else
                {
                    AttackData.Attacking = MacroData.FoodArmy >= AttackData.ArmyFoodAttack || MacroData.FoodUsed > 190;
                }
            }

            return new List<SC2APIProtocol.Action>();
        }
    }
}
