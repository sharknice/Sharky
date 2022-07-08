using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    /// <summary>
    /// Secondary overlords for keeping map control
    /// </summary>
    public class SecondaryOverlordScoutingTask : MicroTask
    {
        private SharkyUnitData SharkyUnitData;
        private TargetingData TargetingData;
        private BaseData BaseData;
        private SharkyOptions SharkyOptions;
        private EnemyData EnemyData;
        private IIndividualMicroController IndividualMicroController;

        private bool LateGame = false;

        int ScoutLocationIndex = 0;

        List<Point2D> ScoutLocations { get; set; }

        public SecondaryOverlordScoutingTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority, IIndividualMicroController individualMicroController)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            BaseData = defaultSharkyBot.BaseData;
            EnemyData = defaultSharkyBot.EnemyData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            IndividualMicroController = individualMicroController;

            Priority = priority;
            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace != Race.Zerg)
                return;

            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (int)UnitTypes.ZERG_OVERLORD)
                {
                    commander.Value.Claimed = true;
                    commander.Value.UnitRole = UnitRole.Scout;
                    UnitCommanders.Add(commander.Value);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (EnemyData.SelfRace != Race.Zerg)
                return commands;

            if (ScoutLocations == null)
            {
                GetScoutLocations();
            }

            if (!LateGame && frame > SharkyOptions.FramesPerSecond * 4 * 60)
            {
                LateGame = true;
                ScoutLocations = new List<Point2D>();
                ScoutLocationIndex = 0;

                foreach (var baseLocation in BaseData.BaseLocations.Where(b => !BaseData.SelfBases.Any(s => s.Location == b.Location) && !BaseData.EnemyBases.Any(s => s.Location == b.Location)))
                {
                    ScoutLocations.Add(baseLocation.MineralLineLocation);
                }
            }

            foreach (var commander in UnitCommanders)
            {
                // Retreat on damage or seeing enemy attacker unit
                if ((commander.UnitCalculation.Unit.Health != commander.UnitCalculation.Unit.HealthMax) || commander.UnitCalculation.EnemiesThreateningDamage.Any())
                {
                    commander.UnitRole = UnitRole.None;
                    var action = commander.Order(frame, Abilities.MOVE, BaseData.MainBase.Location);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
                else
                {
                    if (commander.UnitCalculation.Velocity == 0)
                    {
                        commander.UnitRole = UnitRole.Scout;

                        var action = IndividualMicroController.Scout(commander, ScoutLocations[ScoutLocationIndex % ScoutLocations.Count], TargetingData.NaturalBasePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        ScoutLocationIndex = (ScoutLocationIndex + 1) % ScoutLocations.Count;
                    }
                }
            }

            return commands;
        }

        private void GetScoutLocations()
        {
            ScoutLocations = new List<Point2D>();

            if (EnemyData.EnemyRace == Race.Zerg)
            {
                if (BaseData.EnemyBaseLocations.Count >= 3)
                    ScoutLocations.Add(BaseData.EnemyBaseLocations.Skip(2).First().Location);

                if (BaseData.EnemyBaseLocations.Count >= 4)
                    ScoutLocations.Add(BaseData.EnemyBaseLocations.Skip(3).First().Location);
            }
            else
            {
                ScoutLocations.Add(TargetingData.ForwardDefensePoint);
                ScoutLocations.Add(TargetingData.MainDefensePoint);
            }

            foreach (var baseLocation in BaseData.BaseLocations.Skip(1).Take(BaseData.BaseLocations.Count - 3))
            {
                ScoutLocations.Add(baseLocation.MineralLineLocation);
            }

        }
    }
}
