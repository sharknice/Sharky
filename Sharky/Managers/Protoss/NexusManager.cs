using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers.Protoss
{
    public class NexusManager : SharkyManager
    {
        ActiveUnitData ActiveUnitData;
        UnitDataManager UnitDataManager;
        ChronoData ChronoData;

        public NexusManager(ActiveUnitData activeUnitData, UnitDataManager unitDataManager, ChronoData chronoData)
        {
            ActiveUnitData = activeUnitData;
            UnitDataManager = unitDataManager;
            ChronoData = chronoData;
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var nexus = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS).OrderByDescending(c => c.UnitCalculation.Unit.Energy).FirstOrDefault();
            if (nexus != null)
            {
                var action = ChronoBoost(nexus, (int)observation.Observation.GameLoop);
                if (action != null)
                {
                    actions.Add(action);
                }
            }

            return actions;
        }

        SC2APIProtocol.Action ChronoBoost(UnitCommander nexus, int frame)
        {
            if (nexus.UnitCalculation.Unit.Energy >= 50)
            {
                foreach (var upgrade in ChronoData.ChronodUpgrades)
                {
                    var upgradeData = UnitDataManager.UpgradeData[upgrade];
                    var building = ActiveUnitData.SelfUnits.Where(u => !u.Value.Unit.BuffIds.Contains((uint)Buffs.CHRONOBOOST) && upgradeData.ProducingUnits.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.Orders.Any(o => o.AbilityId == (uint)upgradeData.Ability)).FirstOrDefault().Value;
                    if (building != null)
                    {
                        return nexus.Order(frame, Abilities.CHRONOBOOST, null, building.Unit.Tag);
                    }
                }

                foreach (var unit in ChronoData.ChronodUnits)
                {
                    var trainingData = UnitDataManager.TrainingData[unit];
                    var building = ActiveUnitData.SelfUnits.Where(u => !u.Value.Unit.BuffIds.Contains((uint)Buffs.CHRONOBOOST) && trainingData.ProducingUnits.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.Orders.Any(o => o.AbilityId == (uint)trainingData.Ability)).FirstOrDefault().Value;
                    if (building != null)
                    {
                        return nexus.Order(frame, Abilities.CHRONOBOOST, null, building.Unit.Tag);
                    }
                }
            }

            return null;
        }
    }
}
