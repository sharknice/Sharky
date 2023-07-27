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
        private IndividualMicroController IndividualMicroController;
        private MapData MapData;

        public SecondaryOverlordScoutingTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority, IndividualMicroController individualMicroController)
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            BaseData = defaultSharkyBot.BaseData;
            EnemyData = defaultSharkyBot.EnemyData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            MapData = defaultSharkyBot.MapData;
            IndividualMicroController = individualMicroController;

            Priority = priority;
            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace != Race.Zerg)
                return;

            // Remove morphed overlords
            foreach (var commander in UnitCommanders.Where(commander => commander.UnitCalculation.Unit.UnitType != (int)UnitTypes.ZERG_OVERLORD))
            {
                commander.UnitRole = UnitRole.None;
                commander.Claimed = false;
            }
            UnitCommanders.RemoveAll(commander => commander.UnitRole != UnitRole.Scout);

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

            HashSet<Point2D> scoutedPos = new();

            foreach (var commander in UnitCommanders)
            {
                var pos = GetPoint(scoutedPos, commander.UnitCalculation.Position.ToPoint2D());

                // Retreat on damage or seeing enemy attacker unit
                if ((commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax * 0.8f) || commander.UnitCalculation.EnemiesInRangeOf.Any())
                {
                    var action = IndividualMicroController.Retreat(commander, BaseData.MainBase.Location, null, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
                else
                {
                    if (pos != null)
                    {
                        var action = IndividualMicroController.Scout(commander, pos, TargetingData.NaturalBasePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                }
            }

            return commands;
        }

        private Point2D GetPoint(HashSet<Point2D> scoutedPos, Point2D unitPos)
        {
            var pos = BaseData.BaseLocations
                .Where(x => !BaseData.EnemyBases.Contains(x))
                .Select(x => x.Location)
                .Where(p => !scoutedPos.Contains(p))
                .OrderBy(p => MapData.Map[(int)p.X,(int)p.Y].LastFrameVisibility)
                .ThenBy(p => p.Distance(unitPos))
                .FirstOrDefault();

            if (pos != null)
                scoutedPos.Add(pos);

            return pos;
        }
    }
}
