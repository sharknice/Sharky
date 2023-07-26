namespace Sharky.MicroTasks
{
    public class RepairTask : MicroTask
    {
        ActiveUnitData ActiveUnitData;
        MacroData MacroData;
        SharkyUnitData SharkyUnitData;
        EnemyData EnemyData;
        TargetingData TargetingData;
        SharkyOptions SharkyOptions;

        IIndividualMicroController IndividiualMicroController;

        Dictionary<ulong, RepairData> RepairData;

        public RepairTask(DefaultSharkyBot defaultSharkyBot, float priority, bool enabled = true)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MacroData = defaultSharkyBot.MacroData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            EnemyData = defaultSharkyBot.EnemyData;
            TargetingData = defaultSharkyBot.TargetingData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            if (defaultSharkyBot.MicroData.IndividualMicroControllers.ContainsKey(UnitTypes.TERRAN_SCV))
            {
                IndividiualMicroController = defaultSharkyBot.MicroData.IndividualMicroControllers[UnitTypes.TERRAN_SCV];
            }
            else
            {
                IndividiualMicroController = defaultSharkyBot.MicroData.IndividualMicroController;
            }

            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
            RepairData = new Dictionary<ulong, RepairData>();
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (MacroData.Minerals > 10 && EnemyData.SelfRace == SC2APIProtocol.Race.Terran)
            {
                foreach (var repair in RepairData)
                {
                    var needed = repair.Value.DesiredRepairers - repair.Value.Repairers.Count();
                    if (needed > 0)
                    {
                        var closest = commanders.Where(commander => commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV && 
                            (commander.Value.UnitRole == UnitRole.Minerals || commander.Value.UnitRole == UnitRole.None || !commander.Value.Claimed) && commander.Value.UnitCalculation.Unit.BuffIds.Count() == 0)
                                .OrderBy(c => Vector2.DistanceSquared(c.Value.UnitCalculation.Position, repair.Value.UnitToRepair.Position)).Take(needed);
                        foreach (var scv in closest.Where(c => Vector2.DistanceSquared(c.Value.UnitCalculation.Position, repair.Value.UnitToRepair.Position) < 400)) // limit it to scvs within 20 range
                        {
                            scv.Value.Claimed = true;
                            scv.Value.UnitRole = UnitRole.Repair;
                            UnitCommanders.Add(scv.Value);
                            repair.Value.Repairers.Add(scv.Value);
                        }
                    }
                }
            }
        }


        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (EnemyData.SelfRace == SC2APIProtocol.Race.Terran)
            {
                UpdateRepairData(frame);

                foreach (var repair in RepairData)
                {
                    foreach (var scv in repair.Value.Repairers)
                    {
                        if (scv.UnitCalculation.Unit.Orders.Count() > 1)
                        {
                            var stop = scv.Order(frame, Abilities.STOP);
                            if (stop != null) { actions.AddRange(stop); }
                        }
                        else
                        {
                            if (ActiveUnitData.Commanders.ContainsKey(repair.Key))
                            {
                                var commander = ActiveUnitData.Commanders[repair.Key];
                                var action = IndividiualMicroController.Support(scv, new List<UnitCommander> { commander }, new SC2APIProtocol.Point2D { X = repair.Value.UnitToRepair.Position.X, Y = repair.Value.UnitToRepair.Position.Y }, TargetingData.MainDefensePoint, null, frame);
                                if (action != null) { actions.AddRange(action); }
                            }
                        }
                    }
                }
            }

            return actions;
        }

        private void UpdateRepairData(int frame)
        {
            foreach (var building in ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.BuildProgress == 1 && u.Unit.Health < u.Unit.HealthMax && u.Attributes.Contains(SC2APIProtocol.Attribute.Structure)))
            {
                if (!RepairData.ContainsKey(building.Unit.Tag))
                {
                    RepairData[building.Unit.Tag] = new RepairData(building);
                }
                else
                {
                    RepairData[building.Unit.Tag].UnitToRepair = building;
                    RepairData[building.Unit.Tag].Repairers.RemoveAll(s => s.UnitCalculation.FrameLastSeen != frame);
                }
            }


            foreach (var unit in ActiveUnitData.SelfUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter)))
            {
                foreach (var armyUnit in unit.NearbyAllies.Where(u => u.Unit.BuildProgress == 1 && u.Unit.Health < u.Unit.HealthMax && u.UnitClassifications.Contains(UnitClassification.ArmyUnit) && u.Attributes.Contains(SC2APIProtocol.Attribute.Mechanical)))
                {
                    if (!RepairData.ContainsKey(armyUnit.Unit.Tag))
                    {
                        RepairData[armyUnit.Unit.Tag] = new RepairData(armyUnit);
                    }
                    else
                    {
                        RepairData[armyUnit.Unit.Tag].UnitToRepair = armyUnit;
                        RepairData[armyUnit.Unit.Tag].Repairers.RemoveAll(s => s.UnitCalculation.FrameLastSeen != frame);
                    }
                }
            }

            RemoveFinishedData(frame);

            CalculateRepair(frame);
        }


        private void RemoveFinishedData(int frame)
        {
            var doneList = RepairData.Where(d => d.Value.UnitToRepair.FrameLastSeen != frame).Select(d => d.Key);
            if (MacroData.Minerals == 0)
            {
                doneList = RepairData.Keys;
            }
            foreach (var key in doneList)
            {
                foreach (var scv in RepairData[key].Repairers)
                {
                    scv.Claimed = false;
                    scv.UnitRole = UnitRole.None;
                    UnitCommanders.RemoveAll(u => u.UnitCalculation.Unit.Tag == scv.UnitCalculation.Unit.Tag);
                }
                RepairData.Remove(key);
            }
        }

        private void CalculateRepair(int frame)
        {
            foreach (var repair in RepairData)
            {
                var dps = repair.Value.UnitToRepair.Attackers.Sum(a => a.Dps);
                var singleHps = (repair.Value.UnitToRepair.Unit.HealthMax / SharkyUnitData.UnitData[(UnitTypes)repair.Value.UnitToRepair.Unit.UnitType].BuildTime) * SharkyOptions.FramesPerSecond;
                repair.Value.DesiredRepairers = (int)Math.Ceiling(dps / singleHps);
                if (repair.Value.DesiredRepairers == 0)
                {
                    repair.Value.DesiredRepairers = 1;
                }
                if (repair.Value.UnitToRepair.UnitClassifications.Contains(UnitClassification.DefensiveStructure))
                {
                    repair.Value.DesiredRepairers++;
                }
                if (repair.Value.UnitToRepair.Unit.Health < repair.Value.UnitToRepair.Unit.HealthMax / 2)
                {
                    repair.Value.DesiredRepairers++;

                    if (repair.Value.UnitToRepair.Unit.UnitType == (uint)UnitTypes.TERRAN_PLANETARYFORTRESS)
                    {
                        if (repair.Value.DesiredRepairers < 12)
                        {
                            repair.Value.DesiredRepairers = 12;
                        }
                    }
                }
            }
        }
    }
}
