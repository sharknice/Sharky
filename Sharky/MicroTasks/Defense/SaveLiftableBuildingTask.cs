namespace Sharky.MicroTasks
{
    public class SaveLiftableBuildingTask : MicroTask
    {
        EnemyData EnemyData;
        BaseData BaseData;
        UnitCountService UnitCountService;
        MapData MapData;

        IBuildingPlacement BuildingPlacement;

        public SaveLiftableBuildingTask(DefaultSharkyBot defaultSharkyBot, IBuildingPlacement buildingPlacement, float priority, bool enabled = true)
        {
            EnemyData = defaultSharkyBot.EnemyData;
            BaseData = defaultSharkyBot.BaseData;
            UnitCountService = defaultSharkyBot.UnitCountService;
            MapData = defaultSharkyBot.MapData;

            BuildingPlacement = buildingPlacement;

            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (EnemyData.SelfRace == SC2APIProtocol.Race.Terran)
            {
                foreach (var building in commanders.Where(u => !u.Value.Claimed && u.Value.UnitCalculation.Unit.BuildProgress == 1 && u.Value.UnitCalculation.Unit.Health < u.Value.UnitCalculation.Unit.HealthMax/2 
                    && (u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_COMMANDCENTER || u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_ORBITALCOMMAND || u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS || u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY || u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT)))
                {
                    if (building.Value.UnitCalculation.Unit.Health < building.Value.UnitCalculation.PreviousUnit.Health)
                    {
                        building.Value.Claimed = true;
                        building.Value.UnitRole = UnitRole.Repair;
                        UnitCommanders.Add(building.Value);
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (EnemyData.SelfRace == SC2APIProtocol.Race.Terran)
            {
                RemoveFinishedCommanders(frame);

                foreach (var commander in UnitCommanders)
                {
                    if (commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax / 2)
                    {
                        if (!commander.UnitCalculation.Unit.IsFlying)
                        {
                            if (commander.UnitCalculation.NearbyEnemies.Count() == 0 && commander.UnitCalculation.Unit.Health >= commander.UnitCalculation.PreviousUnit.Health)
                            {
                                continue;
                            }

                            if (IsWall(commander))
                            {
                                continue;
                            }

                            var action = commander.Order(frame, Abilities.CANCEL_LAST);
                            if (action != null) { actions.AddRange(action); }

                            action = commander.Order(frame, Abilities.LIFT, queue: true);
                            if (action != null) { actions.AddRange(action); }
                        }
                        else
                        {
                            if (UnitCountService.Count(UnitTypes.TERRAN_SCV) == 0 && commander.UnitCalculation.UnitClassifications.Contains(UnitClassification.ResourceCenter) && commander.UnitCalculation.EnemiesInRangeOfAvoid.Count() == 0)
                            {
                                if (!commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId > 0 && o.AbilityId != (uint)Abilities.MOVE))
                                {
                                    var location = BuildingPlacement.FindPlacement(new Point2D { X = commander.UnitCalculation.Position.X, Y = commander.UnitCalculation.Position.Y }, (UnitTypes)commander.UnitCalculation.Unit.UnitType, (int)(commander.UnitCalculation.Unit.Radius * 2));

                                    if (location != null)
                                    {
                                        var action = commander.Order(frame, Abilities.LAND, location);
                                        if (action != null) { actions.AddRange(action); }
                                    }
                                }
                            }
                            var safeBase = BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.ResourceCenter.Health == b.ResourceCenter.HealthMax).FirstOrDefault();
                            if (safeBase != null)
                            {
                                var action = commander.Order(frame, Abilities.MOVE, safeBase.Location);
                                if (action != null) { actions.AddRange(action); }
                            }
                        }
                    }
                    else
                    {
                        if (commander.UnitCalculation.Unit.IsFlying)
                        {
                            if (!commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId > 0 && o.AbilityId != (uint)Abilities.MOVE))
                            {
                                var location = BuildingPlacement.FindPlacement(new Point2D { X = commander.UnitCalculation.Position.X, Y = commander.UnitCalculation.Position.Y }, (UnitTypes)commander.UnitCalculation.Unit.UnitType, (int)(commander.UnitCalculation.Unit.Radius * 2));

                                if (location != null)
                                {
                                    var action = commander.Order(frame, Abilities.LAND, location);
                                    if (action != null) { actions.AddRange(action); }
                                }
                            }
                        }
                    }
                }
            }

            return actions;
        }

        private void RemoveFinishedCommanders(int frame)
        {
            var doneList = UnitCommanders.Where(c => c.UnitCalculation.Unit.Health > c.UnitCalculation.Unit.HealthMax / 2 && !c.UnitCalculation.Unit.IsFlying);
            foreach (var commander in doneList)
            {
                commander.UnitRole = UnitRole.None;
                commander.Claimed = false;
                UnitCommanders.Remove(commander);
                break;
            }
        }

        bool IsWall(UnitCommander commander)
        {
            if (MapData?.WallData != null)
            {
                foreach (var selfBase in BaseData.SelfBases)
                {
                    var wallData = MapData.WallData.FirstOrDefault(b => b.BasePosition.X == selfBase.Location.X && b.BasePosition.Y == selfBase.Location.Y);
                    if (wallData?.Production != null)
                    {
                        if (wallData.Production.Any(p => p.X == commander.UnitCalculation.Position.X && p.Y == commander.UnitCalculation.Position.Y))
                        {
                            return true;
                        }
                    }
                    if (wallData?.ProductionWithAddon != null)
                    {
                        if (wallData.ProductionWithAddon.Any(p => p.X == commander.UnitCalculation.Position.X && p.Y == commander.UnitCalculation.Position.Y))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
