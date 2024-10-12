using Sharky.Algorithm;

namespace Sharky.Managers
{
    public class UnitManager : SharkyManager
    {
        SharkyUnitData SharkyUnitData;
        SharkyOptions SharkyOptions;
        TargetPriorityService TargetPriorityService;
        CollisionCalculator CollisionCalculator;
        MapDataService MapDataService;
        DebugService DebugService;
        DamageService DamageService;
        UnitDataService UnitDataService;
        BaseData BaseData;
        EnemyData EnemyData;

        float NearbyDistance = 18;
        float AvoidRange = 1;

        ActiveUnitData ActiveUnitData;

        int TargetPriorityCalculationFrame;


        public UnitManager(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, BaseData baseData, EnemyData enemyData, SharkyOptions sharkyOptions, TargetPriorityService targetPriorityService, CollisionCalculator collisionCalculator, MapDataService mapDataService, DebugService debugService, DamageService damageService, UnitDataService unitDataService)
        {
            ActiveUnitData = activeUnitData;

            SharkyUnitData = sharkyUnitData;
            BaseData = baseData;
            SharkyOptions = sharkyOptions;
            TargetPriorityService = targetPriorityService;
            CollisionCalculator = collisionCalculator;
            MapDataService = mapDataService;
            DebugService = debugService;
            DamageService = damageService;
            UnitDataService = unitDataService;
            EnemyData = enemyData;


            TargetPriorityCalculationFrame = 0;
        }

        public override bool NeverSkip { get { return true; } }

        public override void OnEnd(ResponseObservation observation, Result result)
        {
            Console.WriteLine($"Enemy Deaths: {ActiveUnitData.EnemyDeaths}, {ActiveUnitData.EnemyResourcesLost} resources lost");
            Console.WriteLine($"Self Deaths: {ActiveUnitData.SelfDeaths}, {ActiveUnitData.SelfResourcesLost} resources lost");
            Console.WriteLine($"Neutral Deaths: {ActiveUnitData.NeutralDeaths}");

            base.OnEnd(observation, result);
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            ProcessObservation(observation);
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            ProcessObservation(observation);
            return null;
        }

        private void ProcessObservation(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            if (observation.Observation.RawData.Event != null && observation.Observation.RawData.Event.DeadUnits != null)
            {
                ActiveUnitData.DeadUnits = observation.Observation.RawData.Event.DeadUnits.ToList();
            }
            else
            {
                ActiveUnitData.DeadUnits = new List<ulong>();
            }

            foreach (var unit in ActiveUnitData.SelfUnits.Where(u => SharkyUnitData.UndeadTypes.Contains((UnitTypes)u.Value.Unit.UnitType))) // remove things like purification novas that don't have dead unit events
            {
                if (!observation.Observation.RawData.Units.Any(u => u.Tag == unit.Key))
                {
                    ActiveUnitData.DeadUnits.Add(unit.Key);
                    ActiveUnitData.SelfDeaths--;
                }
            }
            foreach (var unit in ActiveUnitData.EnemyUnits.Where(u => SharkyUnitData.UndeadTypes.Contains((UnitTypes)u.Value.Unit.UnitType))) // remove things like purification novas that don't have dead unit events
            {
                if (!observation.Observation.RawData.Units.Any(u => u.Tag == unit.Key))
                {
                    ActiveUnitData.DeadUnits.Add(unit.Key);
                    ActiveUnitData.EnemyDeaths--;
                }
            }
            foreach (var unit in ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.UndeadTypes.Contains((UnitTypes)u.Value.Unit.UnitType))) // remove things like purification novas that don't have dead unit events
            {
                if (!observation.Observation.RawData.Units.Any(u => u.Tag == unit.Key))
                {
                    ActiveUnitData.DeadUnits.Add(unit.Key);
                    ActiveUnitData.NeutralDeaths--;
                }
            }

