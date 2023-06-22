using SC2APIProtocol;
using Sharky.Builds;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.MicroControllers;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks.Zerg
{
    public class BurrowBlockExpansionsTask : MicroTask
    {
        EnemyData EnemyData;
        IndividualMicroController IndividualMicroController;
        BuildOptions BuildOptions;
        SharkyUnitData UnitData;
        BaseData BaseData;

        public BurrowBlockExpansionsTask(DefaultSharkyBot defaultSharkyBot, float priority, IndividualMicroController individualMicroController, SharkyUnitData unitData, bool enabled)
        {
            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            UnitData = unitData;
            BaseData = defaultSharkyBot.BaseData;
            IndividualMicroController = individualMicroController;

            Priority = priority;
            Enabled = enabled;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace != SC2APIProtocol.Race.Zerg)
            {
                Disable();
                return;
            }

            if (!UnitData.ResearchedUpgrades.Contains((uint)Upgrades.BURROW) || EnemyData.EnemyAggressivityData.ArmyAggressivity > 0.5f)
            {
                return;
            }

            foreach (var commander in commanders.Where(commander => ((commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLINGBURROWED) && (!commander.Value.Claimed || commander.Value.UnitRole == UnitRole.Attack))))
            {
                if (UnitCommanders.Count >= BuildOptions.ZergBuildOptions.MaxBurrowedBlockingZerglings)
                    break;

                commander.Value.Claimed = true;
                commander.Value.UnitRole = UnitRole.BlockExpansion;
                UnitCommanders.Add(commander.Value);
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (!UnitCommanders.Any())
                return actions;

            var points = GetBlockingPoints();

            foreach (var ling in UnitCommanders)
            {
                var point = points.FirstOrDefault();
                points = points.Skip(1);

                if (!FreeOurExpansions(ling, frame, point, actions) && point != null)
                {
                    if (point != null)
                    {
                        actions.AddRange(BlockExpansion(ling, point, frame));
                    }
                }
            }

            return actions;
        }

        private bool FreeOurExpansions(UnitCommander ling, int frame, Point2D pos, List<SC2APIProtocol.Action> actions)
        {
            if ((ling.UnitCalculation.Unit.IsBurrowed || (pos != null && pos.ToVector2().Distance(ling.UnitCalculation.Position) < 6))
                && ling.UnitCalculation.NearbyAllies.Any(x => x.Unit.UnitType == (uint)UnitTypes.ZERG_DRONE && x.Position.Distance(ling.UnitCalculation.Position) < 8))
            {
                actions.AddRange(ling.Order(frame, Abilities.BURROWUP_ZERGLING));
                return true;
            }

            return false;
        }

        private IEnumerable<SC2APIProtocol.Action> BlockExpansion(UnitCommander ling, Point2D pos, int frame)
        {
            if (pos == null)
                return new List<SC2APIProtocol.Action>();

            var distanceFromTarget = pos.Distance(ling.UnitCalculation.Unit.Pos.ToPoint2D());

            if (distanceFromTarget > 1.5f)
            {
                if (ling.UnitCalculation.Unit.IsBurrowed && distanceFromTarget > 10 && (ling.UnitCalculation.NearbyAllies.Any(z => z.Unit.UnitType == (int)UnitTypes.ZERG_ZERGLINGBURROWED && z.Position.Distance(ling.UnitCalculation.Position) < 5) || distanceFromTarget > 36))
                {
                    return ling.Order(frame, Abilities.BURROWUP_ZERGLING);
                }

                return IndividualMicroController.NavigateToPoint(ling, pos, pos, pos, frame);
            }
            else
            {
                return ling.Order(frame, Abilities.BURROWDOWN_ZERGLING);
            }
        }

        private IEnumerable<Point2D> GetBlockingPoints()
        {
            var points = BaseData.EnemyBaseLocations.Where(b => !BaseData.EnemyBases.Contains(b)).Select(b => b.Location).Take(BuildOptions.ZergBuildOptions.MaxBurrowedBlockingZerglings);

            return points;
        }
    }
}
