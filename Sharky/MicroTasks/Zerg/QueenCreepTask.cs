using Sharky.DefaultBot;
using Sharky.MicroTasks.Zerg;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks
{
    public class QueenCreepTask : MicroTask
    {
        EnemyData EnemyData;
        CreepTumorPlacementFinder CreepTumorPlacementFinder;

        public QueenCreepTask(DefaultSharkyBot defaultSharkyBot, float priority, bool enabled = true)
        {
            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            CreepTumorPlacementFinder = defaultSharkyBot.CreepTumorPlacementFinder;

            Priority = priority;
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders.Where(commander => (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEENBURROWED) && !commander.Value.Claimed))
            {
                commander.Value.Claimed = true;
                commander.Value.UnitRole = UnitRole.SpreadCreep;
                UnitCommanders.Add(commander.Value);

                // Max 6 creep queens
                if (UnitCommanders.Count >= 6)
                    return;
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (EnemyData.SelfRace != SC2APIProtocol.Race.Zerg)
            {
                Disable();
                return actions;
            }

            RemoveLarvaQueens(frame);

            return SpreadCreep(frame);
        }

        IEnumerable<SC2APIProtocol.Action> SpreadCreep(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var queen in UnitCommanders)
            {
                if (queen.UnitCalculation.EnemiesThreateningDamage.Count > 0 && queen.UnitRole == UnitRole.SpreadCreep)
                {
                    queen.Claimed = false;
                    queen.UnitRole = UnitRole.Defend;
                }
                else if (queen.UnitCalculation.Unit.Energy >= 25 && frame - queen.LastOrderFrame > 22 && !queen.UnitCalculation.Unit.Orders.Any())
                {
                    var pos = CreepTumorPlacementFinder.FindTumorPlacement(frame, UnitCommanders, queen.UnitCalculation.Unit.Energy >= 30, UnitCommanders.Count <= 2 && frame < 22 * 60 * 5);

                    if (pos == null)
                        return actions;

                    var action = queen.Order(frame, Abilities.BUILD_CREEPTUMOR_QUEEN, pos);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }

            return actions;
        }

        private void RemoveLarvaQueens(int frame)
        {
            // Remove queens taken for hatchery injecting which is more important
            UnitCommanders.RemoveAll(u => u.UnitRole == UnitRole.SpawnLarva);
        }
    }
}
