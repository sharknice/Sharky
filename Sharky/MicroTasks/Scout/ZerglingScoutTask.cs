using SC2APIProtocol;
using Sharky.DefaultBot;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks
{
    public class ZerglingScoutTask : MicroTask
    {
        TargetingData TargetingData;
        EnemyData EnemyData;
        MicroTaskData MicroTaskData;

        int framesSinceLastMainScout = 0;
        bool scoutMain = false;

        public ZerglingScoutTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            EnemyData = defaultSharkyBot.EnemyData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        private void Claim(ConcurrentDictionary<ulong, UnitCommander> commanders, bool allowSteal)
        {
            if (EnemyData.SelfRace != Race.Zerg)
                return;

            foreach (var commander in commanders)
            {
                if ((!commander.Value.Claimed || allowSteal) && commander.Value.UnitCalculation.Unit.UnitType == (int)UnitTypes.ZERG_ZERGLING)
                {
                    // Remove from other microtasks
                    if (commander.Value.Claimed)
                    {
                        foreach (var task in MicroTaskData.MicroTasks.Values)
                        {
                            task.StealUnit(commander.Value);
                        }
                    }

                    commander.Value.Claimed = true;
                    commander.Value.UnitRole = UnitRole.Scout;
                    UnitCommanders.Add(commander.Value);

                    scoutMain = false;

                    if (UnitCommanders.Count >= 1)
                        return;
                }
            }
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() == 0)
            {
                Claim(commanders, false);

                if (UnitCommanders.Count() == 0)
                {
                    Claim(commanders, true);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            foreach (var commander in UnitCommanders)
            {
                if (framesSinceLastMainScout > 22.4f * 40)
                {
                    scoutMain = true;
                    framesSinceLastMainScout = 0;
                }

                Point2D scoutPos = scoutMain ? TargetingData.EnemyMainBasePoint : TargetingData.NaturalFrontScoutPoint;

                var action = commander.Order(frame, Abilities.MOVE, scoutPos);
                if (action != null)
                {
                    commands.AddRange(action);
                }
            }

            framesSinceLastMainScout += 1;

            return commands;
        }
    }
}
