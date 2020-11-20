using Sharky.Managers;
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

        bool Enabled { get; set; }

        bool started { get; set; }

        public WorkerScoutTask(UnitDataManager unitDataManager, ITargetingManager targetingManager, MapDataService mapDataService, bool enabled, float priority)
        {
            UnitDataManager = unitDataManager;
            TargetingManager = targetingManager;
            MapDataService = mapDataService;
            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public void Enable()
        {
            Enabled = true;
        }

        public void Disable()
        {
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
            }
            UnitCommanders = new List<UnitCommander>();

            Enabled = false;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (Enabled)
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
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            commands.AddRange(ScoutEnemyMain(frame));

            return commands;
        }

        IEnumerable<SC2APIProtocol.Action> ScoutEnemyMain(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (!MapDataService.SelfVisible(TargetingManager.EnemyMainBasePoint))
            {
                foreach (var commander in UnitCommanders)
                {
                    var action = commander.Order(frame, Abilities.MOVE, TargetingManager.EnemyMainBasePoint);
                    if (action != null)
                    {
                        commands.Add(action);
                    }
                }
            }
            else
            {
                // TODO: circle around base
            }

            return commands;
        }
    }
}