            if (EnemyData.SelfRace == Race.Zerg)
            {
                foreach (var unit in ActiveUnitData.Commanders.Where(commander => commander.Value.UnitRole == UnitRole.Build && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_DRONE)) // remove drones that morphed to a building
                {
                    if (!observation.Observation.RawData.Units.Any(u => u.Tag == unit.Key))
                    {
                        ActiveUnitData.DeadUnits.Add(unit.Key);
                        ActiveUnitData.SelfDeaths--;
                    }
                }
            }
            else if (EnemyData.SelfRace == Race.Protoss)
            {
                foreach (var unit in ActiveUnitData.Commanders.Where(commander => (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_DARKTEMPLAR) && (commander.Value.UnitRole == UnitRole.Morph || commander.Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.MORPH_ARCHON || o.AbilityId == (uint)Abilities.MORPH_ARCHON2)))) // remove templars that morphed to archons
                {
                    if (!observation.Observation.RawData.Units.Any(u => u.Tag == unit.Key))
                    {
                        ActiveUnitData.DeadUnits.Add(unit.Key);
                        ActiveUnitData.SelfDeaths--;
                    }
                }
            }
            else if (EnemyData.SelfRace == Race.Terran)
            {
                foreach (var unit in ActiveUnitData.Commanders.Where(commander => commander.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && commander.Value.UnitCalculation.FrameLastSeen < frame - 100))
                {
                    if (!observation.Observation.RawData.Units.Any(u => u.Tag == unit.Key))
                    {
                        ActiveUnitData.DeadUnits.Add(unit.Key);
                        ActiveUnitData.SelfDeaths--;
                    }
                }
            }

            foreach (var tag in ActiveUnitData.DeadUnits)
            {
                if (ActiveUnitData.EnemyUnits.Remove(tag, out UnitCalculation removedEnemy))
                {
                    if (!removedEnemy.Unit.IsHallucination)
                    {
                        ActiveUnitData.EnemyDeaths++;
                        ActiveUnitData.EnemyResourcesLost += (int)removedEnemy.UnitTypeData.MineralCost + (int)removedEnemy.UnitTypeData.VespeneCost;
                    }
                }
                if (ActiveUnitData.SelfUnits.Remove(tag, out UnitCalculation removedAlly))
                {
                    if (!removedAlly.Unit.IsHallucination)
                    {
                        ActiveUnitData.SelfDeaths++;
                        ActiveUnitData.SelfResourcesLost += (int)removedAlly.UnitTypeData.MineralCost + (int)removedAlly.UnitTypeData.VespeneCost;
                    }
                }
                else if (ActiveUnitData.NeutralUnits.Remove(tag, out UnitCalculation removedNeutral))
                {
                    ActiveUnitData.NeutralDeaths++;
                }

                ActiveUnitData.Commanders.Remove(tag, out UnitCommander removedCommander);
            }

            foreach (var unit in ActiveUnitData.NeutralUnits.Where(u => u.Value.Unit.DisplayType == DisplayType.Snapshot))
            {
                ActiveUnitData.NeutralUnits.Remove(unit.Key, out UnitCalculation removed);
            }

            var repairers = observation.Observation.RawData.Units.Where(u => u.UnitType == (uint)UnitTypes.TERRAN_SCV || u.UnitType == (uint)UnitTypes.TERRAN_MULE);

            //Parallel.ForEach(observation.Observation.RawData.Units, (unit) =>
            //{
            //    if (unit.Alliance == Alliance.Enemy)
            //    {
            //        var repairingUnitCount = repairers.Where(u => u.Alliance == Alliance.Enemy && Vector2.DistanceSquared(new Vector2(u.Pos.X, u.Pos.Y), new Vector2(unit.Pos.X, unit.Pos.Y)) < (1.0 + u.Radius + unit.Radius) * (0.1 + u.Radius + unit.Radius)).Count();
            //        var attack = new UnitCalculation(unit, repairingUnitCount, SharkyUnitData, SharkyOptions, UnitDataService, frame);
            //        if (ActiveUnitData.EnemyUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
            //        {
            //            attack.SetPreviousUnit(existing, existing.FrameLastSeen);
            //        }
            //        ActiveUnitData.EnemyUnits[unit.Tag] = attack;
            //    }
            //    else if (unit.Alliance == Alliance.Self)
            //    {
            //        var attack = new UnitCalculation(unit, 0, SharkyUnitData, SharkyOptions, UnitDataService, frame);
            //        if (ActiveUnitData.SelfUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
            //        {
            //            attack.SetPreviousUnit(existing, existing.FrameLastSeen);
            //        }
            //        ActiveUnitData.SelfUnits[unit.Tag] = attack;
            //    }
            //    else if (unit.Alliance == Alliance.Neutral)
            //    {
            //        var attack = new UnitCalculation(unit, 0, SharkyUnitData, SharkyOptions, UnitDataService, frame);
            //        if (ActiveUnitData.NeutralUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
            //        {
            //            attack.SetPreviousUnit(existing, existing.FrameLastSeen);
            //        }
            //        ActiveUnitData.NeutralUnits[unit.Tag] = attack;
            //    }
            //});

