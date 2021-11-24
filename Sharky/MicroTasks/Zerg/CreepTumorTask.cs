using Sharky.Chat;
using Sharky.DefaultBot;
using Sharky.MicroTasks.Zerg;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks
{
    public class CreepTumorTask : MicroTask
    {
        EnemyData EnemyData;
        SharkyOptions SharkyOptions;

        CreepTumorPlacementFinder CreepTumorPlacementFinder;

        public CreepTumorTask(DefaultSharkyBot defaultSharkyBot, int desiredCreepSpreaders, float priority, bool enabled = true)
        {
            EnemyData = defaultSharkyBot.EnemyData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            CreepTumorPlacementFinder = defaultSharkyBot.CreepTumorPlacementFinder;

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

            return actions;
        }

        IEnumerable<SC2APIProtocol.Action> SpreadCreep(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitCalculation.Unit.Orders.Count() == 0 && commander.LastAbility != Abilities.BUILD_CREEPTUMOR_TUMOR && frame - commander.UnitCalculation.FrameFirstSeen > SharkyOptions.FramesPerSecond * 22)
                {
                    if (commander.UnitCalculation.EnemiesInRangeOf.Count() > 0)
                    {
                        continue; // TODO: don't suicide and stuff
                    }
                    var spot = CreepTumorPlacementFinder.FindTumorExtensionPlacement(commander.UnitCalculation.Position);
                    if (spot == null)
                    {
                        spot = new SC2APIProtocol.Point2D { X = commander.UnitCalculation.Position.X, Y = commander.UnitCalculation.Position.Y };
                    }
                    if (spot != null)
                    {
                        var action = commander.Order(frame, Abilities.BUILD_CREEPTUMOR_TUMOR, spot);
                        if (action != null)
                        {
                            actions.AddRange(action);
                        }
                    }
                }
            }

            return actions;
        }
    }
}
