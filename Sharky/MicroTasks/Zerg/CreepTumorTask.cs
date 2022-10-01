using Sharky.DefaultBot;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks.Zerg
{
    public class CreepTumorTask : MicroTask
    {
        EnemyData EnemyData;
        SharkyOptions SharkyOptions;
        QueenCreepTask QueenCreepAndDefendTask;

        CreepTumorPlacementFinder CreepTumorPlacementFinder;

        public CreepTumorTask(DefaultSharkyBot defaultSharkyBot, QueenCreepTask queenCreepAndDefendTask, int desiredCreepSpreaders, float priority, bool enabled)
        {
            EnemyData = defaultSharkyBot.EnemyData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            CreepTumorPlacementFinder = defaultSharkyBot.CreepTumorPlacementFinder;
            QueenCreepAndDefendTask = queenCreepAndDefendTask;

            UnitCommanders = new List<UnitCommander>();

            Priority = priority;
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders.Where(c => !c.Value.Claimed && c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_CREEPTUMORBURROWED && c.Value.UnitCalculation.Unit.BuildProgress == 1))
            {
                commander.Value.UnitRole = UnitRole.SpreadCreep;
                commander.Value.Claimed = true;
                UnitCommanders.Add(commander.Value);
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

            actions.AddRange(SpreadCreep(frame));

            UnitCommanders.RemoveAll(x => x.LastAbility == Abilities.BUILD_CREEPTUMOR_TUMOR);

            return actions;
        }

        IEnumerable<SC2APIProtocol.Action> SpreadCreep(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var commander in UnitCommanders)
            {
                if ((commander.UnitCalculation.Unit.Orders.Count() == 0) && (frame - commander.UnitCalculation.FrameFirstSeen > SharkyOptions.FramesPerSecond * 22))
                {
                    if (commander.UnitCalculation.EnemiesInRangeOf.Count() > 0)
                    {
                        continue; // Don't suicide and stuff
                    }

                    var spot = CreepTumorPlacementFinder.FindTumorExtensionPlacement(frame, QueenCreepAndDefendTask.UnitCommanders, commander.UnitCalculation.Position, true, UnitCommanders.Count < 3);

                    if (spot == null)
                    {
                        spot = new SC2APIProtocol.Point2D { X = commander.UnitCalculation.Position.X, Y = commander.UnitCalculation.Position.Y };
                    }
                    
                    var action = commander.Order(frame, Abilities.BUILD_CREEPTUMOR_TUMOR, spot);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }

                }
            }

            return actions;
        }
    }
}
