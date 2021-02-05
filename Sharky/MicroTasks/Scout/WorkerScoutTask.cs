using Sharky.MicroControllers;
using Sharky.Pathing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks
{
    public class WorkerScoutTask : MicroTask
    {
        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        MapDataService MapDataService;
        IIndividualMicroController IndividualMicroController;

        bool started { get; set; }

        public WorkerScoutTask(SharkyUnitData sharkyUnitData, TargetingData targetingData, MapDataService mapDataService, bool enabled, float priority, IIndividualMicroController individualMicroController)
        {
            SharkyUnitData = sharkyUnitData;
            TargetingData = targetingData;
            MapDataService = mapDataService;
            Priority = priority;
            IndividualMicroController = individualMicroController;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() == 0)
            {
                if (started)
                {
                    Disable();
                    return;
                }

                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker))
                    {
                        if (commander.Value.UnitCalculation.Unit.Orders.Any(o => !SharkyUnitData.MiningAbilities.Contains((Abilities)o.AbilityId)))
                        {
                        }
                        else
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Scout;
                            UnitCommanders.Add(commander.Value);
                            started = true;
                            return;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            foreach (var commander in UnitCommanders)
            {
                var action = IndividualMicroController.Scout(commander, TargetingData.EnemyMainBasePoint, TargetingData.ForwardDefensePoint, frame, true);
                if (action != null)
                {
                    commands.AddRange(action);
                }
            }

            return commands;
        }
    }
}
