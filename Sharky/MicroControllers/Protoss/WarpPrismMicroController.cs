namespace Sharky.MicroControllers.Protoss
{
    public class WarpPrismMicroController : IndividualMicroController
    {
        protected int PickupRange = 5;

        MacroData MacroData;

        public WarpPrismMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 3;
            MacroData = defaultSharkyBot.MacroData;
        }

        public override List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var action = new List<SC2APIProtocol.Action>();

            if (SupportArmy(commander, target, defensivePoint, groupCenter, frame, out action))
            {
                return action;
            }
            return base.Attack(commander, target, defensivePoint, groupCenter, frame);
        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (SupportArmy(commander, target, defensivePoint, groupCenter, frame, out action))
            {
                return true;
            }

            return false;
        }

        public bool SupportArmy(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame, out List<SC2APIProtocol.Action> action, IEnumerable<UnitCalculation> supportableUnits = null)
        {
            action = null;

            UpdateLoadTimes(commander);

            if (commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax / 2)
            {
                if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }

                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }

                if (commander.UnitCalculation.Unit.Shield < 5)
                {
                    if (Retreat(commander, target, defensivePoint, frame, out action))
                    {
                        return true;
                    }
                }
            }

            // follow behind at the range of pickup
            var unitToSupport = GetSupportTarget(commander, target, defensivePoint, supportableUnits);
            if (unitToSupport == null)
            {
                if (UnloadUnits(commander, defensivePoint, frame, out action))
                {
                    return true;
                }

                return false;
            }

            if (!commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.Tag == unitToSupport.Unit.Tag))
            {
                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(target.X, target.Y)) > Vector2.DistanceSquared(unitToSupport.Position, new Vector2(target.X, target.Y)))
                {
                    if (NavigateToSupportUnit(commander, target, frame, out action))
                    {
                        return true;
                    }
                }
            }

            if (UnloadUnits(commander, defensivePoint, frame, out action))
            {
                return true;
            }

            var moveTo = GetPickupSpot(new Point2D { X = unitToSupport.Unit.Pos.X, Y = unitToSupport.Unit.Pos.Y }, defensivePoint);
            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.UNLOADALLAT_WARPPRISM || o.AbilityId == (uint)Abilities.UNLOADUNIT_WARPPRISM) || !MapDataService.PathWalkable(moveTo)) // TODO: does this groundpathable thing work right?
            {
                moveTo = new Point2D { X = unitToSupport.Unit.Pos.X, Y = unitToSupport.Unit.Pos.Y };
            }

            if (InRange(new Vector2(moveTo.X, moveTo.Y), commander.UnitCalculation.Position, 2) && InRange(unitToSupport.Position, commander.UnitCalculation.Position, PickupRange))
            {
                //look at all units within pickup range, ordered by proximity to their closest enemy
                // get average hp + shields of back
                // if unit is in front half weapon is off cooldown and (has below that hp + shields or could die in one hit) pick it up
                var friendliesInRange = commander.UnitCalculation.NearbyAllies.Take(25).Where(u => !u.Loaded && InRange(u.Position, commander.UnitCalculation.Position, PickupRange)).OrderBy(u => ClosestEnemyDistance(u));
                var frontHalf = friendliesInRange.Take(friendliesInRange.Count() / 2);
                var backHalf = friendliesInRange.Skip(friendliesInRange.Count() / 2);
                var backAverageHealth = backHalf.Sum(u => u.Unit.Health + u.Unit.Shield) / backHalf.Count();
                foreach (var friendly in frontHalf)
                {
                    if (commander.UnitCalculation.Unit.CargoSpaceMax - commander.UnitCalculation.Unit.CargoSpaceTaken >= UnitDataService.CargoSize((UnitTypes)friendly.Unit.UnitType))
                    {
                        if (ShouldLoadUnit(friendly, backAverageHealth, frame))
                        {
                            action = commander.Order(frame, Abilities.LOAD, null, friendly.Unit.Tag);
                            return true;
                        }
                    }
                }

                if (friendliesInRange.Count() < 4)
                {
                    foreach (var friendly in friendliesInRange)
                    {
                        if (commander.UnitCalculation.Unit.CargoSpaceMax - commander.UnitCalculation.Unit.CargoSpaceTaken >= UnitDataService.CargoSize((UnitTypes)friendly.Unit.UnitType))
                        {
                            if (ShouldLoadUnit(friendly, friendly.Unit.Health + (friendly.Unit.ShieldMax / 2), frame))
                            {
                                if (friendly.Unit.WeaponCooldown > 0 && friendly.Unit.Shield < friendly.Unit.ShieldMax / 2)
                                {
                                    action = commander.Order(frame, Abilities.LOAD, null, friendly.Unit.Tag);
                                    return true;
                                }
                            }
                        }
                    }
                }

                foreach (var friendly in friendliesInRange.Where(f => f.EnemiesInRangeOf.Any() && f.Range > 2).OrderBy(f => f.Unit.Shield).ThenBy(f => f.Unit.Health))
                {
                    if (commander.UnitCalculation.Unit.CargoSpaceMax - commander.UnitCalculation.Unit.CargoSpaceTaken >= UnitDataService.CargoSize((UnitTypes)friendly.Unit.UnitType))
                    {
                        if (friendly.Unit.WeaponCooldown > 0)
                        {
                            if (ActiveUnitData.Commanders.ContainsKey(friendly.Unit.Tag))
                            {
                                if (ActiveUnitData.Commanders[friendly.Unit.Tag].UnitRole != UnitRole.Door)
                                {
                                    action = commander.Order(frame, Abilities.LOAD, null, friendly.Unit.Tag);
                                    return true;
                                }
                            }
                        }
                    }
                }

                if (StartWarping(commander, frame, out action))
                {
                    return true;
                }

                if (AvoidDeceleration(commander, moveTo, false, frame, out action))
                {
                    return true;
                }
            }
            else
            {
                // move to pickup the friendly closest to the enemy
                if (StopWarping(commander, frame, out action))
                {
                    return true;
                }

                if (AvoidDeceleration(commander, moveTo, false, frame, out action))
                {
                    return true;
                }
                action = commander.Order(frame, Abilities.MOVE, moveTo);
                return true;
            }

            return false;
        }

        bool StartWarping(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (MacroData.FoodUsed >= 198) { return false; }

            if (commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax)
            {
                return false;
            }
            if (!MapDataService.PathWalkable(commander.UnitCalculation.Unit.Pos))
            {
                return false;
            }
            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.DefensiveStructure) || e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)))
            {
                return false;
            }
            if (ActiveUnitData.Commanders.Values.Where(v => v.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE && !v.UnitCalculation.Unit.IsActive && v.WarpInAlmostOffCooldown(frame, SharkyOptions.FramesPerSecond, SharkyUnitData)).Count() == 0)
            {
                return false;
            }
            if (!commander.UnitCalculation.NearbyAllies.Any(v => (v.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON || v.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING))) // not near any pylons or other warping prisms
            {
                if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM)
                {
                    action = commander.Order(frame, Abilities.MORPH_WARPPRISMPHASINGMODE, allowSpam: true);
                    return true;
                }
            }
            return false;
        }

        protected bool StopWarping(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING)
            {
                if (commander.UnitCalculation.Unit.Shield > 75 && commander.UnitCalculation.NearbyAllies.Any(v => v.Unit.BuildProgress < 1 && Vector2.DistanceSquared(v.Position, commander.UnitCalculation.Position) < 49 && v.FrameLastSeen == frame)) // and not warping any units in
                {
                    return false;
                }
                action = commander.Order(frame, Abilities.MORPH_WARPPRISMTRANSPORTMODE);
                return true;
            }
            return false;
        }

        bool ShouldLoadUnit(UnitCalculation friendly, float healthLimit, int frame)
        {
            if (ActiveUnitData.Commanders.ContainsKey(friendly.Unit.Tag) && ActiveUnitData.Commanders[friendly.Unit.Tag].UnitRole == UnitRole.Door)
            {
                return false;
            }

            if (friendly.Unit.UnitType == (uint)UnitTypes.PROTOSS_ZEALOT)
            {
                if (friendly.Unit.Shield > 0 || friendly.EnemiesInRange.Any())
                {
                    return false;
                }
            }

            if (friendly.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTOR)
            {
                if (ActiveUnitData.Commanders.ContainsKey(friendly.Unit.Tag) && ActiveUnitData.Commanders[friendly.Unit.Tag].ChildUnitCalculation != null)
                {
                    return false;
                }
            }
            else if (friendly.Unit.WeaponCooldown > 0 && (friendly.Unit.Health + friendly.Unit.Shield) < healthLimit && friendly.Unit.Shield != friendly.Unit.ShieldMax) // TODO: or could die in one hit
            {
                return true;
            }
            else if (friendly.NearbyAllies.Take(25).Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED && Vector2.DistanceSquared(friendly.Position, e.Position) < 10))
            {
                return true;
            }
            else if (friendly.NearbyEnemies.Take(25).Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED && Vector2.DistanceSquared(friendly.Position, e.Position) < 10))
            {
                return true;
            }

            return false;
        }

        float ClosestEnemyDistance(UnitCalculation friendly)
        {
            var closestEnemy = friendly.NearbyEnemies.OrderBy(u => Vector2.DistanceSquared(u.Position, friendly.Position)).FirstOrDefault();
            if (closestEnemy != null)
            {
                return Vector2.DistanceSquared(closestEnemy.Position, friendly.Position);
            }
            return 0;
        }

        void UpdateLoadTimes(UnitCommander commander)
        {
            foreach (var loadedUnit in commander.LoadTimes.Keys.ToList())
            {
                if (!commander.UnitCalculation.Unit.Passengers.Any(p => p.Tag == loadedUnit))
                {
                    commander.LoadTimes.Remove(loadedUnit);
                }
            }
        }

        Point2D GetPickupSpot(Point2D target, Point2D defensivePoint)
        {
            var angle = Math.Atan2(target.Y - defensivePoint.Y, defensivePoint.X - target.X);
            var x = (PickupRange - .5) * Math.Cos(angle);
            var y = (PickupRange -.5) * Math.Sin(angle);
            return new Point2D { X = target.X + (float)x, Y = target.Y - (float)y };
        }

        bool UnloadUnits(UnitCommander commander, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            // TODO: if already unloading all return false and it will unload as it moves?

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.UNLOADALLAT_WARPPRISM || o.AbilityId == (uint)Abilities.UNLOADUNIT_WARPPRISM)) {
                // if a unit has been in there for more than a second, warp prism must be on unloadable ground, move to a new area then try again
                if (commander.LoadTimes.Any(l => l.Value > 100))
                {
                    action = commander.Order(frame, Abilities.MOVE, defensivePoint);
                }

                return true; 
            }

            if (!MapDataService.PathWalkable(commander.UnitCalculation.Unit.Pos))
            {
                return false;
            }

            foreach (var passenger in commander.UnitCalculation.Unit.Passengers)
            {
                if (!commander.LoadTimes.ContainsKey(passenger.Tag))
                {
                    commander.LoadTimes[passenger.Tag] = frame;
                }

                // use LoadTimes to calculate weapon cooldown
                if (commander.UnitCalculation.Unit.Shield + commander.UnitCalculation.Unit.Health < 50 || passenger.Shield > 25) // unload any units that regained shields, or if warp prism dying
                {
                    action = commander.UnloadSpecificUnit(frame, Abilities.UNLOADUNIT_WARPPRISM, passenger.Tag);
                    return true;
                }
                else
                {
                    if (!ActiveUnitData.SelfUnits.ContainsKey(passenger.Tag))
                    {
                        action = commander.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, commander.UnitCalculation.Unit.Tag);
                        return true;
                    }
                    else
                    {
                        var weapon = ActiveUnitData.SelfUnits[passenger.Tag].Weapon;
                        if (weapon == null || (frame - commander.LoadTimes[passenger.Tag]) / SharkyOptions.FramesPerSecond > weapon.Speed) // unload any units ready to fire
                        {
                            action = commander.UnloadSpecificUnit(frame, Abilities.UNLOADUNIT_WARPPRISM, passenger.Tag);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        UnitCalculation GetSupportTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, IEnumerable<UnitCalculation> supportableUnits = null)
        {
            if (supportableUnits == null)
            {
                supportableUnits = ActiveUnitData.SelfUnits.Values.Where(u => !u.Loaded);
            }

            // no allies that already have a friendly warp prism or warp prism phasing within 8 range
            var otherWarpPrisms = supportableUnits.Where(u => u.Unit.Tag != commander.UnitCalculation.Unit.Tag && (u.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || u.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING));

            var friendlies = supportableUnits.Where(u => u.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && !u.Unit.IsFlying
                && !otherWarpPrisms.Any(o => DistanceSquared(o, u) < 64)
                    && Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position) < 225
                    && u.NearbyEnemies.Any(e => DistanceSquared(u, e) < 225)
                ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).First(), u));

            if (friendlies.Any())
            {
                return friendlies.First();
            }

            // if none
            // get any allies
            // select the friendies with enemies in 15 range
            // order by closest to the enemy
            friendlies = supportableUnits.Where(u => u.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && !u.Unit.IsFlying
                            && !otherWarpPrisms.Any(o => DistanceSquared(o, u) < 64)
                                && u.NearbyEnemies.Any(e => DistanceSquared(u, e) < 225)
                            ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).FirstOrDefault(), u));

            if (friendlies.Any())
            {
                return friendlies.First();
            }

            // if still none
            //get ally closest to target
            friendlies = supportableUnits.Where(u => u.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && !u.Unit.IsFlying
                            && !otherWarpPrisms.Any(o => DistanceSquared(o, u) < 64)
                            ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).FirstOrDefault(), u));

            if (friendlies.Any())
            {
                return friendlies.First();
            }

            // if still none
            //get ally closest to target even if there is another warp prism nearby
            friendlies = supportableUnits.Where(u => u.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && !u.Unit.IsFlying
                            ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).FirstOrDefault(), u));

            if (friendlies.Any())
            {
                return friendlies.First();
            }

            return null;
        }

        bool NavigateToSupportUnit(UnitCommander commander, Point2D target, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (MapDataService.PathWalkable(commander.UnitCalculation.Unit.Pos)) // if it is in unplaceable terrain, can't unload
            {
                if (commander.UnitCalculation.Unit.Shield + commander.UnitCalculation.Unit.Health < 50 || commander.UnitCalculation.EnemiesInRangeOf.Count > 0) // if warp prism dying or enemies nearby unload
                {
                    action = commander.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, commander.UnitCalculation.Unit.Tag);
                    return true;
                }

                foreach (var passenger in commander.UnitCalculation.Unit.Passengers)
                {
                    if (ActiveUnitData.Commanders.ContainsKey(passenger.Tag))
                    {
                        var passengerUnit = ActiveUnitData.Commanders[passenger.Tag].UnitCalculation.Unit;
                        var unit = ActiveUnitData.Commanders[passenger.Tag].UnitCalculation;

                        foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
                        {
                            if (DamageService.CanDamage(unit, enemyAttack) && InRange(commander.UnitCalculation.Position, enemyAttack.Position, unit.Range + passengerUnit.Radius + enemyAttack.Unit.Radius) && MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) == MapDataService.MapHeight(enemyAttack.Unit.Pos))
                            {
                                if (!enemyAttack.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && !InRange(commander.UnitCalculation.Position, enemyAttack.Position, 2 + passengerUnit.Radius + enemyAttack.Unit.Radius))
                                {
                                    continue;
                                }
                                // if an enemy is in range drop the unit
                                //action = commander.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, commander.UnitCalculation.Unit.Tag); // TODO: dropping a specific unit not working, can only drop all, change it if they ever fix the api
                                action = commander.UnloadSpecificUnit(frame, Abilities.UNLOADUNIT_WARPPRISM, passenger.Tag);
                                return true;
                            }
                        }
                    } 
                }

                if (InRange(new Vector2(target.X, target.Y), commander.UnitCalculation.Position, 3)) // if made it to the target just drop
                {
                    action = commander.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, commander.UnitCalculation.Unit.Tag);
                    return true;
                }

            }

            if (commander.UnitCalculation.Unit.CargoSpaceMax > commander.UnitCalculation.Unit.CargoSpaceTaken && commander.UnitCalculation.Unit.Shield + commander.UnitCalculation.Unit.Health > 50) // find more units to load
            {
                var friendly = commander.UnitCalculation.NearbyAllies.Take(25).Where(u => !u.Unit.IsFlying && u.Unit.BuildProgress == 1 && u.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && !u.Loaded && commander.UnitCalculation.Unit.CargoSpaceMax - commander.UnitCalculation.Unit.CargoSpaceTaken >= UnitDataService.CargoSize((UnitTypes)u.Unit.UnitType) && u.EnemiesInRange.Count == 0 && u.EnemiesInRangeOf.Count == 0).OrderBy(u => Vector2.DistanceSquared(commander.UnitCalculation.Position, u.Position)).FirstOrDefault();

                if (friendly != null)
                {
                    if (ActiveUnitData.Commanders.ContainsKey(friendly.Unit.Tag))
                    {
                        if (ActiveUnitData.Commanders[friendly.Unit.Tag].UnitRole != UnitRole.Door)
                        {
                            action = commander.Order(frame, Abilities.LOAD, null, friendly.Unit.Tag);
                            return true;
                        }
                    }
                }
            }

            action = commander.Order(frame, Abilities.MOVE, target);
            return true;
        }

        public override List<SC2APIProtocol.Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            DetermineMiningAction(commander, frame, out action);
            return action;
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (SupportArmy(commander, defensivePoint, defensivePoint, groupCenter, frame, out action))
            {
                return action;
            }
            if (commander.UnitCalculation.NearbyEnemies.Any())
            {
                if (Retreat(commander, defensivePoint, defensivePoint, frame, out action)) { return action; }
            }

            return Idle(commander, defensivePoint, frame);
        }

        float DistanceSquared(UnitCalculation unit1, UnitCalculation unit2)
        {
            if (unit1 == null || unit2 == null)
            {
                return 0;
            }
            return Vector2.DistanceSquared(unit1.Position, unit2.Position);
        }

        bool DetermineMiningAction(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = new List<SC2APIProtocol.Action>();

            if (commander.UnitCalculation.NearbyEnemies.Count > 0)
            {
                return false;
            }

            var nexuses = BaseData.SelfBases.Where(u => u.ResourceCenter != null && u.ResourceCenter.BuildProgress == 1 && u.MineralMiningInfo.Any()).OrderBy(u => u.MineralMiningInfo.Sum(m => m.Workers.Count) / u.MineralMiningInfo.Count()).ThenBy(u => Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(u.Location.X, u.Location.Y)));

            //foreach (var nexusBase in BaseData.Bases)
            //{
            //    DrawSphere(SC2Util.Point(nexusBase.MineralLinePos.X, nexusBase.MineralLinePos.Y, 11));
            //}

            if (commander.UnitCalculation.Unit.Passengers.Count > 0)
            {
                //action = commander.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, commander.UnitCalculation.Unit.Tag);
                foreach (var passenger in commander.UnitCalculation.Unit.Passengers)
                {
                    var passengerAction = commander.UnloadSpecificUnit(frame, Abilities.UNLOADUNIT_WARPPRISM, passenger.Tag);
                    if (passengerAction != null)
                    {
                        action.AddRange(passengerAction);
                    }
                }
                return true;
            }

            if (StopWarping(commander, frame, out action))
            {
                return true;
            }

            var otherWarpPrisms = ActiveUnitData.SelfUnits.Where(u => u.Value.Unit.Tag != commander.UnitCalculation.Unit.Tag && (u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING));
            foreach (var nexus in nexuses)
            {
                if (!otherWarpPrisms.Any(o => Vector2.DistanceSquared(o.Value.Position, new Vector2(nexus.Location.X, nexus.Location.Y)) < 25))
                {
                    var miningLocation = GetMiningSpot(nexus);
                    if (miningLocation != null)
                    {
                        //DrawSphere(SC2Util.Point(miningLocation.X, miningLocation.Y, 11));

                        if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(miningLocation.X, miningLocation.Y)) < .5)
                        {
                            var probe = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.BuffIds.Any(b => SharkyUnitData.CarryingMineralBuffs.Contains((Buffs)b)) && InRange(a.Position, commander.UnitCalculation.Position, PickupRange) && !InRange(a.Position, new Vector2(nexus.Location.X, nexus.Location.Y), nexus.ResourceCenter.Radius + 1)).OrderByDescending(u => Vector2.DistanceSquared(new Vector2(nexus.Location.X, nexus.Location.Y), u.Position)).FirstOrDefault();
                            if (probe != null)
                            {
                                action = commander.Order(frame, Abilities.LOAD, null, probe.Unit.Tag);
                                return true;
                            }
                            else
                            {
                                action = commander.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, miningLocation);
                                return true;
                            }
                        }

                        action = commander.Order(frame, Abilities.MOVE, miningLocation);
                        return true;
                    }
                }
            }

            return false;
        }

        Point2D GetMiningSpot(BaseLocation nexusBase)
        {
            if (nexusBase == null)
            {
                return null;
            }
            var mineralPos = nexusBase.MineralLineLocation;

            var angle = Math.Atan2(nexusBase.Location.Y - mineralPos.Y, mineralPos.X - nexusBase.Location.X);
            var x = nexusBase.ResourceCenter.Radius * Math.Cos(angle);
            var y = nexusBase.ResourceCenter.Radius * Math.Sin(angle);
            return new Point2D { X = nexusBase.Location.X + (float)x, Y = nexusBase.Location.Y - (float)y };
        }
    }
}
