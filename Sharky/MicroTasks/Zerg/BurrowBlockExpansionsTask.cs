using SC2APIProtocol;
using Sharky.Builds;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroTasks
{
    public class BurrowBlockExpansionsTask : MicroTask
    {
        EnemyData EnemyData;
        IndividualMicroController IndividualMicroController;
        BuildOptions BuildOptions;
        SharkyUnitData UnitData;
        BaseData BaseData;
        ActiveUnitData ActiveUnitData;

        public BurrowBlockExpansionsTask(DefaultSharkyBot defaultSharkyBot, float priority, IndividualMicroController individualMicroController, SharkyUnitData unitData, bool enabled = true)
        {
            UnitCommanders = new List<UnitCommander>();
            EnemyData = defaultSharkyBot.EnemyData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            UnitData = unitData;
            BaseData = defaultSharkyBot.BaseData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            IndividualMicroController = individualMicroController;

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

            if (!UnitData.ResearchedUpgrades.Contains((uint)Upgrades.BURROW))
            {
                return;
            }

            foreach (var commander in commanders.Where(commander => (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING && !commander.Value.Claimed)))
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
                if (!points.Any())
                    return actions;

                if (ling.UnitCalculation.Unit.IsBurrowed)
                    continue;

                var point = points.First();
                points = points.Skip(1);

                if (point.Distance(ling.UnitCalculation.Unit.Pos.ToPoint2D()) > 1)
                {
                    actions.AddRange(IndividualMicroController.NavigateToPoint(ling, point, point, point, frame));
                }
                else
                {
                    actions.AddRange(ling.Order(frame, Abilities.BURROWDOWN_ZERGLING));
                }

                // TODO: unblock bases we want to expand to
            }

            return actions;
        }

        private IEnumerable<Point2D> GetBlockingPoints()
        {
            var points = BaseData.EnemyBaseLocations.Where(b => !BaseData.EnemyBases.Contains(b)).Select(b => b.Location).Take(BuildOptions.ZergBuildOptions.MaxBurrowedBlockingZerglings);

            return points;
        }
    }
}
