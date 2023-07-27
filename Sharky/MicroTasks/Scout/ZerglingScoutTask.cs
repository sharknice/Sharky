namespace Sharky.MicroTasks
{
    public class ZerglingScoutTask : MicroTask
    {
        TargetingData TargetingData;
        EnemyData EnemyData;
        MicroTaskData MicroTaskData;
        MicroData MicroData;
        SharkyOptions SharkyOptions;

        int framesSinceLastMainScout = 0;
        bool scoutMain = false;

        public ZerglingScoutTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            EnemyData = defaultSharkyBot.EnemyData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            MicroData = defaultSharkyBot.MicroData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;

            CommanderDebugColor = new SC2APIProtocol.Color() { R = 255, G = 255, B = 127 };
            CommanderDebugText = "Scouting";
        }

        private void Claim(Dictionary<ulong, UnitCommander> commanders, bool allowSteal)
        {
            if (EnemyData.SelfRace != Race.Zerg)
                return;

            foreach (var commander in commanders)
            {
                if ((!commander.Value.Claimed || allowSteal) && commander.Value.UnitCalculation.Unit.UnitType == (int)UnitTypes.ZERG_ZERGLING && commander.Value.UnitRole != UnitRole.BlockExpansion)
                {
                    // Remove from other microtasks
                    if (commander.Value.Claimed)
                    {
                        foreach (var task in MicroTaskData.Values)
                        {
                            task.StealUnit(commander.Value);
                        }
                    }

                    commander.Value.Claimed = true;
                    commander.Value.UnitRole = UnitRole.Scout;
                    UnitCommanders.Add(commander.Value);

                    scoutMain = false;

                    if (UnitCommanders.Count >= 1)
                        return;
                }
            }
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() == 0)
            {
                Claim(commanders, false);

                if (UnitCommanders.Count() == 0)
                {
                    Claim(commanders, true);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            foreach (var commander in UnitCommanders)
            {
                if (framesSinceLastMainScout > SharkyOptions.FramesPerSecond * 40)
                {
                    scoutMain = true;
                    framesSinceLastMainScout = 0;
                }

                if (scoutMain)
                {
                    var action = commander.Order(frame, Abilities.MOVE, TargetingData.EnemyMainBasePoint);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
                else
                {
                    var scoutPos = TargetingData.NaturalFrontScoutPoint;

                    var action = MicroData.IndividualMicroControllers[UnitTypes.ZERG_ZERGLING].Scout(commander, scoutPos, TargetingData.MainDefensePoint, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
            }

            framesSinceLastMainScout += 1;

            return commands;
        }
    }
}
