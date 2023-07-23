using SC2APIProtocol;
using Sharky.Builds;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.MicroControllers.Zerg;
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
        DebugService DebugService;

        Dictionary<UnitCommander, Point2D> QueensTumors = new Dictionary<UnitCommander, Point2D>();

        public QueenCreepTask(DefaultSharkyBot defaultSharkyBot, float priority, QueenMicroController queenMicroController, bool enabled)
        {
            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            CreepTumorPlacementFinder = defaultSharkyBot.CreepTumorPlacementFinder;
            ZergBuildingPlacement = defaultSharkyBot.ZergBuildingPlacement;
            QueenMicroController = queenMicroController;
            BuildOptions = defaultSharkyBot.BuildOptions;
            BuildingService = defaultSharkyBot.BuildingService;
            DebugService = defaultSharkyBot.DebugService;

            Priority = priority;
            Enabled = enabled;

            CommanderDebugColor = new Color() { R = 255, G = 32, B = 127 };
            CommanderDebugText = "Creep";
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace != Race.Zerg)
            {
                Disable();
                return;
            }

            // First remove queens that do not have creep spread unit roles, but are already claimed
            UnitCommanders.RemoveAll(q => q.UnitRole != UnitRole.SpreadCreepWait && q.UnitRole != UnitRole.SpreadCreepCast && q.UnitRole != UnitRole.SpreadCreepWalk);

            // Claim all available queens that do not have any other task yet
            foreach (var commander in commanders.Where(commander => (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_QUEENBURROWED) && !commander.Value.Claimed))
            {
                if (UnitCommanders.Count >= BuildOptions.ZergBuildOptions.MaxCreepQueens)
                    break;

                commander.Value.Claimed = true;
                commander.Value.UnitRole = UnitRole.SpreadCreepWait;
                UnitCommanders.Add(commander.Value);
            }

            //Debug();
        }

        private void RemoveQueenCreepTargetFromCreepMap(float x, float y)
        {
            CreepTumorPlacementFinder.RemoveQueenTarget((int)x, (int)y);
        }

        private void AddQueenCreepTargetFromCreepMap(float x, float y)
        {
            CreepTumorPlacementFinder.AddQueenTarget((int)x, (int)y);
        }

        private void Debug()
        {
            CreepTumorPlacementFinder.DebugCreepSpread(DebugService);

            foreach (var action in QueensTumors)
            {
                DebugService.DrawLine(action.Value.ToPoint(12), action.Key.UnitCalculation.Position.ToPoint(16), new Color() { R = 255, G=255, B = 255 });

                for (int i = 6; i<=12; i++)
                {
                    DebugService.DrawSphere(action.Value.ToPoint(i), 0.5f);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            CreepTumorPlacementFinder.UpdateNewCreepSources(frame);

            // Find placement for new tumor
            var newTumorPos = CreepTumorPlacementFinder.FindTumorPlacement(frame);

            foreach (var queen in UnitCommanders)
            {
                // Issue new commands at least 5 frames after the previous one
                if (frame - queen.LastOrderFrame <= 5) continue;

                // Let the queen to pick spread creep position when it is close to use it so the point is more optimal
                if (queen.UnitCalculation.Unit.Energy <= 20) continue;

                // Check if this creep queen has tumor position assigned
                if (!QueensTumors.TryGetValue(queen, out var tumorPosition))
                {
                    // Queen tumor not assigned
                    if (newTumorPos is not null)
                    {
                        tumorPosition = newTumorPos;
                        newTumorPos = null;
                    }
                }
                else
                {
                    // Check if tumor position is valid, if not, remove
                    if (!CreepTumorPlacementFinder.IsValidCreepTumorPosition((int)tumorPosition.X, (int)tumorPosition.Y))
                    {
                        RemoveQueenCreepTargetFromCreepMap(tumorPosition.X, tumorPosition.Y);
                        QueensTumors.Remove(queen);
                        queen.UnitRole = UnitRole.SpreadCreepWait;
                        continue;
                    }
                }

                // Queen has no assigned point
                if (tumorPosition is null)
                {
                    queen.UnitRole = UnitRole.SpreadCreepWait;
                    QueensTumors.Remove(queen);
                    continue;
                }

                if (queen.UnitRole == UnitRole.SpreadCreepCast)
                {
                    if (!queen.UnitCalculation.Unit.Orders.Any() || queen.UnitCalculation.Unit.Orders.FirstOrDefault().AbilityId != (uint)Abilities.BUILD_CREEPTUMOR_QUEEN || frame - queen.LastOrderFrame > 48)
                    {
                        RemoveQueenCreepTargetFromCreepMap(tumorPosition.X, tumorPosition.Y);
                        queen.UnitRole = UnitRole.SpreadCreepWait;
                        QueensTumors.Remove(queen);
                    }

                    continue;
                }
                else if (queen.UnitRole == UnitRole.SpreadCreepWalk)
                {
                    if (PlaceCreep(frame, actions, queen, tumorPosition))
                    {
                        continue;
                    }
                }
                else if (queen.UnitRole == UnitRole.SpreadCreepWait)
                {
                    AddQueenCreepTargetFromCreepMap(tumorPosition.X, tumorPosition.Y);
                    QueensTumors.Add(queen, tumorPosition);
                    queen.UnitRole = UnitRole.SpreadCreepWalk;
                }

                MoveQueen(frame, actions, queen, tumorPosition);
            }

            return actions;
        }

        /// <summary>
        /// Walks the queen to given position, but allows the queen to use transfusions or attacking along the way
        /// </summary>
        private void MoveQueen(int frame, List<SC2APIProtocol.Action> actions, UnitCommander queen, Point2D pos)
        {
            actions.AddRange(QueenMicroController.NavigateToPoint(queen, pos, pos, null, frame));
            queen.UnitRole = UnitRole.SpreadCreepWalk;
        }

        /// <summary>
        /// Places the tumor if the queen is close enough. If the queen is far or hhas not energy, returns false.
        /// </summary>
        private bool PlaceCreep(int frame, List<SC2APIProtocol.Action> actions, UnitCommander queen, Point2D pos)
        {
            if (queen.UnitCalculation.Unit.Energy < 25 || queen.UnitCalculation.Position.DistanceSquared(pos.ToVector2()) > 1)
                return false;

            if (frame - queen.LastOrderFrame > 5)
            {
                var spot = ZergBuildingPlacement.FindPlacement(pos, UnitTypes.ZERG_CREEPTUMORQUEEN, 3, maxDistance: 10, ignoreResourceProximity: true, allowBlockBase: false, requireVision: true);
                if (spot != null)
                {
                    actions.AddRange(queen.Order(frame, Abilities.BUILD_CREEPTUMOR_QUEEN, spot));
                }
                else if (!BuildingService.BlocksResourceCenter(pos.X, pos.Y, 1)) // Maybe remove ?
                {
                    actions.AddRange(queen.Order(frame, Abilities.BUILD_CREEPTUMOR_QUEEN, pos));
                }
                queen.UnitRole = UnitRole.SpreadCreepCast;
            }

            return true;
        }

        public override void StealUnit(UnitCommander commander)
        {
            if (QueensTumors.ContainsKey(commander))
            {
                var tumorPosition = QueensTumors[commander];
                RemoveQueenCreepTargetFromCreepMap(tumorPosition.X, tumorPosition.Y);
            }
            QueensTumors.Remove(commander);
            base.StealUnit(commander);
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var deadUnit in deadUnits)
            {
                var commander = QueensTumors.Keys.FirstOrDefault(x => x.UnitCalculation.Unit.Tag == deadUnit);

                if (commander is not null)
                {
                    var queenPos = QueensTumors[commander];
                    RemoveQueenCreepTargetFromCreepMap(queenPos.X, queenPos.Y);
                    QueensTumors.Remove(commander);
                }
            }

            CreepTumorPlacementFinder.RemoveDeadUnits(deadUnits);

            base.RemoveDeadUnits(deadUnits);
        }
    }
}
