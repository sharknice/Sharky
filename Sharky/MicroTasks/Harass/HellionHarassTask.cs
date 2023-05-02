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

        IIndividualMicroController HellionMicroController;

        bool started { get; set; }

        public int DesiredHellions { get; set; }

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
            if (BaseData?.EnemyBaseLocations?.FirstOrDefault() == null) { return commands; }

            var mainPoint = BaseData.EnemyBaseLocations.FirstOrDefault().MineralLineLocation;
            var mainVector = new Vector2(mainPoint.X, mainPoint.Y);

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitCalculation.EnemiesInRange.Any(e => !e.Attributes.Contains(Attribute.Structure)) || Vector2.DistanceSquared(commander.UnitCalculation.Position, mainVector) < 100)
                {
                    var action = HellionMicroController.HarassWorkers(commander, mainPoint, TargetingData.MainDefensePoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
                else
                {
                    var action = HellionMicroController.NavigateToPoint(commander, mainPoint, TargetingData.MainDefensePoint, null, frame);
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
