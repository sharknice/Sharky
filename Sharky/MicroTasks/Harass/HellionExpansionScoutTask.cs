namespace Sharky.MicroTasks
{
    public class HellionExpansionScoutTask : HellionHarassTask
    {
        public HellionExpansionScoutTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority, IIndividualMicroController hellionMicroController, IIndividualMicroController reaperMicroController) : base(defaultSharkyBot, enabled, priority, hellionMicroController, reaperMicroController)
        {
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < DesiredHellions)
            {
                if (Started)
                {
                    if (!UnitCommanders.Any())
                    {
                        Console.WriteLine($"HellionExpansionScoutTask ended");
                        Disable();
                    }
                    return;
                }

                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_HELLION))
                    {
                        commander.Value.Claimed = true;
                        commander.Value.UnitRole = UnitRole.Harass;
                        UnitCommanders.Add(commander.Value);

                        if (UnitCommanders.Count() >= DesiredHellions)
                        {
                            Started = true;
                            return;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();
            if (!Started) 
            {
                foreach (var commander in UnitCommanders)
                {
                    var action = HellionMicroController.Retreat(commander, TargetingData.MainDefensePoint, null, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }

                return commands; 
            }

            if (AttackPoint == null)
            {
                AttackPoint = BaseData.EnemyBaseLocations.Skip(BaseData.EnemyBases.Count).FirstOrDefault().BehindMineralLineLocation;
            }

            commands.AddRange(OrderHellions(frame));

            return commands;
        }

        protected override void SwitchTargetLocation()
        {
            var currentTargetBase = BaseData.EnemyBaseLocations.FirstOrDefault(b => b.BehindMineralLineLocation.X == AttackPoint.X && b.BehindMineralLineLocation.Y == AttackPoint.Y);
            var index = BaseData.EnemyBaseLocations.IndexOf(currentTargetBase);

            if (index < BaseData.EnemyBases.Count || index > BaseData.EnemyBaseLocations.Count() - BaseData.SelfBases.Count())
            {
                AttackPoint = BaseData.EnemyBaseLocations.Skip(BaseData.EnemyBases.Count).FirstOrDefault().BehindMineralLineLocation;
            }
            else
            {
                AttackPoint = BaseData.EnemyBaseLocations.Skip(index + 1).FirstOrDefault().BehindMineralLineLocation;
            }
        }
    }
}
