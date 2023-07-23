using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.Pathing;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

namespace Sharky.MicroTasks.Zerg
{
    public class CreepTumorTask : MicroTask
    {
        EnemyData EnemyData;
        SharkyOptions SharkyOptions;

        CreepTumorPlacementFinder CreepTumorPlacementFinder;
        DebugService DebugService;

        Dictionary<UnitCommander, Point2D> debugPos = new Dictionary<UnitCommander, Point2D>();

        public CreepTumorTask(DefaultSharkyBot defaultSharkyBot, float priority, bool enabled)
        {
            EnemyData = defaultSharkyBot.EnemyData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            CreepTumorPlacementFinder = defaultSharkyBot.CreepTumorPlacementFinder;
            DebugService = defaultSharkyBot.DebugService;

            UnitCommanders = new List<UnitCommander>();

            Priority = priority;
            Enabled = enabled;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders.Where(c => !c.Value.Claimed && c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_CREEPTUMORBURROWED && c.Value.UnitCalculation.Unit.BuildProgress == 1))
            {
                commander.Value.UnitRole = UnitRole.SpreadCreepWait;
                commander.Value.Claimed = true;
                UnitCommanders.Add(commander.Value);
            }

            //Debug();
        }

        private void Debug()
        {
            foreach (var pos in debugPos.Values)
            {
                if (pos is not null)
                {
                    for (int i=6; i<=12; i++)
                    DebugService.DrawSphere(pos.ToPoint(i), 0.5f);
                }
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
                if (!commander.UnitCalculation.Unit.Orders.Any() && ((frame - commander.UnitCalculation.FrameFirstSeen) > SharkyOptions.FramesPerSecond * 24.5f))
                {
                    if (commander.UnitCalculation.EnemiesInRangeOf.Count() > 0)
                    {
                        continue; // Don't spread if enemy is close
                    }

                    var spot = CreepTumorPlacementFinder.FindTumorExtensionPlacement(frame, commander.UnitCalculation.Position);

                    if (spot != null)
                    {
                        commander.UnitRole = UnitRole.SpreadCreepCast;
                        debugPos[commander] = spot;
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
