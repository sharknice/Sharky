namespace Sharky.MicroTasks
{
    public class SquadPerBaseTask : MicroTask
    {
        BaseData BaseData;
        MicroData MicroData;
        AreaService AreaService;

        public List<DesiredUnitsClaim> DesiredUnitsClaims { get; set; }
        Dictionary<ulong, List<UnitCommander>> BaseSquads { get; set; }

        public SquadPerBaseTask(DefaultSharkyBot defaultSharkyBot, List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = false)
        {
            BaseData = defaultSharkyBot.BaseData;
            MicroData = defaultSharkyBot.MicroData;
            AreaService = defaultSharkyBot.AreaService;

            DesiredUnitsClaims = desiredUnitsClaims;

            Priority = priority;
            Enabled = enabled;

            UnitCommanders = new List<UnitCommander>();
            BaseSquads = new Dictionary<ulong, List<UnitCommander>>();
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    var unitType = commander.Value.UnitCalculation.Unit.UnitType;
                    foreach (var squad in BaseSquads)
                    {
                        if (commander.Value.Claimed)
                        {
                            break;
                        }
                        foreach (var desiredUnitClaim in DesiredUnitsClaims)
                        {
                            if ((uint)desiredUnitClaim.UnitType == unitType && !commander.Value.UnitCalculation.Unit.IsHallucination && NeedDesiredClaim(desiredUnitClaim, squad))
                            {
                                commander.Value.Claimed = true;
                                commander.Value.UnitRole = UnitRole.Defend;
                                squad.Value.Add(commander.Value);
                                UnitCommanders.Add(commander.Value);
                                break;
                            }
                        }
                    }
                }
            }
        }

        bool NeedDesiredClaim(DesiredUnitsClaim desiredUnitClaim, KeyValuePair<ulong, List<UnitCommander>> squad)
        {
            var count = squad.Value.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType);
            if (desiredUnitClaim.UnitType == UnitTypes.TERRAN_SIEGETANK)
            {
                count+= squad.Value.Count(u => u.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED);
            }
            return count < desiredUnitClaim.Count;
        }

        public override IEnumerable<SC2Action> PerformActions(int frame)
        {
            var actions = new List<SC2Action>();

            UpdateBases();

            foreach (var baseData in BaseData.SelfBases.Where(r => r.ResourceCenter != null))
            {
                var squad = BaseSquads[baseData.ResourceCenter.Tag];
                var area = AreaService.GetTargetArea(baseData.Location, 8);
                foreach (var commander in squad)
                {
                    List<SC2APIProtocol.Action> action;
                    if (Vector2.Distance(commander.UnitCalculation.Position, baseData.Location.ToVector2()) > 8)
                    {
                        if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                        {
                            action = individualMicroController.Retreat(commander, baseData.Location, baseData.Location, frame);
                        }
                        else
                        {
                            action = MicroData.IndividualMicroController.Retreat(commander, baseData.Location, baseData.Location, frame);
                        }
                    }
                    else
                    {
                        commander.UnitCalculation.TargetPriorityCalculation.Overwhelm = true;
                        if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                        {
                            action = individualMicroController.AttackWithinArea(commander, area, baseData.Location, baseData.Location, baseData.Location, frame);
                        }
                        else
                        {
                            action = MicroData.IndividualMicroController.AttackWithinArea(commander, area, baseData.Location, baseData.Location, baseData.Location, frame);
                        }
                    }
                    if (action != null) { actions.AddRange(action); }
                }
            }

            return actions;
        }

        private void UpdateBases()
        {
            if (BaseData.SelfBases.Count(b => b.ResourceCenter != null) != BaseSquads.Count)
            {
                var resourceCenters = BaseData.SelfBases.Where(b => b.ResourceCenter != null).Select(b => b.ResourceCenter.Tag).ToList();
                var toRemove = BaseSquads.Keys.Where(b => !resourceCenters.Contains(b)).ToList();
                foreach (var tag in toRemove)
                {
                    foreach (var commander in BaseSquads[tag])
                    {
                        UnitCommanders.Remove(commander);
                    }
                    BaseSquads.Remove(tag);
                }
                var toAdd = resourceCenters.Where(b => !BaseSquads.ContainsKey(b)).ToList();
                foreach (var tag in toAdd)
                {
                    BaseSquads[tag] = new List<UnitCommander>();
                }
            }
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                foreach (var squad in BaseSquads)
                {
                    squad.Value.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
                }
            }
        }
    }
}
