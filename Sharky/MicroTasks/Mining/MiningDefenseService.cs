namespace Sharky.MicroTasks.Mining
{
    public class MiningDefenseService
    {
        BaseData BaseData;
        ActiveUnitData ActiveUnitData;
        IIndividualMicroController WorkerMicroController;
        DebugService DebugService;
        DamageService DamageService;
        MapDataService MapDataService;
        TargetingData TargetingData;
        MineralWalker MineralWalker;
        EnemyData EnemyData;

        /// <summary>
        /// enemies workers will not deal with in this service, because they are handled in another MicroTask.
        /// </summary>
        public HashSet<UnitTypes> IgnoredThreatTypes = new HashSet<UnitTypes> { UnitTypes.TERRAN_REAPER };

        public bool Enabled { get; set; }

        public MiningDefenseService(DefaultSharkyBot defaultSharkyBot, IIndividualMicroController workerMicroController)
        {
            BaseData = defaultSharkyBot.BaseData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            WorkerMicroController = workerMicroController;
            DebugService = defaultSharkyBot.DebugService;
            DamageService = defaultSharkyBot.DamageService;
            MapDataService = defaultSharkyBot.MapDataService;
            TargetingData = defaultSharkyBot.TargetingData;
            MineralWalker = defaultSharkyBot.MineralWalker;
            EnemyData = defaultSharkyBot.EnemyData;
            Enabled = true;
        }

        public List<SC2APIProtocol.Action> DealWithEnemies(int frame, List<UnitCommander> unitCommanders)
        {
            var actions = new List<SC2APIProtocol.Action>();
            if (!Enabled) { return actions; }

            bool workerRushActive = false;
            bool preventGasSteal = false;
            bool preventBuildingLanding = false;

            foreach (var selfBase in BaseData.SelfBases)
            {
                if (selfBase.ResourceCenter != null && ActiveUnitData.Commanders.ContainsKey(selfBase.ResourceCenter.Tag) && ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Any(e => !IgnoredThreatTypes.Contains((UnitTypes)e.Unit.UnitType)))
                {
                    var flyingBuildings = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && u.Unit.IsFlying);
                    if (flyingBuildings.Any())
                    {
                        var nearbyWorkers = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Where(a => a.UnitClassifications.Contains(UnitClassification.Worker));
                        var commanders = ActiveUnitData.Commanders.Where(c => nearbyWorkers.Any(w => w.Unit.Tag == c.Key));
                        preventBuildingLanding = true;
                        foreach (var flyingBuilding in flyingBuildings)
                        {
                            var closestDefender = commanders.OrderBy(d => Vector2.DistanceSquared(d.Value.UnitCalculation.Position, flyingBuilding.Position)).FirstOrDefault();
                            if (closestDefender.Value != null)
                            {
                                closestDefender.Value.UnitRole = UnitRole.PreventBuildingLand;
                                var action = closestDefender.Value.Order(frame, Abilities.MOVE, new SC2APIProtocol.Point2D { X = flyingBuilding.Position.X, Y = flyingBuilding.Position.Y });
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                        }
                    }

                    if (!ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) && (ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker) || e.Attributes.Contains(SC2APIProtocol.Attribute.Structure))))
                    {
                        var enemyGroundDamage = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Take(25).Where(e => e.DamageGround).Sum(e => e.Damage);
                        var nearbyWorkers = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Take(25).Where(a => a.UnitClassifications.Contains(UnitClassification.Worker));
                        var commanders = ActiveUnitData.Commanders.Where(c => nearbyWorkers.Any(w => w.Unit.Tag == c.Key));
                        if (!commanders.Any()) { continue; }

                        if (ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyAllies.Where(e => e.DamageGround || e.UnitClassifications.Contains(UnitClassification.Worker)).Sum(e => e.Damage) > enemyGroundDamage || BaseData.SelfBases.Count() == 1)
                        {
                            int desiredWorkers = 0;
                            var combatUnits = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Take(25).Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit));
                            var workers = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Take(25).Where(u => u.UnitClassifications.Contains(UnitClassification.Worker));

                            desiredWorkers = 1;

                            if (workers.Count(w => w.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE) > 0 && workers.Count(w => w.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE) < 4)
                            {      
                                var takenGases = selfBase.GasMiningInfo.Select(i => i.ResourceUnit);
                                var openGeysers = BaseData.BaseLocations.SelectMany(b => b.VespeneGeysers).Where(g => g.VespeneContents > 0 && !takenGases.Any(t => t.Pos.X == g.Pos.X && t.Pos.Y == g.Pos.Y));
                                foreach (var gas in openGeysers)
                                {
                                    foreach (var worker in workers)
                                    {
                                        if (Vector2.DistanceSquared(new Vector2(gas.Pos.X, gas.Pos.Y), worker.Position) < 64)
                                        {
                                            preventGasSteal = true; // If enemy probe looking at empty gas send closest worker to touch that gas and prevent a steal
                                            var closestDefender = commanders.OrderBy(d => Vector2.DistanceSquared(d.Value.UnitCalculation.Position, new Vector2(gas.Pos.X, gas.Pos.Y))).FirstOrDefault();
                                            if (Vector2.DistanceSquared(closestDefender.Value.UnitCalculation.Position, new Vector2(gas.Pos.X, gas.Pos.Y)) < 25)
                                            {
                                                closestDefender.Value.UnitRole = UnitRole.PreventGasSteal;
                                                var action = closestDefender.Value.Order(frame, Abilities.MOVE, new SC2APIProtocol.Point2D { X = gas.Pos.X, Y = gas.Pos.Y });
                                                if (action != null)
                                                {
                                                    actions.AddRange(action);
                                                }
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            if (workers.Count() > 1)
                            {
                                desiredWorkers = workers.Count() + 1;
                            }
                            if (workers.Count() > 8) // this is a worker rush
                            {
                                desiredWorkers = workers.Count() + 3;
                            }
                            if (desiredWorkers > commanders.Count())
                            {
                                desiredWorkers = commanders.Count();
                            }

                            if (ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).Count() > 8)
                            {
                                DebugService.DrawText("--------------Defending Worker Rush-------------");
                                workerRushActive = true;
                                while (commanders.Count(c => c.Value.UnitRole == UnitRole.Defend) < desiredWorkers)
                                {
                                    var commander = commanders.FirstOrDefault(c => c.Value.UnitRole != UnitRole.Defend && c.Value.UnitRole != UnitRole.PreventGasSteal).Value;
                                    if (commander != null)
                                    {
                                        commander.UnitRole = UnitRole.Defend;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                var defenders = commanders.Where(c => c.Value.UnitRole == UnitRole.Defend);
                                foreach (var defender in defenders)
                                {
                                    var action = WorkerMicroController.Attack(defender.Value, selfBase.Location, selfBase.MineralLineLocation, null, frame);
                                    if (action !=  null)
                                    {
                                        actions.AddRange(action);
                                    }
                                }
                                DebugService.DrawText($"--------------{defenders.Count()} versus {workers.Count()}-------------");

                            }
                            else
                            {
                                var baseVector = new Vector2(selfBase.Location.X, selfBase.Location.Y);
                                var enemyBuildings = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => u.Attributes.Contains(SC2APIProtocol.Attribute.Structure)).OrderByDescending(u => u.Unit.BuildProgress).ThenBy(u => Vector2.DistanceSquared(u.Position, baseVector));
                                desiredWorkers = (enemyBuildings.Count() * 4) + workers.Count();

                                var enemy = ActiveUnitData.Commanders[selfBase.ResourceCenter.Tag].UnitCalculation.NearbyEnemies.Where(u => !u.Unit.IsFlying).OrderBy(u => Vector2.DistanceSquared(u.Position, new Vector2(selfBase.Location.X, selfBase.Location.Y))).FirstOrDefault();
                                if (enemyBuildings.Any() && !enemyBuildings.Any(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOTONCANNON && u.Unit.Shield == u.Unit.ShieldMax && u.Unit.BuildProgress == 1))
                                { // TODO: test this
                                    while (commanders.Count(c => c.Value.UnitRole == UnitRole.Defend) < desiredWorkers && commanders.Count(c => c.Value.UnitRole == UnitRole.Defend) < commanders.Count())
                                    {
                                        var commander = commanders.FirstOrDefault(c => c.Value.UnitRole != UnitRole.Defend && c.Value.UnitRole != UnitRole.PreventGasSteal).Value;
                                        if (commander != null)
                                        {
                                            commander.UnitRole = UnitRole.Defend;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    var defenders = commanders.Where(c => c.Value.UnitRole == UnitRole.Defend);
                                    var sentWorkers = new List<ulong>();
                                    foreach (var enemyBuilding in enemyBuildings)
                                    {
                                        var closestDefenders = defenders.Where(d => !sentWorkers.Contains(d.Key)).OrderBy(d => Vector2.DistanceSquared(d.Value.UnitCalculation.Position, enemyBuilding.Position)).Take(4); // attack each building with 4 workers
                                        foreach (var defender in closestDefenders)
                                        {
                                            var action = defender.Value.Order(frame, Abilities.ATTACK, null, enemyBuilding.Unit.Tag);
                                            if (action != null)
                                            {
                                                actions.AddRange(action);
                                            }
                                        }
                                        sentWorkers.AddRange(closestDefenders.Select(d => d.Key));
                                    }
                                    foreach (var enemyWorker in workers.OrderBy(u => Vector2.DistanceSquared(u.Position, baseVector)))
                                    {
                                        if (!defenders.Any(d => d.Value.LastTargetTag == enemyWorker.Unit.Tag))
                                        {
                                            var closestDefenders = defenders.Where(d => !sentWorkers.Contains(d.Key)).OrderBy(d => Vector2.DistanceSquared(d.Value.UnitCalculation.Position, enemyWorker.Position)).Take(1);
                                            foreach (var defender in closestDefenders)
                                            {
                                                var action = defender.Value.Order(frame, Abilities.ATTACK, null, enemyWorker.Unit.Tag);
                                                if (action != null)
                                                {
                                                    actions.AddRange(action);
                                                }
                                            }
                                            sentWorkers.AddRange(closestDefenders.Select(d => d.Key));
                                        }
                                    }
                                }
                                else if (commanders.Count() < desiredWorkers)
                                {
                                    actions.AddRange(Run(frame, unitCommanders, selfBase));
                                }
                                else if (enemy != null)
                                {
                                    while (commanders.Count(c => c.Value.UnitRole == UnitRole.Defend) < desiredWorkers && commanders.Count(c => c.Value.UnitRole == UnitRole.Defend) < commanders.Count())
                                    {
                                        var commander = commanders.FirstOrDefault(c => c.Value.UnitRole != UnitRole.Defend && c.Value.UnitRole != UnitRole.PreventGasSteal).Value;
                                        if (commander != null)
                                        {
                                            commander.UnitRole = UnitRole.Defend;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }

                                    var defenders = commanders.Where(c => c.Value.UnitRole == UnitRole.Defend);
                                    foreach (var defender in defenders)
                                    {
                                        var action = WorkerMicroController.Attack(defender.Value, selfBase.Location, selfBase.MineralLineLocation, null, frame);
                                        if (action != null)
                                        {
                                            actions.AddRange(action);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var action = Run(frame, unitCommanders, selfBase);
                            if (action != null)
                            {
                                actions.AddRange(action);
                            }
                        }
                    }
                    else
                    {
                        var action = Run(frame, unitCommanders, selfBase);
                        if (action != null)
                        {
                            actions.AddRange(action);
                        }
                    }
                }
            }


            var safeWorkers = ActiveUnitData.Commanders.Where(c => c.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && (c.Value.UnitRole == UnitRole.Defend || c.Value.UnitRole == UnitRole.Bait) && (c.Value.UnitCalculation.Unit.Orders.Count(o => o.AbilityId != (uint)Abilities.MOVE) == 0));
            foreach (var safeWorker in safeWorkers)
            {
                if (safeWorker.Value.UnitRole == UnitRole.Bait && safeWorker.Value.UnitCalculation.NearbyEnemies.Count(e => e.FrameLastSeen == frame) > 0)
                {
                    continue;
                }
                safeWorker.Value.UnitRole = UnitRole.None;
            }

            // only defend near the base, don't chase too far
            foreach (var commander in unitCommanders.Where(u => u.UnitRole == UnitRole.Defend || u.UnitRole == UnitRole.None))
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.ATTACK_ATTACK) && (!commander.UnitCalculation.NearbyAllies.Take(25).Any(a => a.UnitClassifications.Contains(UnitClassification.ResourceCenter)) || commander.UnitCalculation.NearbyAllies.Take(25).Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit))))
                {
                    var action = commander.Order(frame, Abilities.STOP);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }

            if (!workerRushActive)
            {
                foreach (var commander in unitCommanders.Where(u => u.UnitRole == UnitRole.Attack))
                {
                    if (commander.UnitCalculation.NearbyEnemies.Count(e => e.FrameLastSeen == frame) > 0)
                    {
                        continue;
                    }
                    commander.UnitRole = UnitRole.None;
                }
            }
            if (!preventGasSteal)
            {
                foreach (var commander in unitCommanders.Where(c => c.UnitRole == UnitRole.PreventGasSteal))
                {
                    commander.UnitRole = UnitRole.None;
                }
            }
            if (!preventBuildingLanding)
            {
                foreach (var commander in unitCommanders.Where(c => c.UnitRole == UnitRole.PreventBuildingLand))
                {
                    commander.UnitRole = UnitRole.None;
                }
            }

            return actions;
        }

        private List<SC2APIProtocol.Action> Run(int frame, List<UnitCommander> unitCommanders, BaseLocation selfBase)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var otherBase = BaseData.BaseLocations.FirstOrDefault(b => b.Location.X != selfBase.Location.X && b.Location.Y != selfBase.Location.Y);
            if (otherBase != null)
            {
                var wallData = MapDataService.MapData?.WallData?.FirstOrDefault(b => b.BasePosition.X == TargetingData.NaturalBasePoint.X && b.BasePosition.Y == TargetingData.NaturalBasePoint.Y);
                foreach (var commander in unitCommanders)
                {
                    if (EnemyData.SelfRace == Race.Protoss && wallData != null && commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame && !e.Unit.IsFlying) && Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(wallData.Door.X, wallData.Door.Y)) < 9)
                    {
                        if (commander.UnitRole != UnitRole.Build && MineralWalker.MineralWalkHome(commander, frame, out List<SC2APIProtocol.Action> action))
                        {
                            actions.AddRange(action);
                            continue;
                        }
                    }
                    else if (commander.UnitCalculation.EnemiesThreateningDamage.Any() && (commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax || commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax))
                    {
                        var openTransport = commander.UnitCalculation.NearbyAllies.FirstOrDefault(a => a.Unit.HasCargoSpaceMax && a.Unit.CargoSpaceTaken < a.Unit.CargoSpaceMax && Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position) < 25);
                        if (openTransport != null)
                        {
                            var saveAction = commander.Order(frame, Abilities.SMART, targetTag: openTransport.Unit.Tag);
                            commander.UnitRole = UnitRole.Defend;
                            if (saveAction != null)
                            {
                                actions.AddRange(saveAction);
                            }
                            continue;
                        }

                        if (commander.UnitCalculation.Unit.Health + commander.UnitCalculation.Unit.Shield <= (commander.UnitCalculation.EnemiesThreateningDamage.First().Damage * 2) || commander.UnitCalculation.UnitTypeData.MovementSpeed < commander.UnitCalculation.EnemiesThreateningDamage.First().UnitTypeData.MovementSpeed)
                        {
                            var enemy = commander.UnitCalculation.EnemiesThreateningDamage.FirstOrDefault();
                            if (enemy != null)
                            {
                                var closestFriendlyArmy = commander.UnitCalculation.NearbyAllies.Take(25).Where(u => u.UnitClassifications.Any(c => c == UnitClassification.ArmyUnit || c == UnitClassification.DefensiveStructure) && DamageService.CanDamage(u, enemy)).OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                                if (closestFriendlyArmy != null)
                                {
                                    var saveAction = commander.Order(frame, Abilities.MOVE, targetTag: closestFriendlyArmy.Unit.Tag);
                                    commander.UnitRole = UnitRole.Defend;
                                    if (saveAction != null)
                                    {
                                        actions.AddRange(saveAction);
                                    }
                                    continue;
                                }
                            }
                            if (commander.UnitRole == UnitRole.Minerals || commander.UnitRole == UnitRole.Gas)
                            {
                                commander.UnitRole = UnitRole.Defend;
                            }
                            var action = WorkerMicroController.Retreat(commander, otherBase.MineralLineLocation, null, frame);
                            if (action != null)
                            {
                                actions.AddRange(action);
                            }
                        }
                        else
                        {
                            if (commander.UnitRole == UnitRole.Minerals || commander.UnitRole == UnitRole.Gas)
                            {
                                commander.UnitRole = UnitRole.Defend;
                            }
                            var action = WorkerMicroController.Bait(commander, BaseData.BaseLocations.Last().Location, BaseData.BaseLocations.First().Location, null, frame);
                            if (action != null)
                            {
                                actions.AddRange(action);
                            }
                        }
                    }
                    else if (commander.UnitCalculation.EnemiesThreateningDamage.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_ORACLE && e.Unit.BuffIds.Contains((uint)Buffs.ORACLEWEAPON)))
                    {
                        if (commander.UnitRole == UnitRole.Minerals || commander.UnitRole == UnitRole.Gas)
                        {
                            commander.UnitRole = UnitRole.Defend;
                        }
                        var action = WorkerMicroController.Retreat(commander, otherBase.MineralLineLocation, null, frame);
                        if (action != null)
                        {
                            actions.AddRange(action);
                        }
                    }
                    else if (commander.UnitRole != UnitRole.Build && commander.UnitCalculation.Unit.WeaponCooldown == 0 && commander.UnitCalculation.EnemiesInRange.Any())
                    {
                        var action = commander.Order(frame, Abilities.ATTACK, null, commander.UnitCalculation.EnemiesInRange.OrderBy(e => e.Unit.Health + e.Unit.Shield).FirstOrDefault().Unit.Tag);
                        if (action != null)
                        {
                            actions.AddRange(action);
                        }
                    }
                }
            }

            return actions;
        }
    }
}
