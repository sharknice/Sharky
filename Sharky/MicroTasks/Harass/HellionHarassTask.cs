using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class HellionHarassTask : MicroTask
    {
        BaseData BaseData;
        TargetingData TargetingData;

        public IIndividualMicroController HellionMicroController { get; set; }

        bool started { get; set; }

        public int DesiredHellions { get; set; }

        Point2D AttackPoint { get; set; }

        public HellionHarassTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority, IIndividualMicroController hellionMicroController)
        {
            BaseData = defaultSharkyBot.BaseData;
            TargetingData = defaultSharkyBot.TargetingData;

            HellionMicroController = hellionMicroController;

            UnitCommanders = new List<UnitCommander>();

            DesiredHellions = 4;

            Enabled = enabled;
            Priority = priority;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < DesiredHellions)
            {
                if (started)
                {
                    if (!UnitCommanders.Any())
                    {
                        Disable();
                    }
                    return;
                }

                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_HELLION)
                    {
                        commander.Value.Claimed = true;
                        commander.Value.UnitRole = UnitRole.Harass;
                        UnitCommanders.Add(commander.Value);

                        if (UnitCommanders.Count() >= DesiredHellions)
                        {
                            started = true;
                            return;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();
            if (BaseData?.EnemyBaseLocations?.FirstOrDefault() == null || !started) { return commands; }

            if (AttackPoint == null) 
            {
                AttackPoint = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault().MineralLineLocation;
            }

            var mainVector = new Vector2(AttackPoint.X, AttackPoint.Y);

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)) || Vector2.DistanceSquared(commander.UnitCalculation.Position, mainVector) < 100)
                {
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, mainVector) < 36 && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))
                    {
                        AttackPoint = BaseData.EnemyBaseLocations.FirstOrDefault().MineralLineLocation;
                    }
                    var action = HellionMicroController.HarassWorkers(commander, AttackPoint, TargetingData.MainDefensePoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
                else
                {
                    var action = HellionMicroController.NavigateToPoint(commander, AttackPoint, TargetingData.MainDefensePoint, null, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
            }

            return commands;
        }
    }
}
