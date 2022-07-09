using Sharky.Chat;
using Sharky.DefaultBot;
using Sharky.MicroTasks.Zerg;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class QueenDefendTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        EnemyData EnemyData;
        CreepTumorPlacementFinder CreepTumorPlacementFinder;

        public QueenDefendTask(DefaultSharkyBot defaultSharkyBot, float priority, bool enabled = true)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;

            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            CreepTumorPlacementFinder = defaultSharkyBot.CreepTumorPlacementFinder;

            Priority = priority;
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            return;
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
            return actions;

            // TODO: 
            if (EnemyData.SelfRace != SC2APIProtocol.Race.Zerg)
            {
                Disable();
                return actions;
            }

            RemoveLarvaQueens(frame);
        }

        private void RemoveLarvaQueens(int frame)
        {
            // Remove queens taken for hatchery injecting which is more important
            UnitCommanders.RemoveAll(u => u.UnitRole == UnitRole.SpawnLarva);
        }
    }
}
