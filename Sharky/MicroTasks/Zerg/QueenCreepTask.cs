using SC2APIProtocol;
using Sharky.Builds;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.MicroControllers.Zerg;
using Sharky.MicroTasks.Zerg;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks.Zerg
{
    public class QueenCreepTask : MicroTask
    {
        EnemyData EnemyData;
        CreepTumorPlacementFinder CreepTumorPlacementFinder;
        QueenMicroController QueenMicroController;
        BuildOptions BuildOptions;

        Dictionary<UnitCommander, Point2D> CreepPoints = new Dictionary<UnitCommander, Point2D>();

        public QueenCreepTask(DefaultSharkyBot defaultSharkyBot, float priority, QueenMicroController queenMicroController, bool enabled)
        {
            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            CreepTumorPlacementFinder = defaultSharkyBot.CreepTumorPlacementFinder;
            QueenMicroController = queenMicroController;
            BuildOptions = defaultSharkyBot.BuildOptions;

            Priority = priority;
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace != SC2APIProtocol.Race.Zerg)
            {
                Disable();
                return;
            }

            UnitCommanders.RemoveAll(q => q.UnitRole != UnitRole.SpreadCreep);

            foreach (var commander in commanders.Where(commander => (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEENBURROWED) && !commander.Value.Claimed))
            {
                if (UnitCommanders.Count >= BuildOptions.ZergBuildOptions.MaxCreepQueens)
                    break;

                commander.Value.Claimed = true;
                commander.Value.UnitRole = UnitRole.SpreadCreep;
                UnitCommanders.Add(commander.Value);
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var queen in UnitCommanders)
            {
                var pos = CreepTumorPlacementFinder.FindTumorPlacement(frame, UnitCommanders, queen.UnitCalculation.Unit.Energy >= 30, UnitCommanders.Count <= 2 && frame < 22 * 60 * 5);

                if (pos == null)
                {
                    return actions;
                }

                if (PlaceCreep(frame, actions, queen, pos)) continue;
                else
                {
                    MoveQueen(frame, actions, queen, pos);
                }
            }

            return actions;
        }

        private bool MoveQueen(int frame, List<SC2APIProtocol.Action> actions, UnitCommander queen, Point2D pos)
        {
            actions.AddRange(QueenMicroController.NavigateToPoint(queen, pos, pos, null, frame));
            return true;
        }

        private bool PlaceCreep(int frame, List<SC2APIProtocol.Action> actions, UnitCommander queen, Point2D pos)
        {
            if (queen.UnitCalculation.Unit.Energy < 25 || queen.UnitCalculation.Position.DistanceSquared(pos.ToVector2()) > 1 || queen.UnitCalculation.EnemiesInRangeOf.Count > 0)
                return false;

            if (frame - queen.LastOrderFrame > 5)
            {
                actions.AddRange(queen.Order(frame, Abilities.BUILD_CREEPTUMOR_QUEEN, pos));
            }

            return true;
        }
    }
}
