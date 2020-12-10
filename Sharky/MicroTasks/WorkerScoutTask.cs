using Sharky.Managers;
using Sharky.MicroControllers;
using Sharky.Pathing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks
{
    public class WorkerScoutTask : MicroTask
    {
        UnitDataManager UnitDataManager;
        ITargetingManager TargetingManager;
        MapDataService MapDataService;
        IIndividualMicroController IndividualMicroController;

        bool started { get; set; }

        public WorkerScoutTask(UnitDataManager unitDataManager, ITargetingManager targetingManager, MapDataService mapDataService, bool enabled, float priority, IIndividualMicroController individualMicroController)
        {
            UnitDataManager = unitDataManager;
            TargetingManager = targetingManager;
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
                        if (commander.Value.UnitCalculation.Unit.Orders.Any(o => !UnitDataManager.MiningAbilities.Contains((Abilities)o.AbilityId)))
                        {
                        }
                        else
                        {
                            commander.Value.Claimed = true;
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
                var action = IndividualMicroController.Scout(commander, TargetingManager.EnemyMainBasePoint, TargetingManager.ForwardDefensePoint, frame);
                if (action != null)
                {
                    commands.Add(action);
                }
            }

            return commands;
        }
    }
}
