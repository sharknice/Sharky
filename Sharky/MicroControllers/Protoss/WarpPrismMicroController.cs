using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Pathing;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class WarpPrismMicroController : IndividualMicroController
    {
        int PickupRange = 5;

        IBaseManager BaseManager;

        public WarpPrismMicroController(MapDataService mapDataService, UnitDataManager unitDataManager, IUnitManager unitManager, DebugManager debugManager, IPathFinder sharkyPathFinder, SharkyOptions sharkyOptions, MicroPriority microPriority, bool groupUpEnabled, IBaseManager baseManager)
            : base(mapDataService, unitDataManager, unitManager, debugManager, sharkyPathFinder, sharkyOptions, microPriority, groupUpEnabled)
        {
            BaseManager = baseManager;
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (SupportArmy(commander, target, defensivePoint, groupCenter, frame, out action))
            {
                return true;
            }

            return false;
        }

        bool SupportArmy(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame, out SC2APIProtocol.Action action)
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

                if (commander.UnitCalculation.Unit.Shield < 1)
                {
                    if (Retreat(commander, target, defensivePoint, frame, out action))
                    {
                        return true;
                    }
                }
            }

            // follow behind at the range of pickup
            var unitToSupport = GetSupportTarget(commander, target, defensivePoint);

            if (!commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.Tag == unitToSupport.Unit.Tag))
            {
                if (Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(target.X, target.Y)) > Vector2.DistanceSquared(new Vector2(unitToSupport.Unit.Pos.X, unitToSupport.Unit.Pos.Y), new Vector2(target.X, target.Y)))
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
            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.UNLOADALLAT_WARPPRISM) && MapDataService.PathWalkable(moveTo)) // TODO: does this groundpathable thing work right?
            {
                moveTo = new Point2D { X = unitToSupport.Unit.Pos.X, Y = unitToSupport.Unit.Pos.Y };
            }

            if (InRange(moveTo, commander.UnitCalculation.Unit.Pos, 2) && InRange(unitToSupport.Unit.Pos, commander.UnitCalculation.Unit.Pos, PickupRange))
            {
                //look at all units within pickup range, ordered by proximity to their closeest enemy
                // get average hp + shields of back
                // if unit is in front half weapon is off cooldown and (has below that hp + shields or could die in one hit) pick it up
                var friendliesInRange = commander.UnitCalculation.NearbyAllies.Where(u => InRange(u.Unit.Pos, commander.UnitCalculation.Unit.Pos, PickupRange)).OrderBy(u => ClosestEnemyDistance(u));
                var frontHalf = friendliesInRange.Take(friendliesInRange.Count() / 2);
                var backHalf = friendliesInRange.Skip(friendliesInRange.Count() / 2);
                var backAverageHealth = backHalf.Sum(u => u.Unit.Health + u.Unit.Shield) / backHalf.Count();
                foreach (var friendly in frontHalf)
                {
                    if (commander.UnitCalculation.Unit.CargoSpaceMax - commander.UnitCalculation.Unit.CargoSpaceTaken >= UnitDataManager.CargoSize((UnitTypes)friendly.Unit.UnitType))
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
                        if (commander.UnitCalculation.Unit.CargoSpaceMax - commander.UnitCalculation.Unit.CargoSpaceTaken >= UnitDataManager.CargoSize((UnitTypes)friendly.Unit.UnitType))
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

                StartWarping(commander, frame, out action);
                return true;
            }
            else
            {
                // move to pickup the friendly closest to the enemy
                if (StopWarping(commander, frame, out action))
                {
                    return true;
                }

                action = commander.Order(frame, Abilities.MOVE, moveTo);
                return true;
            }
        }

        bool StartWarping(UnitCommander commander, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (!MapDataService.PathWalkable(commander.UnitCalculation.Unit.Pos))
            {
                return false;
            }
            if (UnitManager.Commanders.Values.Where(v => v.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE && !v.UnitCalculation.Unit.IsActive && v.WarpInAlmostOffCooldown(frame, SharkyOptions.FramesPerSecond, UnitDataManager)).Count() == 0)
            {
                return false;
            }
            if (commander.UnitCalculation.Unit.Shield > 25 && !commander.UnitCalculation.NearbyAllies.Any(v => (v.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON || v.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING) && DistanceSquared(commander.UnitCalculation, v) < 400)) // not near any pylons or other warping prisms
            {
                if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM)
                {
                    action = commander.Order(frame, Abilities.MORPH_WARPPRISMPHASINGMODE);
                    return true;
                }
            }
            return false;
        }

        bool StopWarping(UnitCommander commander, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING)
            {
                if (commander.UnitCalculation.NearbyAllies.Any(v => v.Unit.BuildProgress < 1 && Vector2.DistanceSquared(new Vector2(v.Unit.Pos.X, v.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) < 49)) // and not warping any units in
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
            if (friendly.Unit.UnitType == (uint)UnitTypes.PROTOSS_ZEALOT)
            {
                if (friendly.Unit.Shield > 0 || friendly.EnemiesInRange.Count() > 0)
                {
                    return false;
                }
            }
            if (friendly.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTOR)
            {
                if (friendly.Unit.Shield != friendly.Unit.ShieldMax)
                {
                    return UnitManager.Commanders[friendly.Unit.Tag].AbilityOffCooldown(Abilities.EFFECT_PURIFICATIONNOVA, frame, SharkyOptions.FramesPerSecond, UnitDataManager);
                }
            }
            if (friendly.Unit.WeaponCooldown > 0 && (friendly.Unit.Health + friendly.Unit.Shield) < healthLimit && friendly.Unit.Shield != friendly.Unit.ShieldMax) // TODO: or could die in one hit
            {
                return true;
            }
            return false;
        }

        float ClosestEnemyDistance(UnitCalculation friendly)
        {
            var closestEnemy = friendly.NearbyEnemies.OrderBy(u => Vector2.DistanceSquared(new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y), new Vector2(friendly.Unit.Pos.X, friendly.Unit.Pos.Y))).FirstOrDefault();
            if (closestEnemy != null)
            {
                return Vector2.DistanceSquared(new Vector2(closestEnemy.Unit.Pos.X, closestEnemy.Unit.Pos.Y), new Vector2(friendly.Unit.Pos.X, friendly.Unit.Pos.Y));
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
            var x = PickupRange * Math.Cos(angle);
            var y = PickupRange * Math.Sin(angle);
            return new Point2D { X = target.X + (float)x, Y = target.Y - (float)y };
        }

        bool UnloadUnits(UnitCommander commander, Point2D defensivePoint, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.UNLOADALLAT_WARPPRISM)) {
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
                    action = commander.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, commander.UnitCalculation.Unit.Tag); // TODO: dropping a specific unit not working due to api bug, can only drop all, change it if they ever fix the api
                    return true;
                }
                else
                {
                    var weapon = UnitManager.SelfUnits[passenger.Tag].Weapon;
                    if (weapon == null || (frame - commander.LoadTimes[passenger.Tag]) / SharkyOptions.FramesPerSecond > weapon.Speed) // unload any units ready to fire
                    {
                        action = commander.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, commander.UnitCalculation.Unit.Tag); // TODO: dropping a specific unit not working, can only drop all, change it if they ever fix the api
                        return true;
                    }
                }
            }
            return false;
        }

        UnitCalculation GetSupportTarget(UnitCommander commander, Point2D target, Point2D defensivePoint)
        {
            // no allies that already have a friendly warp prism or warp prism phasing within 8 range
            var otherWarpPrisms = UnitManager.SelfUnits.Where(u => u.Value.Unit.Tag != commander.UnitCalculation.Unit.Tag && (u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING));

            var friendlies = UnitManager.SelfUnits.Where(u => u.Value.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !u.Value.Unit.IsFlying
                && !otherWarpPrisms.Any(o => DistanceSquared(o.Value, u.Value) < 64)
                    && Vector2.DistanceSquared(new Vector2(u.Value.Unit.Pos.X, u.Value.Unit.Pos.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) < 225
                    && u.Value.NearbyEnemies.Any(e => DistanceSquared(u.Value, e) < 225)
                ).OrderBy(u => DistanceSquared(u.Value.NearbyEnemies.OrderBy(e => DistanceSquared(e, u.Value)).First(), u.Value));

            if (friendlies.Count() > 0)
            {
                return friendlies.First().Value;
            }

            // if none
            // get any allies
            // select the friendies with enemies in 15 range
            // order by closest to the enemy
            friendlies = UnitManager.SelfUnits.Where(u => u.Value.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !u.Value.Unit.IsFlying
                            && !otherWarpPrisms.Any(o => DistanceSquared(o.Value, u.Value) < 64)
                                && u.Value.NearbyEnemies.Any(e => DistanceSquared(u.Value, e) < 225)
                            ).OrderBy(u => DistanceSquared(u.Value.NearbyEnemies.OrderBy(e => DistanceSquared(e, u.Value)).FirstOrDefault(), u.Value));

            if (friendlies.Count() > 0)
            {
                return friendlies.First().Value;
            }

            // if still none
            //get ally closest to target
            friendlies = UnitManager.SelfUnits.Where(u => u.Value.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !u.Value.Unit.IsFlying
                            && !otherWarpPrisms.Any(o => DistanceSquared(o.Value, u.Value) < 64)
                            ).OrderBy(u => DistanceSquared(u.Value.NearbyEnemies.OrderBy(e => DistanceSquared(e, u.Value)).FirstOrDefault(), u.Value));

            if (friendlies.Count() > 0)
            {
                return friendlies.First().Value;
            }

            // if still none
            //get ally closest to target even if there is another warp prism nearby
            friendlies = UnitManager.SelfUnits.Where(u => u.Value.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !u.Value.Unit.IsFlying
                            ).OrderBy(u => DistanceSquared(u.Value.NearbyEnemies.OrderBy(e => DistanceSquared(e, u.Value)).FirstOrDefault(), u.Value));

            if (friendlies.Count() > 0)
            {
                return friendlies.First().Value;
            }

            return null;
        }

        bool NavigateToSupportUnit(UnitCommander commander, Point2D target, int frame, out SC2APIProtocol.Action action)
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
                    var passengerUnit = UnitManager.Commanders[passenger.Tag].UnitCalculation.Unit;
                    var unit = UnitManager.Commanders[passenger.Tag].UnitCalculation;

                    foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
                    {
                        if (UnitManager.CanDamage(unit.Weapons, enemyAttack.Unit) && InRange(commander.UnitCalculation.Unit.Pos, enemyAttack.Unit.Pos, unit.Range + passengerUnit.Radius + enemyAttack.Unit.Radius) && MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) == MapDataService.MapHeight(enemyAttack.Unit.Pos))
                        {
                            if (!enemyAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !InRange(commander.UnitCalculation.Unit.Pos, enemyAttack.Unit.Pos, 2 + passengerUnit.Radius + enemyAttack.Unit.Radius))
                            {
                                continue;
                            }
                            // if an enemy is in range drop the unit
                            action = commander.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, commander.UnitCalculation.Unit.Tag); // TODO: dropping a specific unit not working, can only drop all, change it if they ever fix the api
                            return true;
                        }
                    }
                }

                if (InRange(target, commander.UnitCalculation.Unit.Pos, 3)) // if made it to the target just drop
                {
                    action = commander.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, commander.UnitCalculation.Unit.Tag);
                    return true;
                }

            }

            if (commander.UnitCalculation.Unit.CargoSpaceMax > commander.UnitCalculation.Unit.CargoSpaceTaken && commander.UnitCalculation.Unit.Shield + commander.UnitCalculation.Unit.Health > 50) // find more units to load
            {
                var friendly = commander.UnitCalculation.NearbyAllies.Where(u => !u.Unit.IsFlying && u.Unit.BuildProgress == 1 && u.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !commander.UnitCalculation.Unit.Passengers.Any(p => p.Tag == u.Unit.Tag) && commander.UnitCalculation.Unit.CargoSpaceMax - commander.UnitCalculation.Unit.CargoSpaceTaken >= UnitDataManager.CargoSize((UnitTypes)u.Unit.UnitType) && u.EnemiesInRange.Count == 0 && u.EnemiesInRangeOf.Count == 0).OrderBy(u => Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y))).FirstOrDefault();

                if (friendly != null)
                {
                    action = commander.Order(frame, Abilities.LOAD, null, friendly.Unit.Tag);
                    return true;
                }
            }

            action = commander.Order(frame, Abilities.MOVE, target);
            return true;
        }

        public override SC2APIProtocol.Action Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            SC2APIProtocol.Action action = null;
            DetermineMiningAction(commander, frame, out action);
            return action;
        }

        public override SC2APIProtocol.Action Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            // TODO: pick up nearby units that are retreating to help them retreat faster
            return Idle(commander, defensivePoint, frame);
        }

        float DistanceSquared(UnitCalculation unit1, UnitCalculation unit2)
        {
            if (unit1 == null || unit2 == null)
            {
                return 0;
            }
            return Vector2.DistanceSquared(new Vector2(unit1.Unit.Pos.X, unit1.Unit.Pos.Y), new Vector2(unit2.Unit.Pos.X, unit2.Unit.Pos.Y));
        }

        bool DetermineMiningAction(UnitCommander commander, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (commander.UnitCalculation.NearbyEnemies.Count > 0)
            {
                return false;
            }

            var nexuses = UnitManager.SelfUnits.Where(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && u.Value.Unit.BuildProgress == 1 && u.Value.Unit.AssignedHarvesters > 0 && u.Value.Unit.IdealHarvesters > 0).OrderBy(u => u.Value.Unit.AssignedHarvesters / (float)u.Value.Unit.IdealHarvesters).ThenBy(u => DistanceSquared(commander.UnitCalculation, u.Value));

            //foreach (var nexusBase in BaseManager.Bases)
            //{
            //    DrawSphere(SC2Util.Point(nexusBase.MineralLinePos.X, nexusBase.MineralLinePos.Y, 11));
            //}

            if (commander.UnitCalculation.Unit.Passengers.Count > 0)
            {
                action = commander.Order(frame, Abilities.UNLOADALLAT_WARPPRISM, null, commander.UnitCalculation.Unit.Tag);
                return true;
            }

            if (StopWarping(commander, frame, out action))
            {
                return true;
            }

            var otherWarpPrisms = UnitManager.SelfUnits.Where(u => u.Value.Unit.Tag != commander.UnitCalculation.Unit.Tag && (u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING));
            foreach (var nexus in nexuses)
            {
                if (!otherWarpPrisms.Any(o => Vector2.DistanceSquared(new Vector2(o.Value.Unit.Pos.X, o.Value.Unit.Pos.Y), new Vector2(nexus.Value.Unit.Pos.X, nexus.Value.Unit.Pos.Y)) < 25))
                {
                    var miningLocation = GetMiningSpot(nexus.Value);
                    if (miningLocation != null)
                    {
                        //DrawSphere(SC2Util.Point(miningLocation.X, miningLocation.Y, 11));

                        if (Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(miningLocation.X, miningLocation.Y)) < .5)
                        {
                            var probe = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.BuffIds.Any(b => UnitDataManager.CarryingMineralBuffs.Contains((Buffs)b)) && InRange(a.Unit.Pos, commander.UnitCalculation.Unit.Pos, PickupRange) && !InRange(a.Unit.Pos, nexus.Value.Unit.Pos, nexus.Value.Unit.Radius + 1)).OrderByDescending(u => DistanceSquared(nexus.Value, u)).FirstOrDefault();
                            if (probe != null)
                            {
                                action = commander.Order(frame, Abilities.LOAD, null, probe.Unit.Tag);
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

        Point2D GetMiningSpot(UnitCalculation nexus)
        {
            var nexusPoint = new Point2D { X = nexus.Unit.Pos.X, Y = nexus.Unit.Pos.Y };
            var nexusBase = BaseManager.BaseLocations.Where(b => b.Location.X == nexusPoint.X && b.Location.Y == nexusPoint.Y).FirstOrDefault();
            if (nexusBase == null)
            {
                return nexusPoint;
            }
            var mineralPos = nexusBase.MineralLineLocation;

            var angle = Math.Atan2(nexusPoint.Y - mineralPos.Y, mineralPos.X - nexusPoint.X);
            var x = nexus.Unit.Radius * Math.Cos(angle);
            var y = nexus.Unit.Radius * Math.Sin(angle);
            return new Point2D { X = nexusPoint.X + (float)x, Y = nexusPoint.Y - (float)y };
        }
    }
}
