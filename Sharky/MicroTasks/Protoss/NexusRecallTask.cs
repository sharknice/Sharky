namespace Sharky.MicroTasks
{
    public class NexusRecallTask : MicroTask
    {
        public Point2D NexusLocation { get; set; }

        ActiveUnitData ActiveUnitData;
        SharkyOptions SharkyOptions;
        SharkyUnitData SharkyUnitData;

        public NexusRecallTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (NexusLocation != null && UnitCommanders.Count() == 0)
            {
                var nexus = commanders.FirstOrDefault(c => c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && c.Value.UnitCalculation.Position.X == NexusLocation.X && c.Value.UnitCalculation.Position.Y == NexusLocation.Y).Value;
                if (nexus != null)
                {
                    nexus.UnitRole = UnitRole.Defend;
                    UnitCommanders.Add(nexus);
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            foreach (var nexus in UnitCommanders)
            {
                if (nexus.UnitCalculation.Unit.Energy >= 50 && nexus.AbilityOffCooldown(Abilities.NEXUSMASSRECALL, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
                {
                    var tempestInTrouble = ActiveUnitData.SelfUnits.FirstOrDefault(c => c.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_TEMPEST && c.Value.TargetPriorityCalculation != null && c.Value.TargetPriorityCalculation.AirWinnability < 1 && c.Value.EnemiesThreateningDamage.Count(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_VIKINGFIGHTER || e.Unit.UnitType == (uint)UnitTypes.ZERG_CORRUPTOR) > 3 && Vector2.DistanceSquared(c.Value.Position, new Vector2(NexusLocation.X, NexusLocation.Y)) > 225 && !c.Value.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_MOTHERSHIP)).Value;
                    if (tempestInTrouble == null)
                    {
                        tempestInTrouble = ActiveUnitData.SelfUnits.FirstOrDefault(c => c.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_TEMPEST && c.Value.TargetPriorityCalculation != null && c.Value.Unit.Shield < 5 && c.Value.NearbyEnemies.Count(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_VIKINGFIGHTER || e.Unit.UnitType == (uint)UnitTypes.ZERG_CORRUPTOR) > 1 && Vector2.DistanceSquared(c.Value.Position, new Vector2(NexusLocation.X, NexusLocation.Y)) > 225 && !c.Value.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_MOTHERSHIP) && c.Value.NearbyAllies.Count(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_VOIDRAY) < 5).Value;
                    }
                    if (tempestInTrouble != null)
                    {
                        var action = nexus.Order(frame, Abilities.NEXUSMASSRECALL, new Point2D { X = tempestInTrouble.Position.X, Y = tempestInTrouble.Position.Y });
                        if (action != null)
                        {
                            commands.AddRange(action);
                            return commands;
                        }
                    }
                }
            }

            return commands;
        }
    }
}