            foreach (var unit in observation.Observation.RawData.Units)
            {
                if (unit.Alliance == Alliance.Enemy)
                {
                    var repairingUnits = repairers.Where(u => u.Tag != unit.Tag && u.Alliance == Alliance.Enemy && Vector2.DistanceSquared(new Vector2(u.Pos.X, u.Pos.Y), new Vector2(unit.Pos.X, unit.Pos.Y)) < (1.0 + u.Radius + unit.Radius) * (0.1 + u.Radius + unit.Radius));
                    var attack = new UnitCalculation(unit, repairingUnits.ToList(), SharkyUnitData, SharkyOptions, UnitDataService, MapDataService.IsOnCreep(unit.Pos), frame);
                    if (ActiveUnitData.EnemyUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.SetPreviousUnit(existing, existing.FrameLastSeen);
                    }
                    ActiveUnitData.EnemyUnits[unit.Tag] = attack;
                }
                else if (unit.Alliance == Alliance.Self)
                {
                    var repairingUnits = repairers.Where(u => u.Alliance == Alliance.Self && Vector2.DistanceSquared(new Vector2(u.Pos.X, u.Pos.Y), new Vector2(unit.Pos.X, unit.Pos.Y)) < (1.0 + u.Radius + unit.Radius) * (0.1 + u.Radius + unit.Radius));
                    var attack = new UnitCalculation(unit, repairingUnits.ToList(), SharkyUnitData, SharkyOptions, UnitDataService, MapDataService.IsOnCreep(unit.Pos), frame);
                    if (ActiveUnitData.SelfUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.SetPreviousUnit(existing, existing.FrameLastSeen);
                    }
                    ActiveUnitData.SelfUnits[unit.Tag] = attack;
                }
                else if (unit.Alliance == Alliance.Neutral)
                {
                    var attack = new UnitCalculation(unit, new List<Unit>(), SharkyUnitData, SharkyOptions, UnitDataService, MapDataService.IsOnCreep(unit.Pos), frame);
                    if (ActiveUnitData.NeutralUnits.TryGetValue(unit.Tag, out UnitCalculation existing))
                    {
                        attack.SetPreviousUnit(existing, existing.FrameLastSeen);
                    }
                    else if (frame > 0 && SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)attack.Unit.UnitType))
                    {
                        var existingMatch = ActiveUnitData.NeutralUnits.FirstOrDefault(m => m.Value.Unit.Pos.X == attack.Unit.Pos.X && m.Value.Unit.Pos.Y == attack.Unit.Pos.Y);
                        if (existingMatch.Value != null)
                        {
                            UnitCalculation foo;
                            if (ActiveUnitData.NeutralUnits.Remove(existingMatch.Key, out foo))
                            {
                                foreach (var baseLocation in BaseData.BaseLocations)
                                {
                                    if (baseLocation.MineralFields.RemoveAll(m => m.Pos.X == attack.Unit.Pos.X && m.Pos.Y == attack.Unit.Pos.Y) > 0)
                                    {
                                        baseLocation.MineralFields.Add(unit);
                                    }
                                }
                                foreach (var baseLocation in BaseData.EnemyBaseLocations)
                                {
                                    if (baseLocation.MineralFields.RemoveAll(m => m.Pos.X == attack.Unit.Pos.X && m.Pos.Y == attack.Unit.Pos.Y) > 0)
                                    {
                                        baseLocation.MineralFields.Add(unit);
                                    }
                                }
                            }
                        }
                    }
                    ActiveUnitData.NeutralUnits[unit.Tag] = attack;
                }
            }

            foreach (var unit in ActiveUnitData.EnemyUnits.Where(u => u.Value.FrameLastSeen != frame && u.Value.UnitTypeData.Attributes.Contains(SC2Attribute.Structure))) // structures get replaced by snapshots if we can't see them, so just remove them and let them get readded
            {
                ActiveUnitData.EnemyUnits.Remove(unit.Key, out UnitCalculation removed);
            }

            var beforeFiveMinutes = frame < SharkyOptions.FramesPerSecond * 60 * 5;
            foreach (var enemy in ActiveUnitData.EnemyUnits.Select(e => e.Value).ToList()) // if we can see this area of the map and the unit isn't there anymore remove it (we just remove it because visible units will get re-added below)
            {
                if (enemy.FrameLastSeen != frame && MapDataService.SelfVisible(enemy.Unit.Pos))
                {
                    if (SharkyUnitData.BurrowedUnits.Contains((UnitTypes)enemy.Unit.UnitType) && !MapDataService.InSelfDetection(enemy.Unit.Pos))
                    {
                        enemy.Unit.DisplayType = DisplayType.Hidden;
                        continue; // it's still there but it's burrowed so we can't see it
                    }
                    ActiveUnitData.EnemyUnits.Remove(enemy.Unit.Tag, out UnitCalculation removed);
                }
                else if (beforeFiveMinutes)
                {
                    if (enemy.Unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS || enemy.Unit.UnitType == (uint)UnitTypes.PROTOSS_ARCHON)
                    {
                        enemy.Unit.IsHallucination = true;
                    }
                }
            }

            foreach (var unit in ActiveUnitData.SelfUnits.Where(u => u.Value.FrameLastSeen != frame && u.Value.Unit.UnitType == (uint)UnitTypes.ZERG_DRONE)) // structures get replaced by snapshots if we can't see them, so just remove them and let them get readded
            {
                if (unit.Value.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId)))
                {
                    ActiveUnitData.SelfUnits.Remove(unit.Key, out UnitCalculation removed);
                }
            }

            foreach (var allyAttack in ActiveUnitData.SelfUnits)
            {
                ClearUnitCalculations(allyAttack);
            }
            foreach (var enemyAttack in ActiveUnitData.EnemyUnits)
            {
                ClearUnitCalculations(enemyAttack);
            }


            KDTree2<UnitCalculation> enemyUnits = new KDTree2<UnitCalculation>();
            foreach (var enemyAttack in ActiveUnitData.EnemyUnits.Values)
            {
                enemyUnits.Add(enemyAttack, enemyAttack.Position);
            }
            enemyUnits.Build();

            KDTree2<UnitCalculation> selfUnits = new KDTree2<UnitCalculation>();
            foreach (var allyAttack in ActiveUnitData.SelfUnits.Values)
            {
                selfUnits.Add(allyAttack, allyAttack.Position);
            }
            selfUnits.Build();


            foreach (var allyAttack in ActiveUnitData.SelfUnits)
            {
                if (allyAttack.Value.FrameLastSeen != frame) { continue; }

                enemyUnits.ForRange(allyAttack.Value.Position, NearbyDistance, (enemyAttack) =>
                {
                    var range = GetRange(allyAttack.Value, enemyAttack);
                    var distanceSquared = Vector2.DistanceSquared(allyAttack.Value.Position, enemyAttack.Position);
                    if (DamageService.CanDamage(allyAttack.Value, enemyAttack) && distanceSquared <= (range + allyAttack.Value.Unit.Radius + enemyAttack.Unit.Radius) * (range + allyAttack.Value.Unit.Radius + enemyAttack.Unit.Radius))
                    {
                        if (allyAttack.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED && distanceSquared < 4)
                        {
                            return;
                        }
                        if (!enemyAttack.Unit.BuffIds.Contains((uint)Buffs.NEURALPARASITE))
                        {
                            allyAttack.Value.EnemiesInRange.Add(enemyAttack);
                        }
                        enemyAttack.EnemiesInRangeOf.Add(allyAttack.Value);
                    }
                    if (DamageService.CanDamage(enemyAttack, allyAttack.Value))
                    {
                        range = GetRange(enemyAttack, allyAttack.Value);
                        if (distanceSquared <= (AvoidRange + range + allyAttack.Value.Unit.Radius + enemyAttack.Unit.Radius) * (AvoidRange + range + allyAttack.Value.Unit.Radius + enemyAttack.Unit.Radius))
                        {
                            if (enemyAttack.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED && distanceSquared < 4)
                            {
                                return;
                            }
                            allyAttack.Value.EnemiesInRangeOfAvoid.Add(enemyAttack);
                            if (distanceSquared <= (range + allyAttack.Value.Unit.Radius + enemyAttack.Unit.Radius) * (range + allyAttack.Value.Unit.Radius + enemyAttack.Unit.Radius))
                            {
                                enemyAttack.EnemiesInRange.Add(allyAttack.Value);
                                allyAttack.Value.EnemiesInRangeOf.Add(enemyAttack);
                            }
                        }
                    }

                    enemyAttack.NearbyEnemies.Add(allyAttack.Value);
                    if (!enemyAttack.Unit.BuffIds.Contains((uint)Buffs.NEURALPARASITE))
                    {
                        allyAttack.Value.NearbyEnemies.Add(enemyAttack);
                    }
                });

                selfUnits.ForRange(allyAttack.Value.Position, NearbyDistance, (u) =>
                {
                    if (allyAttack.Key != u.Unit.Tag)
                        allyAttack.Value.NearbyAllies.Add(u);
                });
                allyAttack.Value.Loaded = false;

                if (ActiveUnitData.Commanders.TryGetValue(allyAttack.Value.Unit.Tag, out var commander))
                {
                    commander.UnitCalculation = allyAttack.Value;
                }
                else
                {
                    commander = new UnitCommander(allyAttack.Value);
                    ActiveUnitData.Commanders[allyAttack.Value.Unit.Tag] = commander;
                }

                if (ActiveUnitData.Commanders.ContainsKey(allyAttack.Value.Unit.Tag))
                {
                    var parent = GetParentUnitCalculation(ActiveUnitData.Commanders[allyAttack.Value.Unit.Tag]);
                    ActiveUnitData.Commanders[allyAttack.Value.Unit.Tag].ParentUnitCalculation = parent;
                    if (parent != null && ActiveUnitData.Commanders.ContainsKey(parent.Unit.Tag))
                    {
                        ActiveUnitData.Commanders[parent.Unit.Tag].ChildUnitCalculation = ActiveUnitData.Commanders[allyAttack.Value.Unit.Tag].UnitCalculation;
                    }
                }

                allyAttack.Value.Attackers = GetTargetedAttacks(allyAttack.Value).ToList();
                allyAttack.Value.Targeters = GetTargeters(allyAttack.Value).ToList();
                allyAttack.Value.EnemiesThreateningDamage = GetEnemiesThreateningDamage(allyAttack.Value);

                if (allyAttack.Value.Unit.Passengers != null)
                {
                    var tags = allyAttack.Value.Unit.Passengers.Select(p => p.Tag);
                    foreach (var tag in tags)
                    {
                        if (ActiveUnitData.SelfUnits.ContainsKey(tag))
                        {
                            ActiveUnitData.SelfUnits[tag].Loaded = true;
                            ActiveUnitData.SelfUnits[tag].NearbyAllies = allyAttack.Value.NearbyAllies;
                            ActiveUnitData.SelfUnits[tag].NearbyEnemies = allyAttack.Value.NearbyEnemies;
                            ActiveUnitData.SelfUnits[tag].Position = allyAttack.Value.Position;
                            var selfAttack = ActiveUnitData.SelfUnits[tag];

                            foreach (var enemyAttack in allyAttack.Value.NearbyEnemies)
                            {
                                var range = GetRange(selfAttack, enemyAttack);
                                if (DamageService.CanDamage(selfAttack, enemyAttack) && Vector2.DistanceSquared(selfAttack.Position, enemyAttack.Position) <= (range + selfAttack.Unit.Radius + enemyAttack.Unit.Radius) * (range + selfAttack.Unit.Radius + enemyAttack.Unit.Radius))
                                {
                                    if (selfAttack.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED && Vector2.DistanceSquared(selfAttack.Position, enemyAttack.Position) < 4)
                                    {
                                        continue;
                                    }
                                    selfAttack.EnemiesInRange.Add(enemyAttack);
                                }
                                if (DamageService.CanDamage(enemyAttack, selfAttack))
                                {
                                    range = GetRange(enemyAttack, selfAttack);
                                    var distanceSquared = Vector2.DistanceSquared(selfAttack.Position, enemyAttack.Position);
                                    if (distanceSquared <= (AvoidRange + range + selfAttack.Unit.Radius + enemyAttack.Unit.Radius) * (AvoidRange + range + selfAttack.Unit.Radius + enemyAttack.Unit.Radius))
                                    {
                                        if (selfAttack.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED && distanceSquared < 4)
                                        {
                                            continue;
                                        }
                                        selfAttack.EnemiesInRangeOfAvoid.Add(enemyAttack);
                                        if (distanceSquared <= (range + selfAttack.Unit.Radius + enemyAttack.Unit.Radius) * (range + selfAttack.Unit.Radius + enemyAttack.Unit.Radius))
                                        {
                                            selfAttack.EnemiesInRangeOf.Add(enemyAttack);
                                        }
                                    }
                                }
                            }

                            ActiveUnitData.SelfUnits[tag].EnemiesThreateningDamage = GetEnemiesThreateningDamage(ActiveUnitData.SelfUnits[tag]);

                            if (ActiveUnitData.SelfUnits[tag].Unit.Shield < ActiveUnitData.SelfUnits[tag].Unit.ShieldMax)
                            {
                                var timeLoaded = frame - ActiveUnitData.SelfUnits[tag].FrameLastSeen;
                                var regenFrames = timeLoaded - (SharkyOptions.FramesPerSecond * 7); // 7 seconds to start regenerating shields
                                var shieldRegenerated = regenFrames / (SharkyOptions.FramesPerSecond / 2f); // 2 shield points regenerated per second
                                if (shieldRegenerated > ActiveUnitData.SelfUnits[tag].Unit.ShieldMax - ActiveUnitData.SelfUnits[tag].Unit.Shield)
                                {
                                    ActiveUnitData.SelfUnits[tag].Unit.Shield = ActiveUnitData.SelfUnits[tag].Unit.ShieldMax;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var enemyAttack in ActiveUnitData.EnemyUnits)
            {
                enemyUnits.ForRange(enemyAttack.Value.Position, NearbyDistance, (u) =>
                {
                    if (enemyAttack.Key != u.Unit.Tag)
                        enemyAttack.Value.NearbyAllies.Add(u);
                });
            }

            if (TargetPriorityCalculationFrame + 10 < frame)
            {
                foreach (var selfUnit in ActiveUnitData.SelfUnits)
                {
                    if (selfUnit.Value.TargetPriorityCalculation == null || selfUnit.Value.TargetPriorityCalculation.FrameCalculated + 10 < frame)
                    {
                        var priorityCalculation = TargetPriorityService.CalculateTargetPriority(selfUnit.Value, frame);
                        selfUnit.Value.TargetPriorityCalculation = priorityCalculation;
                        foreach (var nearbyUnit in selfUnit.Value.NearbyAllies.Where(a => a.NearbyEnemies.Count == selfUnit.Value.NearbyAllies.Count))
                        {
                            nearbyUnit.TargetPriorityCalculation = priorityCalculation;
                        }
                    }

                    //selfUnit.Value.Attackers = GetTargettedAttacks(selfUnit.Value).ToList();
                    //selfUnit.Value.EnemiesThreateningDamage = GetEnemiesThreateningDamage(selfUnit.Value);
                }
                TargetPriorityCalculationFrame = frame;
            }

            if (SharkyOptions.Debug)
            {
                foreach (var selfUnit in ActiveUnitData.SelfUnits)
                {
                    DebugService.DrawLine(selfUnit.Value.Unit.Pos, new Point { X = selfUnit.Value.End.X, Y = selfUnit.Value.End.Y, Z = selfUnit.Value.Unit.Pos.Z + 1f }, new SC2APIProtocol.Color { R = 0, B = 0, G = 255 });
                }
                foreach (var enemyUnit in ActiveUnitData.EnemyUnits)
                {
                    DebugService.DrawLine(enemyUnit.Value.Unit.Pos, new Point { X = enemyUnit.Value.End.X, Y = enemyUnit.Value.End.Y, Z = enemyUnit.Value.Unit.Pos.Z + 1f }, new SC2APIProtocol.Color { R = 255, B = 0, G = 0 });
                }
            }
        }

        void ClearUnitCalculations(KeyValuePair<ulong, UnitCalculation> attack)
        {
            attack.Value.NearbyAllies.Clear();
            attack.Value.NearbyEnemies.Clear();
            attack.Value.EnemiesInRange.Clear();
            attack.Value.EnemiesInRangeOf.Clear();
            attack.Value.EnemiesInRangeOfAvoid.Clear();
            attack.Value.EnemiesThreateningDamage.Clear();
            attack.Value.Attackers.Clear();
            attack.Value.Targeters.Clear();
        }

        float GetRange(UnitCalculation allyAttack, UnitCalculation enemyAttack)
        {
            var range = allyAttack.Range;

            if (allyAttack.Weapons.Any())
            {
                var weapons = allyAttack.Weapons;
                var unit = enemyAttack.Unit;
                Weapon weapon;
                if (unit.IsFlying || unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS || unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM))
                {
                    weapon = weapons.FirstOrDefault(w => w.Type == Weapon.Types.TargetType.Air || w.Type == Weapon.Types.TargetType.Any);
                }
                else
                {
                    weapon = weapons.FirstOrDefault(w => w.Type == Weapon.Types.TargetType.Ground || w.Type == Weapon.Types.TargetType.Any);
                }
                if (weapon != null)
                {
                    return weapon.Range;
                }
            }

            return range;
        }

        float GetRange(KeyValuePair<ulong, UnitCalculation> allyAttack, KeyValuePair<ulong, UnitCalculation> enemyAttack)
        {
            return GetRange(allyAttack.Value, enemyAttack.Value);
        }

        List<UnitCalculation> GetTargetedAttacks(UnitCalculation unitCalculation)
        {
            var attacks = new List<UnitCalculation>();

            foreach (var enemyAttack in unitCalculation.EnemiesInRangeOfAvoid)
            {
                if (DamageService.CanDamage(enemyAttack, unitCalculation)
                    && CollisionCalculator.Collides(unitCalculation.Position, unitCalculation.Unit.Radius, enemyAttack.Start, enemyAttack.End))
                {
                    attacks.Add(enemyAttack);
                }
            };

            return attacks;
        }

        List<UnitCalculation> GetTargeters(UnitCalculation unitCalculation)
        {
            var attacks = new List<UnitCalculation>();

            foreach (var enemyAttack in unitCalculation.NearbyEnemies)
            {
                if (DamageService.CanDamage(enemyAttack, unitCalculation)
                    && CollisionCalculator.Collides(unitCalculation.Position, unitCalculation.Unit.Radius, enemyAttack.Start, enemyAttack.EndPlusFive))
                {
                    attacks.Add(enemyAttack);
                }
            };

            return attacks;
        }

        List<UnitCalculation> GetEnemiesThreateningDamage(UnitCalculation unitCalculation)
        {
            var attacks = new List<UnitCalculation>();

            foreach (var enemyAttack in unitCalculation.NearbyEnemies)
            {
                if (DamageService.CanDamage(enemyAttack, unitCalculation))
                {
                    // TODO: add any enemy in enemiesinrangeofavoid, do not need to calculate them for this

                    var fireTime = 0.25f; // TODO: use real weapon fire times
                    var weapon = unitCalculation.UnitTypeData.Weapons.FirstOrDefault();
                    if (weapon != null && weapon.HasSpeed)
                    {
                        fireTime = weapon.Speed / 10f; // TODO: need to get the actual fire times for weapons
                    }
                    var distance = Vector2.Distance(unitCalculation.Position, enemyAttack.Position);
                    if (enemyAttack.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED && distance < 2)
                    {
                        continue;
                    }
                    var avoidDistance = AvoidRange + enemyAttack.Range + unitCalculation.Unit.Radius + enemyAttack.Unit.Radius;
                    var distanceToInRange = distance - avoidDistance;
                    var timeToGetInRange = distanceToInRange / unitCalculation.UnitTypeData.MovementSpeed; // TODO: factor in speed buffs like creep
                    if (timeToGetInRange < fireTime || (enemyAttack.Unit.UnitType == (uint)UnitTypes.TERRAN_BATTLECRUISER && Vector2.DistanceSquared(enemyAttack.Position, unitCalculation.Position) < 100)) // in yamato range
                    {
                        attacks.Add(enemyAttack);
                    }
                }
            }

            return attacks;
        }

        UnitCalculation GetParentUnitCalculation(UnitCommander commander)
        {
            if (commander.ParentUnitCalculation != null)
            {
                if (ActiveUnitData.Commanders.ContainsKey(commander.ParentUnitCalculation.Unit.Tag))
                {
                    return ActiveUnitData.Commanders[commander.ParentUnitCalculation.Unit.Tag].UnitCalculation;
                }
            }

            if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT)
            {
                var closestAdept = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT).OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                if (closestAdept != null)
                {
                    return closestAdept;
                }
            }
            if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED)
            {
                var closestDisruptor = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTOR).OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                if (closestDisruptor != null)
                {
                    return closestDisruptor;
                }
            }
            if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_INTERCEPTOR)
            {
                var closestCarrier = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_CARRIER).OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                if (closestCarrier != null)
                {
                    return closestCarrier;
                }
            }

            return null;
        }
    }
}
