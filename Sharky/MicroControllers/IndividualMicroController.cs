using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.MicroTasks.Attack;
using Sharky.Pathing;
using Sharky.S2ClientTypeEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers
{
    public class IndividualMicroController : IIndividualMicroController
    {
        protected MapDataService MapDataService;
        protected SharkyUnitData SharkyUnitData;
        protected ActiveUnitData ActiveUnitData;
        protected DebugService DebugService;
        protected IPathFinder SharkyPathFinder;
        protected BaseData BaseData;
        protected SharkyOptions SharkyOptions;
        protected DamageService DamageService;
        protected UnitDataService UnitDataService;
        protected TargetingData TargetingData;
        protected TargetingService TargetingService;

        public MicroPriority MicroPriority { get; set; }

        public bool GroupUpEnabled;
        protected float GroupUpDistanceSmall;
        protected float GroupUpDistance;
        protected float GroupUpDistanceMax;
        protected float AvoidDamageDistance;
        protected float LooseFormationDistance;
        protected float GroupUpStateDistanceSquared;

        public IndividualMicroController(MapDataService mapDataService, SharkyUnitData unitDataManager, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, TargetingService targetingService, MicroPriority microPriority, bool groupUpEnabled, float avoidDamageDistance = .5f)
        {
            MapDataService = mapDataService;
            SharkyUnitData = unitDataManager;
            ActiveUnitData = activeUnitData;
            DebugService = debugService;
            SharkyPathFinder = sharkyPathFinder;
            BaseData = baseData;
            SharkyOptions = sharkyOptions;
            MicroPriority = microPriority;
            GroupUpEnabled = groupUpEnabled;
            DamageService = damageService;
            UnitDataService = unitDataService;
            TargetingData = targetingData;
            TargetingService = targetingService;

            GroupUpDistanceSmall = 5;
            GroupUpDistance = 10;
            GroupUpDistanceMax = 50;
            AvoidDamageDistance = avoidDamageDistance;
            LooseFormationDistance = 1.75f;
            GroupUpStateDistanceSquared = 25f;
        }

        public IndividualMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder pathFinder, MicroPriority microPriority, bool groupUpEnabled, float avoidDamageDistance = .5f)
        {
            MapDataService = defaultSharkyBot.MapDataService;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            DebugService = defaultSharkyBot.DebugService;
            BaseData = defaultSharkyBot.BaseData;
            TargetingData = defaultSharkyBot.TargetingData;

            SharkyOptions = defaultSharkyBot.SharkyOptions;

            SharkyPathFinder = pathFinder;
            MicroPriority = microPriority;
            GroupUpEnabled = groupUpEnabled;

            DamageService = defaultSharkyBot.DamageService;
            UnitDataService = defaultSharkyBot.UnitDataService;
            TargetingService = defaultSharkyBot.TargetingService;

            GroupUpDistanceSmall = 5;
            GroupUpDistance = 10;
            GroupUpDistanceMax = 50;
            AvoidDamageDistance = avoidDamageDistance;
            LooseFormationDistance = 1.75f;
            GroupUpStateDistanceSquared = 100f;
        }

        public virtual List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            if (commander.UnitCalculation.Unit.IsOnScreen)
            {
                var breakpoint = true;
            }

            List<SC2APIProtocol.Action> action = null;

            var formation = GetDesiredFormation(commander);
            var bestTarget = GetBestTarget(commander, target, frame);

            UpdateState(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame);

            if (GroupUpBasedOnState(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            if (PreOffenseOrder(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (AvoidTargettedOneHitKills(commander, target, defensivePoint, frame, out action)) { return action; }

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (MicroPriority == MicroPriority.StayOutOfRange)
            {
                if (SpecialCaseRetreat(commander, target, defensivePoint, frame, out action)) { return action; }
                if (MoveAway(commander, target, defensivePoint, frame, out action)) { return action; }
            }

            if (WeaponReady(commander, frame))
            {
                if (AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }
            }

            if (Move(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            return commander.Order(frame, Abilities.ATTACK, target);
        }

        public virtual List<SC2APIProtocol.Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            UpdateState(commander, defensivePoint, defensivePoint, null, null, Formation.Normal, frame);
            if (RechargeShieldsAtBattery(commander, defensivePoint, defensivePoint, frame, out action)) { return action; }
            return action;
        }

        public virtual List<SC2APIProtocol.Action> Scout(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool prioritizeVision = false)
        {
            List<SC2APIProtocol.Action> action = null;
            UpdateState(commander, target, defensivePoint, null, null, Formation.Normal, frame);

            if (!prioritizeVision || MapDataService.SelfVisible(target))
            {
                if (WeaponReady(commander, frame) && (commander.UnitCalculation.Unit.Shield + commander.UnitCalculation.Unit.Health) > (commander.UnitCalculation.Unit.ShieldMax + (commander.UnitCalculation.Unit.HealthMax / 2.0f)))
                {
                    var bestTarget = GetBestTarget(commander, target, frame);
                    if (bestTarget != null && bestTarget.UnitClassifications.Contains(UnitClassification.Worker) && commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag))
                    {
                        if (AttackBestTarget(commander, target, defensivePoint, target, bestTarget, frame, out action))
                        {
                            return action;
                        }
                    }
                }
            }

            if (prioritizeVision && !MapDataService.SelfVisible(target))
            {
                return commander.Order(frame, Abilities.MOVE, target);
            }

            if (WorkerEscapeSurround(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            if (AvoidTargettedOneHitKills(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            if (!MapDataService.SelfVisible(target))
            {
               return commander.Order(frame, Abilities.MOVE, target);
            }
            else
            {
                // TODO: circle around base
                return Bait(commander, target, defensivePoint, null, frame);
            }
        }

        public virtual List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            UpdateState(commander, defensivePoint, defensivePoint, groupCenter, null, Formation.Normal, frame);

            if (GroupUpBasedOnState(commander, defensivePoint, defensivePoint, groupCenter, null, frame, out List<SC2APIProtocol.Action> action)) { return action; }

            // TODO: setup a concave above the ramp if there is a ramp, get earch grid point on high ground, make sure at least one unit on each point
            if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(defensivePoint.X, defensivePoint.Y)) > 25 || MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) < MapDataService.MapHeight(defensivePoint))
            {
                if (Retreat(commander, defensivePoint, defensivePoint, frame, out action)) { return action; }
                return commander.Order(frame, Abilities.MOVE, defensivePoint);
            }
            else
            {
                return Idle(commander, defensivePoint, frame);
            }
        }

        public virtual List<SC2APIProtocol.Action> Bait(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            UpdateState(commander, target, defensivePoint, groupCenter, null, Formation.Normal, frame);

            var formation = GetDesiredFormation(commander);
            var bestTarget = GetBestTarget(commander, target, frame);

            if (WorkerEscapeSurround(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            if (AvoidTargettedOneHitKills(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            if (NavigateToTarget(commander, target, groupCenter, bestTarget, formation, frame, out action))
            {
                return action;
            }

            return commander.Order(frame, Abilities.ATTACK, target);
        }

        protected virtual bool Move(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat))
            {
                if (Retreat(commander, target, defensivePoint, frame, out action)) { return true; }
            }

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return true; }

            if (MoveAway(commander, target, defensivePoint, frame, out action)) { return true; }

            return NavigateToTarget(commander, target, groupCenter, bestTarget, formation, frame, out action);
        }

        protected virtual bool SpecialCaseMove(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (AvoidPurificationNovas(commander, target, defensivePoint, frame, out action)) { return true; }

            if (AvoidRavagerShots(commander, target, defensivePoint, frame, out action)) { return true; }

            if (AvoidStorms(commander, target, defensivePoint, frame, out action)) { return true; }

            if (DealWithCyclones(commander, target, defensivePoint, frame, out action)) { return true; }

            if (DealWithSiegedTanks(commander, target, defensivePoint, frame, out action)) { return true; }

            if (AvoidReaperCharges(commander, target, defensivePoint, frame, out action)) { return true; }

            if (RechargeShieldsAtBattery(commander, target, defensivePoint, frame, out action)) { return true; }

            // TODO: special case movement
            //if (ChargeBlindly(commander, target))
            //{
            //    return true;
            //}

            //if (ChargeUpRamp(commander, target, bestTarget))
            //{
            //    return true;
            //}

            //if (FollowShades(commander))
            //{
            //    return true;
            //}

            return false;
        }

        protected virtual bool SpecialCaseRetreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (AvoidPurificationNovas(commander, target, defensivePoint, frame, out action)) { return true; }

            if (AvoidRavagerShots(commander, target, defensivePoint, frame, out action)) { return true; }

            if (AvoidStorms(commander, target, defensivePoint, frame, out action)) { return true; }

            if (AvoidReaperCharges(commander, target, defensivePoint, frame, out action)) { return true; }

            if (RechargeShieldsAtBattery(commander, target, defensivePoint, frame, out action)) { return true; }

            return false;
        }

        protected virtual bool GetHighGroundVision(UnitCommander commander, Point2D target, Point2D defensivePoint, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (!commander.UnitCalculation.Unit.IsFlying && commander.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_COLOSSUS)
            {
                if (bestTarget != null && !MapDataService.SelfVisible(bestTarget.Unit.Pos) && MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) != MapDataService.MapHeight(bestTarget.Unit.Pos))
                {
                    var badChokes = TargetingData.ChokePoints.Bad.Where(b => Vector2.DistanceSquared(b.Center, bestTarget.Position) < 100 && Vector2.DistanceSquared(b.Center, commander.UnitCalculation.Position) < 100);
                    if (badChokes.Count() > 0)
                    {
                        var chokePoint = badChokes.OrderBy(b => Vector2.DistanceSquared(b.Center, commander.UnitCalculation.Position)).First();
                        var distanceToBestTarget = Vector2.DistanceSquared(bestTarget.Position, commander.UnitCalculation.Position);
                        if (distanceToBestTarget < 121 || distanceToBestTarget > Vector2.DistanceSquared(chokePoint.Center, commander.UnitCalculation.Position))
                        {
                            action = commander.Order(frame, Abilities.MOVE, new Point2D { X = chokePoint.Center.X, Y = chokePoint.Center.Y });
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        protected virtual bool RechargeShieldsAtBattery(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.ShieldMax > 0 && commander.UnitCalculation.Unit.Shield < 10)
            {
                var shieldBatttery = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && a.Unit.BuildProgress == 1 && a.Unit.Energy > 5 && a.Unit.Orders.Count() == 0).OrderBy(b => Vector2.DistanceSquared(commander.UnitCalculation.Position, b.Position)).FirstOrDefault();
                if (shieldBatttery != null)
                {
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, shieldBatttery.Position) > 35)
                    {
                        action = commander.Order(frame, Abilities.MOVE, new Point2D { X = shieldBatttery.Position.X, Y = shieldBatttery.Position.Y });
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual bool NavigateToTarget(UnitCommander commander, Point2D target, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (GetInFormation(commander, formation, frame, out action))
            {
                return true;
            }

            if (bestTarget != null && commander.UnitCalculation.NearbyEnemies.Any(enemy => enemy.Unit.Tag == bestTarget.Unit.Tag))
            {
                if ((MicroPriority == MicroPriority.NavigateToLocation) && !commander.UnitCalculation.EnemiesInRange.Any(enemy => enemy.Unit.Tag == bestTarget.Unit.Tag))
                {

                }
                else
                {
                    return MoveToAttackTarget(commander, bestTarget, frame, out action);
                }
            }

            if (GroupUpEnabled && GroupUp(commander, target, groupCenter, false, frame, out action)) { return true; }

            if (AvoidDeceleration(commander, target, frame, out action)) { return true; }

            if (bestTarget != null && MicroPriority != MicroPriority.NavigateToLocation && MapDataService.SelfVisible(bestTarget.Unit.Pos))
            {
                return MoveToAttackTarget(commander, bestTarget, frame, out action);
            }

            action = commander.Order(frame, Abilities.MOVE, target);
            return true;
        }

        public virtual bool AvoidDeceleration(UnitCommander commander, Point2D target, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            // if air unit or worker (units with acceleration) and within X distance of target move at a 45 degree angle from target to avoid decelerating
            if (commander.UnitCalculation.Unit.IsFlying || commander.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker))
            {
                if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), commander.UnitCalculation.Position) < 4)
                {
                    var angle = commander.UnitCalculation.Unit.Facing + ((float)Math.PI * .25f);
                    var point = GetPoint(commander.UnitCalculation.Position, angle, 5);
                    if (point.X < 0 || point.X > MapDataService.MapData.MapWidth)
                    {
                        point.X = target.X;
                    }
                    if (point.Y < 0 || point.Y > MapDataService.MapData.MapHeight)
                    {
                        point.Y = target.Y;
                    }
                    action = commander.Order(frame, Abilities.MOVE, point);
                    return true;
                }
            }
            return false;
        }

        protected virtual bool MoveToAttackTarget(UnitCommander commander, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (commander.UnitCalculation.Unit.IsFlying && bestTarget.Attributes.Contains(SC2APIProtocol.Attribute.Structure) || commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax ||
                (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && 
                (commander.UnitCalculation.Unit.IsFlying || commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_REAPER || commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS || MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) == MapDataService.MapHeight(bestTarget.Unit.Pos))))
            {
                if (SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType))
                {
                    var point = new Point2D { X = bestTarget.Position.X, Y = bestTarget.Position.Y };
                    action = commander.Order(frame, Abilities.MOVE, point);
                    return true;
                }

                var attackPoint = GetPositionFromRange(commander, bestTarget.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Range + bestTarget.Unit.Radius + commander.UnitCalculation.Unit.Radius);
                if (commander.UnitCalculation.Unit.IsFlying)
                {
                    if (!commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageAir))
                    {
                        attackPoint = new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y };
                    }
                    if (AvoidDeceleration(commander, attackPoint, frame, out action)) { return true; }
                }

                action = commander.Order(frame, Abilities.MOVE, attackPoint);
                return true;
            }
            else
            {
                var position = GetSurroundPoint(commander, bestTarget);
                action = commander.Order(frame, Abilities.MOVE, position);
                return true;
            }
        }

        protected virtual Point2D GetSurroundPoint(UnitCommander commander, UnitCalculation bestTarget)
        {
            if (commander.UnitCalculation.Unit.IsFlying || bestTarget.Attributes.Contains(SC2APIProtocol.Attribute.Structure) || bestTarget.Unit.IsFlying || bestTarget.Unit.UnitType == (uint)UnitTypes.ZERG_LARVA || bestTarget.Unit.UnitType == (uint)UnitTypes.ZERG_BROODLING)
            {
                return new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y };
            }

            var angle = bestTarget.Unit.Facing;
            if (angle == 0 && bestTarget.Vector.X != 0 && bestTarget.Vector.Y != 0)
            {
                angle = (float)Math.Atan2(bestTarget.Vector.Y, bestTarget.Vector.X);
            }
            // block enemy unit, if already blocked then surround enemy unit, by forming circle around it, cover one of 4 sides, if the sides are all taken by allies just move directly on
            var front = GetPoint(bestTarget.Position, angle, bestTarget.Unit.Radius);
            if (!PointBlocked(bestTarget, front, commander.UnitCalculation.Unit.Tag))
            {
                return front;
            }
            var behind = GetPoint(bestTarget.Position, angle + (float)Math.PI, bestTarget.Unit.Radius);
            if (!PointBlocked(bestTarget, behind, commander.UnitCalculation.Unit.Tag))
            {
                return behind;
            }
            var right = GetPoint(bestTarget.Position, angle + ((float)Math.PI * .5f), bestTarget.Unit.Radius);
            if (!PointBlocked(bestTarget, right, commander.UnitCalculation.Unit.Tag))
            {
                return right;
            }
            var left = GetPoint(bestTarget.Position, angle + ((float)Math.PI * 1.5f), bestTarget.Unit.Radius);
            if (!PointBlocked(bestTarget, left, commander.UnitCalculation.Unit.Tag))
            {
                return left;
            }

            return new Point2D { X = bestTarget.Position.X, Y = bestTarget.Position.Y };
        }

        protected bool PointBlocked(UnitCalculation unitCalculation, Point2D point, ulong excludedUnit)
        {
            var vector = new Vector2(point.X, point.Y);
            if (unitCalculation.NearbyEnemies.Any(u => u.Unit.Tag != excludedUnit && Vector2.DistanceSquared(vector, u.Position) < .1))
            {
                return true;
            }
            if (unitCalculation.NearbyAllies.Any(u => u.Unit.Tag != excludedUnit && Vector2.DistanceSquared(vector, u.Position) < .1))
            {
                return true;
            }
            return false;
        }

        protected Point2D GetPoint(Vector2 start, float angle, float distance)
        {
            var x = (float)(distance * Math.Sin(angle + (Math.PI / 2)));
            var y = (float)(distance * Math.Cos(angle + (Math.PI / 2)));
            return new Point2D { X = start.X + x, Y = start.Y + y };
        }

        protected virtual bool GetInFormation(UnitCommander commander, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat) { return false; }

            if (formation == Formation.Normal)
            {
                return false;
            }

            if (formation == Formation.Loose)
            {
                var closestAlly = commander.UnitCalculation.NearbyAllies.OrderBy(a => Vector2.DistanceSquared(commander.UnitCalculation.Position, a.Position)).FirstOrDefault();
                if (closestAlly != null)
                {
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, closestAlly.Position) < (LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius) * (LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius))
                    {
                        var avoidPoint = GetPositionFromRange(commander, closestAlly.Unit.Pos, commander.UnitCalculation.Unit.Pos, LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius + .5f);
                        action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                        return true;
                    }
                }
            }

            if (formation == Formation.Tight && commander.UnitCalculation.NearbyEnemies.Count() > 0)
            {
                var vectors = commander.UnitCalculation.NearbyAllies.Where(a => (!a.Unit.IsFlying && !commander.UnitCalculation.Unit.IsFlying) || (a.Unit.IsFlying && commander.UnitCalculation.Unit.IsFlying)).Select(u => u.Position);
                if (vectors.Count() > 0)
                {
                    var max = 1;
                    if (commander.UnitCalculation.Unit.IsFlying)
                    {
                        max = 4;
                    }
                    var center = new Point2D { X = vectors.Average(v => v.X), Y = vectors.Average(v => v.Y) };
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(center.X, center.Y)) + commander.UnitCalculation.Unit.Radius > max)
                    {
                        action = commander.Order(frame, Abilities.MOVE, center);
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual bool MoveAway(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.TargetPriorityCalculation.Overwhelm || MicroPriority == MicroPriority.AttackForward)
            {
                if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action)) { return true; }

                if (commander.UnitCalculation.Unit.ShieldMax > 0 && commander.UnitCalculation.Unit.Shield < 25 && AvoidDamage(commander, target, defensivePoint, frame, out action)) // TODO: this only works for protoss, if we want it to work for zerg and terran it needs to change
                {
                    return true;
                }
            }
            else if (MicroPriority == MicroPriority.LiveAndAttack)
            {
                if (WorkerEscapeSurround(commander, target, defensivePoint, frame, out action)) { return true; }

                if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }

                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual bool AvoidDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            var attacks = new List<UnitCalculation>();

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (DamageService.CanDamage(enemyAttack, commander.UnitCalculation) && InRange(commander.UnitCalculation.Position, enemyAttack.Position, UnitDataService.GetRange(enemyAttack.Unit) + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius + AvoidDamageDistance))
                {
                    attacks.Add(enemyAttack);
                }
            }

            if (attacks.Count > 0)
            {
                var attack = attacks.OrderBy(e => (e.Range * e.Range) - Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position)).FirstOrDefault();  // enemies that are closest to being outranged
                var range = UnitDataService.GetRange(attack.Unit);
                if (attack.Range > range)
                {
                    range = attack.Range;
                }

                if (commander.UnitCalculation.Range < range && commander.UnitCalculation.UnitTypeData.MovementSpeed <= attack.UnitTypeData.MovementSpeed)
                {
                    return false; // if we can't get out of range before we attack again don't bother running away
                }

                if (commander.RetreatPathFrame + 20 < frame)
                {
                    if (commander.UnitCalculation.Unit.IsFlying)
                    {
                        commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, defensivePoint.X, defensivePoint.Y, frame);
                    }
                    else
                    {
                        commander.RetreatPath = SharkyPathFinder.GetSafeGroundPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, defensivePoint.X, defensivePoint.Y, frame);
                    }
                    commander.RetreatPathFrame = frame;
                    commander.RetreatPathIndex = 1;
                }
                if (FollowPath(commander, frame, out action))
                {
                    return true;
                }

                var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance);
                if (AvoidDeceleration(commander, avoidPoint, frame, out action)) { return true; }
                action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                return true;
            }

            if (MaintainRange(commander, frame, out action)) { return true; }

            return false;
        }

        protected virtual bool MaintainRange(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (MicroPriority == MicroPriority.JustLive)
            {
                return false;
            }

            if (commander.UnitCalculation.Unit.ShieldMax > 0 && commander.UnitCalculation.Unit.Shield < 1)
            {
                return false;
            }

            var range = commander.UnitCalculation.Range;
            var enemiesInRange = new List<UnitCalculation>();

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (DamageService.CanDamage(enemyAttack, commander.UnitCalculation) && InRange(commander.UnitCalculation.Position, enemyAttack.Position, range + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius + AvoidDamageDistance))
                {
                    enemiesInRange.Add(enemyAttack);
                }
            }

            var closestEnemy = enemiesInRange.OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closestEnemy == null)
            {
                return false;
            }

            var avoidPoint = GetPositionFromRange(commander, closestEnemy.Unit.Pos, commander.UnitCalculation.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + closestEnemy.Unit.Radius);
            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
            return true;
        }

        protected virtual bool Retreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            // TODO: if this unit is low health and the enemies will catch up if it shoots, don't shoot, just keep running, for example stalker running away from roach, if distancetoroach - (roachspeed * stalkerfiretime) < roachrange , do not fire, just run
            // but also if the unit is just going to die because it's in range of enemies already, fire away because it's going to die anyways

            if (commander.UnitCalculation.EnemiesInRange.Any() && WeaponReady(commander, frame) && !SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType)) // keep shooting as you retreat
            {
                var bestTarget = GetBestTarget(commander, target, frame);
                if (bestTarget != null && MapDataService.SelfVisible(bestTarget.Unit.Pos))
                {
                    action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    return true;
                }               
            }

            var closestEnemy = commander.UnitCalculation.NearbyEnemies.OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();

            if (commander.UnitCalculation.NearbyEnemies.All(e => e.Range < commander.UnitCalculation.Range && e.UnitTypeData.MovementSpeed < commander.UnitCalculation.UnitTypeData.MovementSpeed))
            {
                if (closestEnemy != null && MapDataService.SelfVisible(closestEnemy.Unit.Pos))
                {
                    if (commander.UnitCalculation.Range > closestEnemy.Range)
                    {
                        var speed = commander.UnitCalculation.UnitTypeData.MovementSpeed;
                        var enemySpeed = closestEnemy.UnitTypeData.MovementSpeed;
                        if (closestEnemy.Unit.BuffIds.Contains((uint)Buffs.MEDIVACSPEEDBOOST))
                        {
                            enemySpeed = 5.94f;
                        }
                        if (closestEnemy.Unit.BuffIds.Contains((uint)Buffs.STIMPACK) || closestEnemy.Unit.BuffIds.Contains((uint)Buffs.STIMPACKMARAUDER))
                        {
                            enemySpeed += 1.57f;
                        }

                        if (speed > enemySpeed || closestEnemy.Range + 3 < commander.UnitCalculation.Range)
                        {
                            if (MaintainRange(commander, frame, out action)) { return true; }
                        }
                    }
                }
            }

            if (closestEnemy != null && commander.RetreatPathFrame + 20 < frame)
            {
                if (commander.UnitCalculation.Unit.IsFlying)
                {
                    commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, defensivePoint.X, defensivePoint.Y, frame);
                }
                else
                {
                    commander.RetreatPath = SharkyPathFinder.GetSafeGroundPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, defensivePoint.X, defensivePoint.Y, frame);
                }
                commander.RetreatPathFrame = frame;
                commander.RetreatPathIndex = 1;
            }

            if (FollowPath(commander, frame, out action)) { return true; }

            action = commander.Order(frame, Abilities.MOVE, defensivePoint);
            return true;
        }

        protected bool FollowPath(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.RetreatPath.Count() > 0)
            {
                if (SharkyOptions.Debug)
                {
                    var thing = commander.RetreatPath.ToList();
                    for (int index = 0; index < thing.Count - 1; index++)
                    {
                        DebugService.DrawLine(new Point { X = thing[index].X, Y = thing[index].Y, Z = 16 }, new Point { X = thing[index + 1].X, Y = thing[index + 1].Y, Z = 16 }, new Color { R = 0, G = 0, B = 255 });
                    }
                }

                if (commander.RetreatPathIndex < commander.RetreatPath.Count())
                {
                    var point = commander.RetreatPath[commander.RetreatPathIndex];

                    action = commander.Order(frame, Abilities.MOVE, new Point2D { X = point.X, Y = point.Y });
                    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = commander.UnitCalculation.Unit.Pos.Z }, 1, new Color { R = 0, G = 0, B = 255 });

                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, point) < 4)
                    {
                        commander.RetreatPathIndex++;
                    }

                    return true;
                }
            }

            return false;
        }

        protected float PredictedHealth(UnitCalculation unitCalculation)
        {
            return unitCalculation.Unit.Health + unitCalculation.Unit.Shield + unitCalculation.SimulatedHealPerSecond - unitCalculation.IncomingDamage;
        }

        // TODO: avoid putting unit in range of static defense, or any enemy range if possible, unless those units are already attacking friendly units
        protected virtual UnitCalculation GetBestTarget(UnitCommander commander, Point2D target, int frame)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            var range = commander.UnitCalculation.Range;

            var attacks = commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType == DisplayType.Visible && AttackersFilter(commander, u)); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Count() > 0)
            {
                var oneShotKills = attacks.Where(a => PredictedHealth(a) < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData) && !a.Unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD));
                if (oneShotKills.Count() > 0)
                {
                    if (existingAttackOrder != null)
                    {
                        var existing = oneShotKills.FirstOrDefault(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }

                    var oneShotKill = GetBestTargetFromList(commander, oneShotKills, existingAttackOrder);
                    if (oneShotKill != null)
                    {
                        return oneShotKill;
                    }
                    else
                    {
                        commander.BestTarget = oneShotKills.OrderBy(o => o.Dps).FirstOrDefault();
                        return commander.BestTarget;
                    }
                }

                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null && (bestAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || (bestAttack.UnitClassifications.Contains(UnitClassification.Worker) && bestAttack.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag))))
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
                //if (bestAttack != null && MapAnalyzer.IsChoke(bestAttack.Unit.Pos)) // TODO: if it's a blocking a choke point attack it
                //{
                //    commander.BestTarget = bestAttack;
                //    return bestAttack;
                //}
            }

            if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_TEMPEST)
            {
                range = 10;
            }
            // TODO: don't go attack units super far away if there are still units that can't attack this unit, but are close
            var outOfRangeAttacks = commander.UnitCalculation.NearbyEnemies.Where(enemyAttack => !commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == enemyAttack.Unit.Tag)
                && enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation, enemyAttack) && AttackersFilter(commander, enemyAttack));

            attacks = outOfRangeAttacks.Where(enemyAttack => enemyAttack.EnemiesInRange.Count() > 0);
            if (attacks.Count() > 0)
            {
                var bestOutOfRangeAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestOutOfRangeAttack != null && (bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
                {
                    commander.BestTarget = bestOutOfRangeAttack;
                    return bestOutOfRangeAttack;
                }
                if (bestAttack == null)
                {
                    bestAttack = bestOutOfRangeAttack;
                }
            }

            //attacks = outOfRangeAttacks.Where(enemyAttack => !enemyAttack.NearbyAllies.Any(u => u.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag) ||
            //            (DamageService.CanDamage(u, commander.UnitCalculation) && (Vector2.DistanceSquared(enemyAttack.Position, u.Position) < (u.Range * u.Range) ||
            //            Vector2.DistanceSquared(commander.UnitCalculation.Position, u.Position) < (u.Range * u.Range)))));
            attacks = outOfRangeAttacks;
            if (attacks.Count() > 0)
            {
                var bestOutOfRangeAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestOutOfRangeAttack != null && (bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
                {
                    commander.BestTarget = bestOutOfRangeAttack;
                    return bestOutOfRangeAttack;
                }
                if (bestAttack == null)
                {
                    bestAttack = bestOutOfRangeAttack;
                }
            }

            if (!MapDataService.SelfVisible(target)) // if enemy main is unexplored, march to enemy main
            {
                var fakeMainBase = new Unit(commander.UnitCalculation.Unit);
                fakeMainBase.Pos = new Point { X = target.X, Y = target.Y, Z = 1 };
                fakeMainBase.Alliance = Alliance.Enemy;
                return new UnitCalculation(fakeMainBase, 0, SharkyUnitData, SharkyOptions, UnitDataService, frame);
            }
            var unitsNearEnemyMain = ActiveUnitData.EnemyUnits.Values.Where(e => e.Unit.UnitType != (uint)UnitTypes.ZERG_LARVA && InRange(new Vector2(target.X, target.Y), e.Position, 20));
            if (unitsNearEnemyMain.Count() > 0 && InRange(new Vector2(target.X, target.Y), commander.UnitCalculation.Position, 100))
            {
                attacks = unitsNearEnemyMain.Where(enemyAttack => enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation, enemyAttack) && AttackersFilter(commander, enemyAttack));
                if (attacks.Count() > 0)
                {
                    var bestMainAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                    if (bestMainAttack != null && (bestMainAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestMainAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
                    {
                        commander.BestTarget = bestMainAttack;
                        return bestMainAttack;
                    }
                    if (bestAttack == null)
                    {
                        bestAttack = bestMainAttack;
                    }
                }
            }

            commander.BestTarget = bestAttack;
            return bestAttack;
        }

        protected Point2D GetBestTargetAttackPoint(UnitCommander commander, UnitCalculation bestTarget)
        {
            var enemyPosition = new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y };
            // TODO: make sure this is working correctly, not sure if Vector is accurate, or this function is accurate
            if (bestTarget.Velocity > 0)
            {
                var interceptionPoint = CalculateInterceptionPoint(bestTarget.Position, bestTarget.AverageVector, commander.UnitCalculation.End, commander.UnitCalculation.Velocity);
                var point = new Point2D { X = interceptionPoint.X, Y = interceptionPoint.Y };
                if (point != null && point.X > 0 && point.Y > 0 && point.X < MapDataService.MapData.MapWidth && interceptionPoint.Y < MapDataService.MapData.MapHeight)
                {
                    if ((!commander.UnitCalculation.Unit.IsFlying || !bestTarget.Unit.IsFlying) && (!MapDataService.PathWalkable(point) || MapDataService.MapHeight(point) != MapDataService.MapHeight(enemyPosition)))
                    {
                        // TODO: also check if enemy position is walkable, and if it isn't figure out the best spot to move
                        DebugService.DrawLine(commander.UnitCalculation.Unit.Pos, new Point { X = point.X, Y = point.Y, Z = commander.UnitCalculation.Unit.Pos.Z + 1f }, new Color { B = 0, G = 0, R = 255 });
                        DebugService.DrawLine(bestTarget.Unit.Pos, new Point { X = point.X, Y = point.Y, Z = commander.UnitCalculation.Unit.Pos.Z + 1f }, new Color { B = 0, G = 0, R = 255 });
                        DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = commander.UnitCalculation.Unit.Pos.Z + 1f }, .5f, new Color { B = 0, G = 0, R = 255 });
                        return enemyPosition;
                    }
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(point.X, point.Y)) > 625)
                    {
                        return enemyPosition;
                    }
                    if (Math.Abs(Math.Atan2(commander.UnitCalculation.Vector.X, commander.UnitCalculation.Vector.Y) - Math.Atan2(bestTarget.AverageVector.X, bestTarget.AverageVector.Y)) < .5) // if they're both goign the same direction just go straight at the enemy instead to prevent bug going opposite direction
                    {
                        return enemyPosition;
                    }
                    DebugService.DrawLine(commander.UnitCalculation.Unit.Pos, new Point { X = point.X, Y = point.Y, Z = commander.UnitCalculation.Unit.Pos.Z + 1f }, new Color { B = 255, G = 0, R = 0 });
                    DebugService.DrawLine(bestTarget.Unit.Pos, new Point { X = point.X, Y = point.Y, Z = commander.UnitCalculation.Unit.Pos.Z + 1f }, new Color { B = 255, G = 0, R = 0 });
                    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = commander.UnitCalculation.Unit.Pos.Z + 1f }, .5f, new Color { B = 255, G = 0, R = 0 });
                    return point;
                }
            }

            return enemyPosition;
        }

        protected virtual bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action))
            {
                return true;
            }

            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                action = commander.Order(frame, Abilities.ATTACK, defensivePoint); // no damaging targets in range, retreat towards the defense point
                return true;
            }

            if (bestTarget != null && commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && MicroPriority != MicroPriority.NavigateToLocation)
            {              
                if (GetHighGroundVision(commander, target, defensivePoint, bestTarget, frame, out action)) { return true; }

                var enemyPosition = GetBestTargetAttackPoint(commander, bestTarget);
                if (SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType))
                {
                    action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                    return true;
                }

                action = commander.Order(frame, Abilities.ATTACK, enemyPosition);
                return true;
            }

            if (GroupUpEnabled && GroupUp(commander, target, groupCenter, true, frame, out action))
            {
                return true;
            }

            if (bestTarget != null && MicroPriority != MicroPriority.NavigateToLocation)
            {
                var enemyPosition = GetBestTargetAttackPoint(commander, bestTarget);
                if (SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType))
                {
                    action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                    return true;
                }
                action = commander.Order(frame, Abilities.ATTACK, enemyPosition);
                return true;
            }

            action = commander.Order(frame, Abilities.ATTACK, target); // no damaging targets in range, attack towards the main target
            return true;
        }

        protected virtual bool GroupUp(UnitCommander commander, Point2D target, Point2D groupCenter, bool attack, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.NearbyEnemies.Any() || groupCenter == null)
            {
                return false;
            }

            if (frame > SharkyOptions.FramesPerSecond * 10 * 60 && ActiveUnitData.EnemyUnits.Count() < 10)
            {
                return false; // stop grouping up when searching for the last enemy units to finish the game
            }

            // if not near the center of all the units attacking
            // move toward the center
            var groupUpSmall = false;
            if (commander.UnitCalculation.NearbyAllies.Count < 10 && Vector2.DistanceSquared(new Vector2(groupCenter.X, groupCenter.Y), commander.UnitCalculation.Position) > GroupUpDistanceSmall * GroupUpDistanceSmall && Vector2.DistanceSquared(new Vector2(groupCenter.X, groupCenter.Y), commander.UnitCalculation.Position) < GroupUpDistanceMax * GroupUpDistanceMax)
            {
                groupUpSmall = true;
            }
            if (groupUpSmall || (Vector2.DistanceSquared(new Vector2(groupCenter.X, groupCenter.Y), commander.UnitCalculation.Position) > GroupUpDistance * GroupUpDistance && Vector2.DistanceSquared(new Vector2(groupCenter.X, groupCenter.Y), commander.UnitCalculation.Position) < GroupUpDistanceMax * GroupUpDistanceMax))
            {
                if (!commander.UnitCalculation.Unit.IsFlying && !MapDataService.PathWalkable(groupCenter))
                {
                    return false;
                }

                if (attack)
                {
                    //DebugService.DrawSphere(new Point { X = groupCenter.X, Y = groupCenter.Y, Z = 10 });
                    action = commander.Order(frame, Abilities.ATTACK, groupCenter);
                }
                else
                {
                    action = commander.Order(frame, Abilities.MOVE, groupCenter);
                }

                return true;
            }

            return false;
        }

        protected virtual bool GroupUpBasedOnState(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.CommanderState == CommanderState.Grouping && groupCenter != null) // TODO: maybe not group if in range of enemies
            {
                if (SpecialCaseRetreat(commander, groupCenter, defensivePoint, frame, out action)) { return true; }

                if (WeaponReady(commander, frame))
                {
                    if (AttackBestTargetInRange(commander, groupCenter, bestTarget, frame, out action)) { return true; }
                }
                if ((!commander.UnitCalculation.Unit.IsFlying && MapDataService.PathWalkable(groupCenter)) || 
                    (commander.UnitCalculation.Unit.IsFlying && MapDataService.PathFlyable(commander.UnitCalculation.Unit.Pos, groupCenter)))
                {
                    action = commander.Order(frame, Abilities.MOVE, groupCenter);
                    return true;
                }
            }

            return false;
        }

        protected virtual bool AttackBestTargetInRange(UnitCommander commander, Point2D target, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (bestTarget != null)
            {
                if (commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && bestTarget.Unit.DisplayType == DisplayType.Visible && MapDataService.SelfVisible(bestTarget.Unit.Pos))
                {
                    bestTarget.IncomingDamage += GetDamage(commander.UnitCalculation.Weapons, bestTarget.Unit, bestTarget.UnitTypeData);
                    action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    return true;
                }
            }

            return false;
        }

        protected virtual UnitCalculation GetBestTargetFromList(UnitCommander commander, IEnumerable<UnitCalculation> attacks, UnitOrder existingAttackOrder)
        {
            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.KillDetection)
            {
                var detectingEnemies = attacks.Where(u => SharkyUnitData.DetectionTypes.Contains((UnitTypes)u.Unit.UnitType)).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position));
                if (existingAttackOrder != null)
                {
                    var existing = detectingEnemies.FirstOrDefault(u => u.Unit.Tag == existingAttackOrder.TargetUnitTag);
                    if (existing != null)
                    {
                        return existing;
                    }
                    if (commander.BestTarget != null)
                    {
                        existing = detectingEnemies.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }
                }
                var enemy = detectingEnemies.FirstOrDefault();
                if (enemy != null)
                {
                    return enemy;
                }

                var abilityDetectingEnemies = attacks.Where(u => SharkyUnitData.AbilityDetectionTypes.Contains((UnitTypes)u.Unit.UnitType)).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position));
                if (existingAttackOrder != null)
                {
                    var existing = abilityDetectingEnemies.FirstOrDefault(u => u.Unit.Tag == existingAttackOrder.TargetUnitTag);
                    if (existing != null)
                    {
                        return existing;
                    }
                    if (commander.BestTarget != null)
                    {
                        existing = abilityDetectingEnemies.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }
                }
                enemy = abilityDetectingEnemies.FirstOrDefault();
                if (enemy != null)
                {
                    return enemy;
                }
            }

            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.KillWorkers)
            {
                var scvs = attacks.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position));
                if (existingAttackOrder != null)
                {
                    var existing = scvs.FirstOrDefault(u => u.Unit.Tag == existingAttackOrder.TargetUnitTag);
                    if (existing != null)
                    {
                        return existing;
                    }
                    if (commander.BestTarget != null)
                    {
                        existing = scvs.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }
                }
                var scv = scvs.FirstOrDefault();
                if (scv != null)
                {
                    return scv;
                }
            }
            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.KillBunker)
            {
                var bunkers = attacks.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position));

                if (existingAttackOrder != null)
                {
                    var existing = bunkers.FirstOrDefault(u => u.Unit.Tag == existingAttackOrder.TargetUnitTag);
                    if (existing != null)
                    {
                        return existing;
                    }
                    if (commander.BestTarget != null)
                    {
                        existing = bunkers.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }
                }
                var bunker = bunkers.FirstOrDefault();
                if (bunker != null)
                {
                    return bunker;
                }
            }

            var weapon = UnitDataService.GetWeapon(commander.UnitCalculation.Unit);

            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
            {
                var airAttackers = attacks.Where(u => u.DamageAir && AirAttackersFilter(commander, u));

                if (airAttackers.Count() > 0)
                {
                    var bestDpsReduction = GetBestDpsReduction(commander, weapon, airAttackers, attacks);

                    if (existingAttackOrder != null)
                    {
                        var existingReduction = airAttackers.Where(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag).FirstOrDefault();
                        if (existingReduction == null && commander.BestTarget != null)
                        {
                            existingReduction = airAttackers.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        }
                        if (existingReduction != null)
                        {
                            var existing = existingReduction.Dps / TimeToKill(weapon, existingReduction.Unit, existingReduction.UnitTypeData);
                            var best = bestDpsReduction.Dps / TimeToKill(weapon, bestDpsReduction.Unit, bestDpsReduction.UnitTypeData);
                            if (existing * 1.25 > best)
                            {
                                return existingReduction; // just keep attacking the same unit
                            }
                        }
                    }

                    if (bestDpsReduction != null)
                    {
                        return bestDpsReduction;
                    }
                }
            }
            else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
            {
                var groundAttackers = attacks.Where(u => u.DamageGround && u.Unit.UnitType != (uint)UnitTypes.ZERG_BROODLING && (!u.UnitClassifications.Contains(UnitClassification.Worker) || u.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag)) && GroundAttackersFilter(commander, u));
                if (groundAttackers.Count() > 0)
                {
                    var bestDpsReduction = GetBestDpsReduction(commander, weapon, groundAttackers, attacks);

                    if (existingAttackOrder != null)
                    {
                        var existingReduction = groundAttackers.Where(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag).FirstOrDefault();
                        if (existingReduction == null && commander.BestTarget != null)
                        {
                            existingReduction = groundAttackers.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        }
                        if (existingReduction != null)
                        {
                            var existing = existingReduction.Dps / TimeToKill(weapon, existingReduction.Unit, existingReduction.UnitTypeData);
                            var best = bestDpsReduction.Dps / TimeToKill(weapon, bestDpsReduction.Unit, bestDpsReduction.UnitTypeData);
                            if (existing * 1.25 > best)
                            {
                                return existingReduction; // just keep attacking the same unit
                            }
                        }
                    }

                    if (bestDpsReduction != null)
                    {
                        return bestDpsReduction;
                    }
                }
            }

            var threats = attacks.Where(enemyAttack => enemyAttack.Damage > 0 && DamageService.CanDamage(enemyAttack, commander.UnitCalculation) && enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_BROODLING && (!enemyAttack.UnitClassifications.Contains(UnitClassification.Worker) || enemyAttack.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag)) && GroundAttackersFilter(commander, enemyAttack) && AirAttackersFilter(commander, enemyAttack));
            if (threats.Count() > 0)
            {
                var bestDpsReduction = GetBestDpsReduction(commander, weapon, threats, attacks);
                if (existingAttackOrder != null)
                {
                    var existingReduction = threats.Where(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag).FirstOrDefault();
                    if (commander.BestTarget != null)
                    {
                        var existing = threats.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            existingReduction = existing;
                        }
                    }
                    if (existingReduction != null)
                    {
                        var existing = existingReduction.Dps / TimeToKill(weapon, existingReduction.Unit, existingReduction.UnitTypeData);
                        var best = bestDpsReduction.Dps / TimeToKill(weapon, bestDpsReduction.Unit, bestDpsReduction.UnitTypeData);
                        if (existing * 1.25 > best)
                        {
                            return existingReduction; // just keep attacking the same unit
                        }
                    }
                }
                if (bestDpsReduction != null)
                {
                    return bestDpsReduction;
                }
            }

            var defensiveBuildings = attacks.Where(enemyAttack => enemyAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure) && GroundAttackersFilter(commander, enemyAttack)).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position));
            var defensiveBuilding = defensiveBuildings.FirstOrDefault();
            if (commander.BestTarget != null)
            {
                var existing = defensiveBuildings.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                if (existing != null)
                {
                    defensiveBuilding = existing;
                }
            }
            if (defensiveBuilding != null)
            {
                return defensiveBuilding;
            }

            var workers = attacks.Where(enemyAttack => enemyAttack.UnitClassifications.Contains(UnitClassification.Worker) && GroundAttackersFilter(commander, enemyAttack)).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position));
            if (workers.Count() > 0)
            {
                if (existingAttackOrder != null)
                {
                    var existingReduction = workers.Where(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag).FirstOrDefault();
                    if (commander.BestTarget != null)
                    {
                        var existing = workers.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                        if (existing != null)
                        {
                            existingReduction = existing;
                        }
                    }
                    if (existingReduction != null)
                    {
                        return existingReduction; // just keep attacking the same unit
                    }
                }
                return workers.FirstOrDefault();
            }

            var building = GetBestBuildingTarget(attacks, commander);
            if (building != null)
            {
                return building;
            }

            return null;
        }

        protected UnitCalculation GetBestBuildingTarget(IEnumerable<UnitCalculation> attacks, UnitCommander commander)
        {
            var orderedAttacks = attacks.Where(enemy => enemy.Unit.UnitType != (uint)UnitTypes.ZERG_LARVA && enemy.Unit.UnitType != (uint)UnitTypes.ZERG_BROODLING && enemy.Unit.UnitType != (uint)UnitTypes.ZERG_EGG && GroundAttackersFilter(commander, enemy)).OrderBy(enemy => (UnitTypes)enemy.Unit.UnitType, new UnitTypeTargetPriority()).ThenBy(enemy => PredictedHealth(enemy)).ThenBy(enemy => Vector2.DistanceSquared(enemy.Position, commander.UnitCalculation.Position));
            var pylon = orderedAttacks.FirstOrDefault(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON);
            if (pylon != null)
            {
                return pylon;
            }
            var activeBuilding = orderedAttacks.Where(a => a.Unit.IsActive).FirstOrDefault();
            if (activeBuilding != null && !activeBuilding.UnitClassifications.Contains(UnitClassification.ResourceCenter))
            {
                return activeBuilding;
            }
            else
            {
                if (commander.BestTarget != null)
                {
                    var existing = orderedAttacks.FirstOrDefault(o => o.Unit.Tag == commander.BestTarget.Unit.Tag);
                    if (existing != null)
                    {
                        return existing; // just keep attacking the same unit
                    }
                }

                return orderedAttacks.FirstOrDefault();
            }
        }

        protected virtual UnitCalculation GetBestDpsReduction(UnitCommander commander, Weapon weapon, IEnumerable<UnitCalculation> primaryTargets, IEnumerable<UnitCalculation> secondaryTargets)
        {
            var bestDpsReduction = primaryTargets.OrderByDescending(enemy => enemy.Dps / TimeToKill(weapon, enemy.Unit, enemy.UnitTypeData)).ThenBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();

            return bestDpsReduction;
        }

        protected float TimeToKill(Weapon weapon, Unit unit, UnitTypeData unitTypeData)
        {
            float bonus = 0;
            if (unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD))
            {
                bonus += 100;
            }
            var damage = GetDamage(weapon, unit, unitTypeData);
            return (unit.Health + unit.Shield + bonus) / (damage / weapon.Speed);
        }

        protected virtual float GetDamage(List<Weapon> weapons, Unit unit, UnitTypeData unitTypeData)
        {
            Weapon weapon;
            if (unit.IsFlying || unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS || unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM))
            {
                weapon = weapons.FirstOrDefault(w => w.Type == Weapon.Types.TargetType.Air || w.Type == Weapon.Types.TargetType.Any);
            }
            else
            {
                weapon = weapons.FirstOrDefault(w => w.Type == Weapon.Types.TargetType.Ground || w.Type == Weapon.Types.TargetType.Any);
            }

            return GetDamage(weapon, unit, unitTypeData);
        }

        protected virtual float GetDamage(Weapon weapon, Unit unit, UnitTypeData unitTypeData)
        {
            if (weapon == null || weapon.Damage == 0)
            {
                return 0;
            }
            if ((unit.IsFlying || unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM)) && weapon.Type == Weapon.Types.TargetType.Ground)
            {
                return 0;
            }
            if (!unit.IsFlying && weapon.Type == Weapon.Types.TargetType.Air && unit.UnitType != (uint)UnitTypes.PROTOSS_COLOSSUS)
            {
                return 0;
            }

            float bonusDamage = 0;
            var damageBonus = weapon.DamageBonus.FirstOrDefault(d => unitTypeData.Attributes.Contains(d.Attribute));
            if (damageBonus != null)
            {
                bonusDamage += damageBonus.Bonus;
            }

            float bonusArmor = 0;
            if (unit.BuffIds.Contains(76))
            {
                bonusArmor += 2;
            }

            var damage = (weapon.Damage + bonusDamage) - (unitTypeData.Armor + bonusArmor + unit.ArmorUpgradeLevel + unit.ShieldUpgradeLevel); // TODO: weapon upgrades

            if (damage < 0.5)
            {
                damage = 0.5f;
            }

            return damage * weapon.Attacks;
        }

        protected virtual bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown < 2;
        }

        protected virtual bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        protected virtual bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        protected virtual Formation GetDesiredFormation(UnitCommander commander)
        {
            if (commander.UnitCalculation.Unit.IsFlying)
            {
                if (MapDataService.GetCells(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, 5).Any(e => e.EnemyAirSplashDpsInRange > 0))
                {
                    return Formation.Loose;
                }
                else
                {
                    //var nearbyFlyingAllies = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.IsFlying);
                    //if (nearbyFlyingAllies.Count() > 0 && nearbyFlyingAllies.Count(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position) < 4) < 1)
                    //{
                    //    return Formation.Tight;
                    //}
                    return Formation.Normal;
                }
            }

            var zerglingDps = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING || e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLINGBURROWED).Sum(e => e.Dps);
            var splashDps = commander.UnitCalculation.NearbyEnemies.Where(e => SharkyUnitData.GroundSplashDamagers.Contains((UnitTypes)e.Unit.UnitType)).Sum(e => e.Dps);

            if (zerglingDps > splashDps)
            {
                return Formation.Tight;
            }
            if (splashDps > 0)
            {
                return Formation.Loose;
            }

            return Formation.Normal;
        }

        protected virtual bool WorkerEscapeSurround(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker))
            {
                if (commander.UnitCalculation.EnemiesInRangeOf.Count() > 0)
                {
                    var selfBase = BaseData.SelfBases.FirstOrDefault();
                    if (selfBase != null)
                    {
                        var mineralField = selfBase.MineralFields.FirstOrDefault();
                        if (mineralField != null)
                        {
                            action = commander.Order(frame, Abilities.HARVEST_GATHER, null, mineralField.Tag);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        protected virtual bool AvoidTargettedOneHitKills(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            var attack = commander.UnitCalculation.Attackers.Where(a => a.Damage > commander.UnitCalculation.Unit.Health + commander.UnitCalculation.Unit.Shield).OrderBy(e => (e.Range * e.Range) - Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position)).FirstOrDefault();
            if (attack != null)
            {
                if (commander.UnitCalculation.Unit.IsFlying)
                {
                    var avoidPoint = GetAirAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
                else
                {
                    var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }

            return false;
        }

        protected virtual bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (MicroPriority == MicroPriority.AttackForward && commander.UnitCalculation.Unit.Health > commander.UnitCalculation.Unit.HealthMax / 4.0) { return false; }
            var attack = commander.UnitCalculation.Attackers.OrderBy(e => (e.Range * e.Range) - Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position)).FirstOrDefault();
            if (attack != null)
            {
                if (commander.UnitCalculation.Unit.IsFlying)
                {
                    var avoidPoint = GetAirAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
                else
                {
                    var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }

            return false;
        }

        protected virtual bool AvoidPurificationNovas(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.IsFlying) { return false; }

            var nova = commander.UnitCalculation.NearbyEnemies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED).OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (nova == null && commander.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTOR)
            {
                nova = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED && a.Unit.BuffDurationRemain < a.Unit.BuffDurationMax / 2 && Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position) < 25).OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            }

            if (nova != null)
            {
                var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, nova.Unit.Pos, target, defensivePoint, 5);
                action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                return true;
            }

            return false;
        }

        protected virtual bool AvoidRavagerShots(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            foreach (var effect in SharkyUnitData.Effects)
            {
                if (effect.EffectId == (uint)Effects.CORROSIVEBILE)
                {
                    if (Vector2.DistanceSquared(new Vector2(effect.Pos[0].X, effect.Pos[0].Y), commander.UnitCalculation.Position) < 4)
                    {
                        Point2D avoidPoint;
                        if (commander.UnitCalculation.Unit.IsFlying)
                        {
                            avoidPoint = GetAirAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, new Point { X = effect.Pos[0].X, Y = effect.Pos[0].Y, Z = 1 }, target, defensivePoint, 5);
                        }
                        else
                        {
                            avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, new Point { X = effect.Pos[0].X, Y = effect.Pos[0].Y, Z = 1 }, target, defensivePoint, 5);

                        }
                        action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual bool AvoidStorms(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            foreach (var effect in SharkyUnitData.Effects)
            {
                if (effect.EffectId == (uint)Effects.STORM)
                {
                    if (Vector2.DistanceSquared(new Vector2(effect.Pos[0].X, effect.Pos[0].Y), commander.UnitCalculation.Position) < 8)
                    {
                        Point2D avoidPoint;
                        if (commander.UnitCalculation.Unit.IsFlying)
                        {
                            avoidPoint = GetAirAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, new Point { X = effect.Pos[0].X, Y = effect.Pos[0].Y, Z = 1 }, target, defensivePoint, 5);
                        }
                        else
                        {
                            avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, new Point { X = effect.Pos[0].X, Y = effect.Pos[0].Y, Z = 1 }, target, defensivePoint, 5);

                        }
                        action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual bool AvoidReaperCharges(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.IsFlying) { return false; }

            var charge = commander.UnitCalculation.NearbyEnemies.Where(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_KD8CHARGE && Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position) < 12).OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (charge == null)
            {
                charge = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_KD8CHARGE && Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position) < 12).OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            }

            if (charge != null)
            {
                var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, charge.Unit.Pos, target, defensivePoint, 5);
                action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                return true;
            }

            return false;
        }

        protected virtual bool DealWithCyclones(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat) { return false; }

            var lockOnRange = 7;
            var enemyCyclones = commander.UnitCalculation.NearbyEnemies.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_CYCLONE && InRange(commander.UnitCalculation.Position, u.Position, commander.UnitCalculation.Unit.Radius + lockOnRange));
            if (enemyCyclones.Count() > 0 && MicroPriority != MicroPriority.StayOutOfRange && (commander.UnitCalculation.TargetPriorityCalculation.AirWinnability > 1 || commander.UnitCalculation.TargetPriorityCalculation.GroundWinnability > 1))
            {
                var cycloneDps = enemyCyclones.Sum(e => e.Dps);
                var otherDps = commander.UnitCalculation.NearbyEnemies.Where(u => u.Unit.UnitType != (uint)UnitTypes.TERRAN_CYCLONE && InRange(commander.UnitCalculation.Position, u.Position, commander.UnitCalculation.Unit.Radius + lockOnRange)).Sum(e => e.Dps);

                if (cycloneDps > otherDps)
                {
                    var closestCyclone = enemyCyclones.OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position)).FirstOrDefault();
                    action = commander.Order(frame, Abilities.ATTACK, null, closestCyclone.Unit.Tag);
                    return true;
                }
            }

            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.LOCKON))
            {
                if (Retreat(commander, defensivePoint, defensivePoint, frame, out action)) { return true; }
            }

            return false;
        }

        protected virtual bool DealWithSiegedTanks(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.IsFlying || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || !MapDataService.InEnemyVision(commander.UnitCalculation.Unit.Pos))
            {
                return false;
            }

            if (WeaponReady(commander, frame)) { return false; }

            var siegeRange = 13; // 13 siege mode range, can't shoot within 2 range
            var enemySiegedTanks = commander.UnitCalculation.EnemiesInRangeOf.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED);

            var siegedCount = enemySiegedTanks.Count();
            if (siegedCount == 0)
            {
                return false;
            }

            var estimatedSiegeDps = siegedCount * 75;

            var otherEnemyAttacks = commander.UnitCalculation.EnemiesInRangeOf.Where(u => u.Unit.UnitType != (uint)UnitTypes.TERRAN_SIEGETANKSIEGED);
            var otherDps = otherEnemyAttacks.Sum(e => e.Dps);

            if (estimatedSiegeDps > otherDps)
            {
                var closestSiegePosition = enemySiegedTanks.OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                action = commander.Order(frame, Abilities.MOVE, new Point2D { X = closestSiegePosition.Unit.Pos.X, Y = closestSiegePosition.Unit.Pos.Y });
                return true;
            }

            return false;
        }

        protected virtual Point2D GetPositionFromRange(UnitCommander commander, Point target, Point position, float range)
        {
            var angle = Math.Atan2(target.Y - position.Y, position.X - target.X);
            var x = range * Math.Cos(angle);
            var y = range * Math.Sin(angle);
            return new Point2D { X = target.X + (float)x, Y = target.Y - (float)y };
        }

        protected virtual Point2D GetGroundAvoidPoint(UnitCommander commander, Point start, Point avoid, Point2D target, Point2D defensivePoint, float range)
        {
            var avoidPoint = GetPositionFromRange(commander, avoid, start, range);
            if (!MapDataService.PathWalkable(start, avoidPoint))
            {
                if (Vector2.DistanceSquared(new Vector2(avoidPoint.X, avoidPoint.Y), new Vector2(target.X, target.Y)) < Vector2.DistanceSquared(new Vector2(avoidPoint.X, avoidPoint.Y), new Vector2(defensivePoint.X, defensivePoint.Y)))
                {
                    avoidPoint = target;
                }
                else
                {
                    avoidPoint = defensivePoint;
                }
            }
            return avoidPoint;
        }

        protected virtual Point2D GetAirAvoidPoint(UnitCommander commander, Point start, Point avoid, Point2D target, Point2D defensivePoint, float range)
        {
            var avoidPoint = GetPositionFromRange(commander, avoid, start, range);
            if (!MapDataService.PathFlyable(start, avoidPoint))
            {
                if (Vector2.DistanceSquared(new Vector2(avoidPoint.X, avoidPoint.Y), new Vector2(target.X, target.Y)) < Vector2.DistanceSquared(new Vector2(avoidPoint.X, avoidPoint.Y), new Vector2(defensivePoint.X, defensivePoint.Y)))
                {
                    avoidPoint = target;
                }
                else
                {
                    avoidPoint = defensivePoint;
                }
            }
            return avoidPoint;
        }

        protected virtual bool GroundAttackersFilter(UnitCommander commander, UnitCalculation enemyAttack)
        {
            if (PredictedHealth(enemyAttack) <= 0)
            {
                return false;
            }

            return true;
        }

        protected virtual bool AttackersFilter(UnitCommander commander, UnitCalculation enemyAttack)
        {
            if (PredictedHealth(enemyAttack) <= 0)
            {
                return false;
            }
            return true;
        }

        protected virtual bool AirAttackersFilter(UnitCommander commander, UnitCalculation enemyAttack)
        {
            if (PredictedHealth(enemyAttack) <= 0)
            {
                return false;
            }

            return true;
        }

        protected bool InRange(Vector2 targetLocation, Vector2 unitLocation, float range)
        {
            return Vector2.DistanceSquared(targetLocation, unitLocation) <= (range * range);
        }

        protected virtual bool Detected(UnitCommander commander)
        {
            foreach (var scan in SharkyUnitData.Effects.Where(e => e.EffectId == (uint)Effects.SCAN))
            {
                if (InRange(new Vector2(scan.Pos[0].X, scan.Pos[0].Y), commander.UnitCalculation.Position, scan.Radius + commander.UnitCalculation.Unit.Radius + 1))
                {
                    return true;
                }
            }

            if (commander.UnitCalculation.Unit.Health + commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.PreviousUnit.Health + commander.UnitCalculation.PreviousUnit.Shield)
            {
                return true; // if getting attacked must be detected (unless it's by splash damage)
            }

            // TODO: get range of detection for units, calculate if this unit is in within detection range
            return commander.UnitCalculation.NearbyEnemies.Any(e => SharkyUnitData.DetectionTypes.Contains((UnitTypes)e.Unit.UnitType)) || commander.UnitCalculation.Unit.BuffIds.Any(b => b == (uint)Buffs.ORACLEREVELATION || b == (uint)Buffs.FUNGALGROWTH || b == (uint)Buffs.EMPDECLOAK);
        }

        public virtual List<SC2APIProtocol.Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            var unitToSupport = GetSupportTarget(commander, supportTargets, target, defensivePoint);

            if (unitToSupport == null)
            {
                return Attack(commander, target, defensivePoint, groupCenter, frame);
            }

            var supportPoint = GetSupportSpot(commander, unitToSupport, target, defensivePoint);

            var formation = GetDesiredFormation(commander);
            var bestTarget = GetBestTarget(commander, unitToSupport, supportPoint, frame);

            if (SpecialCaseMove(commander, supportPoint, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            if (PreOffenseOrder(commander, supportPoint, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (AvoidTargettedOneHitKills(commander, supportPoint, defensivePoint, frame, out action)) { return action; }

            if (OffensiveAbility(commander, supportPoint, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (MicroPriority == MicroPriority.StayOutOfRange)
            {
                if (SpecialCaseRetreat(commander, supportPoint, defensivePoint, frame, out action)) { return action; }
                if (MoveAway(commander, supportPoint, defensivePoint, frame, out action)) { return action; }
            }

            if (WeaponReady(commander, frame))
            {
                // don't initiate the attack, just defend yourself and the support target
                if (commander.UnitCalculation.EnemiesInRange.Count() > 0 || commander.UnitCalculation.EnemiesInRangeOf.Count() > 0 || unitToSupport.UnitCalculation.EnemiesInRangeOf.Count() > 0)
                {
                    if (AttackBestTarget(commander, supportPoint, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }
                }
            }

            if (Move(commander, supportPoint, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            return commander.Order(frame, Abilities.ATTACK, supportPoint);
        }

        protected virtual UnitCommander GetSupportTarget(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint)
        {
            // support targets are ordered before they're passed in
            return supportTargets.FirstOrDefault();

            if (supportTargets == null)
            {
                return null;
            }

            // out of nearby allies within 15 range
            // select the friendlies with enemies in 15 range
            // order by closest to the enemy
            var friendlies = supportTargets.Where(c => Vector2.DistanceSquared(c.UnitCalculation.Position, commander.UnitCalculation.Position) < 225
                    && c.UnitCalculation.NearbyEnemies.Any(e => Vector2.DistanceSquared(c.UnitCalculation.Position, e.Position) < 225)
                ).OrderBy(u => Vector2.DistanceSquared(u.UnitCalculation.NearbyEnemies.OrderBy(e => Vector2.DistanceSquared(e.Position, u.UnitCalculation.Position)).First().Position, u.UnitCalculation.Position));

            if (friendlies.Count() > 0)
            {
                return friendlies.First();
            }

            // if none
            // get any allies
            // select the friendies with enemies in 15 range
            // order by closest to the enemy
            friendlies = supportTargets.Where(u => u.UnitCalculation.NearbyEnemies.Any(e => Vector2.DistanceSquared(u.UnitCalculation.Position, e.Position) < 225)).OrderBy(u => Vector2.DistanceSquared(u.UnitCalculation.NearbyEnemies.OrderBy(e => Vector2.DistanceSquared(e.Position, u.UnitCalculation.Position)).First().Position, u.UnitCalculation.Position));

            if (friendlies.Count() > 0)
            {
                return friendlies.First();
            }

            // if still none
            //get ally closest to target
            friendlies = supportTargets.OrderBy(u => Vector2.DistanceSquared(u.UnitCalculation.Position, new Vector2(target.X, target.Y)));

            if (friendlies.Count() > 0)
            {
                return friendlies.First();
            }

            return null;
        }

        protected virtual Point2D GetSupportSpot(UnitCommander commander, UnitCommander unitToSupport, Point2D target, Point2D defensivePoint)
        {
            if (commander.UnitCalculation.Range < unitToSupport.UnitCalculation.Range)
            {
                return new Point2D { X = unitToSupport.UnitCalculation.Position.X, Y = unitToSupport.UnitCalculation.Position.Y };
            }

            var angle = Math.Atan2(unitToSupport.UnitCalculation.Position.Y - defensivePoint.Y, defensivePoint.X - unitToSupport.UnitCalculation.Position.X);
            var x = commander.UnitCalculation.Range * Math.Cos(angle);
            var y = commander.UnitCalculation.Range * Math.Sin(angle);

            var supportPoint = new Point2D { X = unitToSupport.UnitCalculation.Position.X + (float)x, Y = unitToSupport.UnitCalculation.Position.Y - (float)y };
            if (MapDataService.MapHeight(supportPoint) != MapDataService.MapHeight(unitToSupport.UnitCalculation.Unit.Pos))
            {
                supportPoint = new Point2D { X = unitToSupport.UnitCalculation.Position.X, Y = unitToSupport.UnitCalculation.Position.Y };
            }

            return supportPoint;
        }

        protected virtual UnitCalculation GetBestTarget(UnitCommander commander, UnitCommander unitToSupport, Point2D target, int frame)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            var range = commander.UnitCalculation.Range;

            var attacks = new List<UnitCalculation>(commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType == DisplayType.Visible && AttackersFilter(commander, u))); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Count > 0)
            {
                var oneShotKills = attacks.Where(a => PredictedHealth(a) < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData) && !a.Unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD));
                if (oneShotKills.Count() > 0)
                {
                    if (existingAttackOrder != null)
                    {
                        var existing = oneShotKills.FirstOrDefault(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }

                    var oneShotKill = GetBestTargetFromList(commander, oneShotKills, existingAttackOrder);
                    if (oneShotKill != null)
                    {
                        commander.BestTarget = oneShotKill;
                        return oneShotKill;
                    }
                    else
                    {
                        commander.BestTarget = oneShotKills.OrderBy(o => o.Dps).FirstOrDefault();
                        return commander.BestTarget;
                    }
                }

                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null && (bestAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || (bestAttack.UnitClassifications.Contains(UnitClassification.Worker) && bestAttack.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag))))
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_TEMPEST)
            {
                range = 10;
            }

            attacks = new List<UnitCalculation>();
            foreach (var enemyAttack in unitToSupport.UnitCalculation.EnemiesInRangeOf)
            {
                if (enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation, enemyAttack) && !InRange(enemyAttack.Position, commander.UnitCalculation.Position, range + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius) && AttackersFilter(commander, enemyAttack))
                {
                    attacks.Add(enemyAttack);
                }
            }
            if (attacks.Count > 0)
            {
                var bestOutOfRangeAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestOutOfRangeAttack != null && (bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
                {
                    commander.BestTarget = bestOutOfRangeAttack;
                    return bestOutOfRangeAttack;
                }
                if (bestAttack == null)
                {
                    bestAttack = bestOutOfRangeAttack;
                }
            }

            attacks = new List<UnitCalculation>();
            foreach (var enemyAttack in commander.UnitCalculation.EnemiesInRangeOf)
            {
                if (enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation, enemyAttack) && !InRange(enemyAttack.Position, commander.UnitCalculation.Position, range + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius) && AttackersFilter(commander, enemyAttack))
                {
                    attacks.Add(enemyAttack);
                }
            }
            if (attacks.Count > 0)
            {
                var bestOutOfRangeAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestOutOfRangeAttack != null && (bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
                {
                    commander.BestTarget = bestOutOfRangeAttack;
                    return bestOutOfRangeAttack;
                }
                if (bestAttack == null)
                {
                    bestAttack = bestOutOfRangeAttack;
                }
            }

            commander.BestTarget = bestAttack;
            return bestAttack;
        }

        public virtual List<SC2APIProtocol.Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            var bestTarget = GetBestHarassTarget(commander, target);

            if (SpecialCaseMove(commander, target, defensivePoint, null, bestTarget, Formation.Normal, frame, out action)) { return action; }
            if (PreOffenseOrder(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
            if (AvoidTargettedOneHitKills(commander, target, defensivePoint, frame, out action)) { return action; }
            if (OffensiveAbility(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }

            if (WeaponReady(commander, frame))
            {
                if (AttackBestTarget(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
            }

            var formation = GetDesiredFormation(commander);
            if (Move(commander, target, defensivePoint, null, bestTarget, formation, frame, out action)) { return action; }

            return commander.Order(frame, Abilities.MOVE, target);
        }

        protected virtual UnitCalculation GetBestHarassTarget(UnitCommander commander, Point2D target)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            var range = commander.UnitCalculation.Range;

            var attacks = new List<UnitCalculation>(commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType != DisplayType.Hidden && u.UnitClassifications.Contains(UnitClassification.Worker) && AttackersFilter(commander, u))); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Count > 0)
            {
                var oneShotKills = attacks.Where(a => PredictedHealth(a) < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData));
                if (oneShotKills.Count() > 0)
                {
                    if (existingAttackOrder != null)
                    {
                        var existing = oneShotKills.FirstOrDefault(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }

                    var oneShotKill = GetBestTargetFromList(commander, oneShotKills, existingAttackOrder);
                    if (oneShotKill != null)
                    {
                        return oneShotKill;
                    }
                    else
                    {
                        commander.BestTarget = oneShotKills.OrderBy(o => o.Dps).FirstOrDefault();
                        return commander.BestTarget;
                    }
                }

                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null && bestAttack.UnitClassifications.Contains(UnitClassification.Worker) && bestAttack.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag))
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = new List<UnitCalculation>(); // nearby units not in range right now
            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (enemyAttack.Unit.DisplayType != DisplayType.Hidden && enemyAttack.UnitClassifications.Contains(UnitClassification.Worker) && !InRange(enemyAttack.Position, commander.UnitCalculation.Position, range + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius) && AttackersFilter(commander, enemyAttack))
                {
                    attacks.Add(enemyAttack);
                }
            }
            if (attacks.Count > 0)
            {
                var bestOutOfRangeAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestOutOfRangeAttack != null && (bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestOutOfRangeAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure)))
                {
                    commander.BestTarget = bestOutOfRangeAttack;
                    return bestOutOfRangeAttack;
                }
                if (bestAttack == null)
                {
                    bestAttack = bestOutOfRangeAttack;
                }
            }

            commander.BestTarget = bestAttack;
            return bestAttack;
        }

        public virtual List<SC2APIProtocol.Action> NavigateToPoint(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, null, Formation.Normal, frame, out action)) { return action; }

            if (commander.UnitCalculation.NearbyEnemies.Any(e => DamageService.CanDamage(e, commander.UnitCalculation)))
            {
                if (commander.RetreatPathFrame + 20 < frame)
                {
                    if (commander.UnitCalculation.Unit.IsFlying)
                    {
                        commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, target.X, target.Y, frame);
                        commander.RetreatPathFrame = frame;
                    }
                    else
                    {
                        commander.RetreatPath = SharkyPathFinder.GetSafeGroundPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, target.X, target.Y, frame);
                        commander.RetreatPathFrame = frame;
                    }
                }

                if (FollowPath(commander, frame, out action)) { return action; }
            }

            if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            NavigateToTarget(commander, target, groupCenter, null, Formation.Normal, frame, out action);

            return action;
        }

        protected Vector2 CalculateInterceptionPoint(Vector2 targetPosition, Vector2 targetVelocity, Vector2 interceptorPosition, float interceptorSpeed)
        {
            var totarget = targetPosition - interceptorPosition;

            var a = Vector2.Dot(targetVelocity, targetVelocity) - (interceptorSpeed * interceptorSpeed);
            var b = 2 * Vector2.Dot(targetVelocity, totarget);
            var c = Vector2.Dot(totarget, totarget);

            var p = -b / (2 * a);
            var q = (float)Math.Sqrt((b * b) - 4 * a * c) / (2 * a);

            var t1 = p - q;
            var t2 = p + q;
            float t;

            if (t1 > t2 && t2 > 0)
            {
                t = t2;
            }
            else
            {
                t = t1;
            }

            return  targetPosition + targetVelocity * t;
        }

        protected virtual void UpdateState(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame)
        {

        }
    }
}
