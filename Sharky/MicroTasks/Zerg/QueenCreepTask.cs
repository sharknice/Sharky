using SC2APIProtocol;
using Sharky.Builds;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.MicroControllers.Zerg;
using System.Collections;
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
        IBuildingPlacement ZergBuildingPlacement;
        BuildingService BuildingService;

        Dictionary<UnitCommander, Point2D> CreepPoints = new Dictionary<UnitCommander, Point2D>();

        public QueenCreepTask(DefaultSharkyBot defaultSharkyBot, float priority, QueenMicroController queenMicroController, bool enabled)
        {
            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            CreepTumorPlacementFinder = defaultSharkyBot.CreepTumorPlacementFinder;
            ZergBuildingPlacement = defaultSharkyBot.ZergBuildingPlacement;
            QueenMicroController = queenMicroController;
            BuildOptions = defaultSharkyBot.BuildOptions;
            BuildingService = defaultSharkyBot.BuildingService;

            Priority = priority;
            Enabled = enabled;

            CommanderDebugColor = new SC2APIProtocol.Color() { R = 255, G = 32, B = 127 };
            CommanderDebugText = "Creep";
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
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

            var pos = CreepTumorPlacementFinder.FindTumorPlacement(frame, UnitCommanders, UnitCommanders.Any(c => c.UnitCalculation.Unit.Energy >= 30));

            if (pos == null)
            {
                return actions;
            }

            foreach (var queen in UnitCommanders)
            {
                if (frame - queen.LastOrderFrame > 5)
                {
                    if (PlaceCreep(frame, actions, queen, pos)) continue;
                    else
                    {
                        MoveQueen(frame, actions, queen, pos);
                    }
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
                var spot = ZergBuildingPlacement.FindPlacement(pos, UnitTypes.ZERG_CREEPTUMORQUEEN, 3, maxDistance: 10, ignoreResourceProximity: true, allowBlockBase: false, requireVision: true);
                if (spot != null)
                {
                    actions.AddRange(queen.Order(frame, Abilities.BUILD_CREEPTUMOR_QUEEN, spot));
                }
                else if (!BuildingService.BlocksResourceCenter(pos.X, pos.Y, 1))
                {
                    actions.AddRange(queen.Order(frame, Abilities.BUILD_CREEPTUMOR_QUEEN, pos));
                }
            }

            return true;
        }
    }
}
