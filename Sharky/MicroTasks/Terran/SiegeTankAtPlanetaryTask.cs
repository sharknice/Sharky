namespace Sharky.MicroTasks
{
    public class SiegeTankAtPlanetaryTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        MapDataService MapDataService;
        TargetingData TargetingData;

        IIndividualMicroController SiegeTankMicroController;
        IIndividualMicroController SiegedTankMicroController;

        /// <summary>
        /// the maximum number of planetaries to put tanks at
        /// </summary>
        public int MaxPlanetaries { get; set; }

        /// <summary>
        /// the number of tanks to defend
        /// </summary>
        public int DesiredCount { get; set; }

        public SiegeTankAtPlanetaryTask(DefaultSharkyBot defaultSharkyBot, IIndividualMicroController siegeTankMicroController, IIndividualMicroController siegedTankMicroController, bool enabled, float priority)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MapDataService = defaultSharkyBot.MapDataService;
            TargetingData = defaultSharkyBot.TargetingData;

            SiegeTankMicroController = siegeTankMicroController;
            SiegedTankMicroController = siegedTankMicroController;

            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;

            MaxPlanetaries = 3;
            DesiredCount = 3;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < DesiredCount)
            {
                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED))
                    {
                        commander.Value.Claimed = true;
                        commander.Value.UnitRole = UnitRole.Defend;
                        UnitCommanders.Add(commander.Value);

                        if (UnitCommanders.Count() == DesiredCount)
                        {
                            return;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();
            if (!UnitCommanders.Any()) { return commands; }

            var planetaries = ActiveUnitData.Commanders.Values.Where(u => u.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_PLANETARYFORTRESS).OrderBy(p => Vector2.DistanceSquared(TargetingData.EnemyMainBasePoint.ToVector2(), p.UnitCalculation.Position)).Take(MaxPlanetaries);
            int tanksPerPlanetary = 1;
            if (planetaries.Any())
            {
                tanksPerPlanetary = UnitCommanders.Count() / planetaries.Count();
            }

            var availableTanks = UnitCommanders.ToList();
            foreach (var planetary in planetaries)
            {
                var nearbyTanks = planetary.UnitCalculation.NearbyAllies.Where(a => UnitCommanders.Any(c => c.UnitCalculation.Unit.Tag == a.Unit.Tag));
                var tanks = nearbyTanks.OrderBy(t => Vector2.DistanceSquared(t.Position, planetary.UnitCalculation.Position)).Take(tanksPerPlanetary);
                if (nearbyTanks.Count() < tanksPerPlanetary)
                {
                    var commander = availableTanks.OrderBy(t => Vector2.DistanceSquared(t.UnitCalculation.Position, planetary.UnitCalculation.Position)).FirstOrDefault();
                    if (commander != null)
                    {
                        if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK)
                        {
                            var action = SiegeTankMicroController.Retreat(commander, planetary.UnitCalculation.Position.ToPoint2D(), null, frame);
                            if (action != null) { commands.AddRange(action); }
                        }
                        else
                        {
                            var action = SiegedTankMicroController.Retreat(commander, planetary.UnitCalculation.Position.ToPoint2D(), null, frame);
                            if (action != null) { commands.AddRange(action); }
                        }
                    }
                }
                foreach (var tank in tanks)
                {
                    var commander = availableTanks.FirstOrDefault(t => tank.Unit.Tag == t.UnitCalculation.Unit.Tag);
                    if (commander != null)
                    {
                        if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK)
                        {
                            var action = SiegeTankMicroController.Retreat(commander, planetary.UnitCalculation.Position.ToPoint2D(), null, frame);
                            if (action != null) { commands.AddRange(action); }
                        }
                        else
                        {
                            var action = SiegedTankMicroController.Retreat(commander, planetary.UnitCalculation.Position.ToPoint2D(), null, frame);
                            if (action != null) { commands.AddRange(action); }
                        }
                    }

                }
                availableTanks.RemoveAll(t => tanks.Any(tank => tank.Unit.Tag == t.UnitCalculation.Unit.Tag));
            }

            foreach (var tank in availableTanks)
            {
                if (tank.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK)
                {
                    var action = SiegeTankMicroController.Retreat(tank, TargetingData.ForwardDefensePoint, null, frame);
                    if (action != null) { commands.AddRange(action); }
                }
                else
                {
                    var action = SiegedTankMicroController.Retreat(tank, TargetingData.ForwardDefensePoint, null, frame);
                    if (action != null) { commands.AddRange(action); }
                }
            }

            return commands;
        }
    }
}
