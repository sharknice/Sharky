using SC2APIProtocol;
using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class ProxyScoutTask : MicroTask
    {
        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        BaseData BaseData;
        SharkyOptions SharkyOptions;
        IIndividualMicroController IndividualMicroController;

        bool started { get; set; }

        List<Point2D> ScoutLocations { get; set; }
        int ScoutLocationIndex { get; set; }

        bool LateGame;

        public ProxyScoutTask(SharkyUnitData sharkyUnitData, TargetingData targetingData, BaseData baseData, SharkyOptions sharkyOptions, bool enabled, float priority, IIndividualMicroController individualMicroController)
        {
            SharkyUnitData = sharkyUnitData;
            TargetingData = targetingData;
            BaseData = baseData;
            SharkyOptions = sharkyOptions;
            Priority = priority;
            IndividualMicroController = individualMicroController;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
            LateGame = false;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() == 0)
            {
                if (started)
                {
                    Disable();
                    return;
                }

                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker))
                    {
                        if (commander.Value.UnitCalculation.Unit.Orders.Any(o => !SharkyUnitData.MiningAbilities.Contains((Abilities)o.AbilityId)))
                        {
                        }
                        else
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Scout;
                            UnitCommanders.Add(commander.Value);
                            started = true;
                            return;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            if (ScoutLocations == null)
            {
                GetScoutLocations();
            }
            if (!LateGame && frame > SharkyOptions.FramesPerSecond * 4 * 60)
            {
                LateGame = true;
                ScoutLocations = new List<Point2D>();
                foreach (var baseLocation in BaseData.BaseLocations.Where(b => !BaseData.SelfBases.Any(s => s.Location == b.Location) && !BaseData.EnemyBases.Any(s => s.Location == b.Location)))
                {
                    ScoutLocations.Add(baseLocation.MineralLineLocation);
                }
                ScoutLocationIndex = 0;
            }

            var commands = new List<SC2APIProtocol.Action>();

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitRole != UnitRole.Scout) { commander.UnitRole = UnitRole.Scout; }
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker) || e.Attributes.Contains(Attribute.Structure)))
                {
                    // TODO: attack scv that is building something, then any worker, then buildlng
                    var enemy = commander.UnitCalculation.NearbyEnemies.FirstOrDefault();
                    var action = IndividualMicroController.Attack(commander, new Point2D { X = enemy.Unit.Pos.X, Y = enemy.Unit.Pos.Y }, TargetingData.ForwardDefensePoint, null, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
                else if (Vector2.DistanceSquared(new Vector2(ScoutLocations[ScoutLocationIndex].X, ScoutLocations[ScoutLocationIndex].Y), commander.UnitCalculation.Position) < 2)
                {
                    ScoutLocationIndex++;
                    if (ScoutLocationIndex >= ScoutLocations.Count())
                    {
                        ScoutLocationIndex = 0;
                    }
                }
                else
                {
                    var action = IndividualMicroController.Scout(commander, ScoutLocations[ScoutLocationIndex], TargetingData.ForwardDefensePoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
            }

            return commands;
        }

        private void GetScoutLocations()
        {
            ScoutLocations = new List<Point2D>();
            foreach (var baseLocation in BaseData.BaseLocations.Skip(1).Take(4))
            {
                ScoutLocations.Add(baseLocation.MineralLineLocation);
            }
            ScoutLocationIndex = 0;
        }
    }
}
