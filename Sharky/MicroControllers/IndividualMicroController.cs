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
        protected EnemyData EnemyData;
        protected SharkyOptions SharkyOptions;
        protected DamageService DamageService;
        protected UnitDataService UnitDataService;
        protected TargetingData TargetingData;
        protected TargetingService TargetingService;
        public AttackPathingService AttackPathingService;
        protected AttackData AttackData;
        protected ChatService ChatService;
        protected TagService TagService;
        protected CameraManager CameraManager;

        public MicroPriority MicroPriority { get; set; }

        public bool GroupUpEnabled { get; set; }
        public bool IgnoreDistractions { get; set; }
        public bool TargetEnemyMainFirst { get; set; }
        public bool AlwaysUsePathing { get; set; }
        public HashSet<UnitTypes> AvoidedUnitTypes { get; set; } = new HashSet<UnitTypes>();

        protected float GroupUpDistanceSmall;
        protected float GroupUpDistance;
        protected float GroupUpDistanceMax;
        protected float AvoidDamageDistance;
        protected float LooseFormationDistance;
        protected float GroupUpStateDistanceSquared;
        protected float MaximumSupportDistanceSqaured;

        public IndividualMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder pathFinder, MicroPriority microPriority, bool groupUpEnabled, float avoidDamageDistance = 2f, bool ignoreDistractions = true)
        {
            MapDataService = defaultSharkyBot.MapDataService;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            DebugService = defaultSharkyBot.DebugService;
            BaseData = defaultSharkyBot.BaseData;
            EnemyData = defaultSharkyBot.EnemyData;
            TargetingData = defaultSharkyBot.TargetingData;
            AttackData = defaultSharkyBot.AttackData;
            CameraManager = defaultSharkyBot.CameraManager;

            SharkyOptions = defaultSharkyBot.SharkyOptions;

            SharkyPathFinder = pathFinder;
            MicroPriority = microPriority;
            GroupUpEnabled = groupUpEnabled;

            DamageService = defaultSharkyBot.DamageService;
            UnitDataService = defaultSharkyBot.UnitDataService;
            TargetingService = defaultSharkyBot.TargetingService;
            AttackPathingService = defaultSharkyBot.AttackPathingService;
            ChatService = defaultSharkyBot.ChatService;
            TagService = defaultSharkyBot.TagService;

            GroupUpDistanceSmall = 5;
            GroupUpDistance = 10;
            GroupUpDistanceMax = 50;
            AvoidDamageDistance = avoidDamageDistance;
            LooseFormationDistance = 1.75f;
            GroupUpStateDistanceSquared = 100f;
            IgnoreDistractions = ignoreDistractions;
            MaximumSupportDistanceSqaured = 225f;
        }

        public virtual List<SC2Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            if (commander.UnitCalculation.Unit.IsOnScreen)
            {
                var breakpoint = true;
            }

            List<SC2Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }

            var formation = GetDesiredFormation(commander);
            var bestTarget = GetBestTarget(commander, target, frame);

            UpdateState(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame);

            if (ContinueInRangeAttack(commander, frame, out action)) { return action; }

            if (GroupUpBasedOnState(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            if (PreOffenseOrder(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (AvoidTargettedOneHitKills(commander, target, defensivePoint, frame, out action)) { return action; }

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (ShouldStayOutOfRange(commander, frame))
            {
                return AttackStayOutOfRange(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame);
            }

            if (TargetEnemyMainFirst && MapDataService.LastFrameVisibility(TargetingData.EnemyMainBasePoint) < 5 * 60 * SharkyOptions.FramesPerSecond)
            {
                target = TargetingData.EnemyMainBasePoint;
            }

            if (GetHighGroundVision(commander, target, defensivePoint, bestTarget, frame, out action)) { return action; }
            if (AvoidPointlessDamage(commander, target, defensivePoint, formation, frame, out action)) { return action; }

            if (WeaponReady(commander, frame))
            {
                if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action)) { return action; }
                if (GetInFormation(commander, formation, frame, out action)) { return action; }
                if (AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }
            }

            if (MoveToAttackOnCooldown(commander, bestTarget, target, defensivePoint, frame, out action)) { return action; }

            if (Move(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            if (AvoidDeceleration(commander, target, true, frame, out action)) { return action; }
            return commander.Order(frame, Abilities.ATTACK, target);
        }

        public virtual bool MoveToAttackOnCooldown(UnitCommander commander, UnitCalculation bestTarget, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (bestTarget == null || bestTarget.FrameLastSeen != frame)
            {
                return false;
            }

            if (!commander.UnitCalculation.Unit.IsFlying && MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) != MapDataService.MapHeight(bestTarget.Unit.Pos))
            {
                return false;
            }

            if (!WeaponReady(commander, frame) && AvoidTargettedDamage(commander, target, defensivePoint, frame, out action)) 
            { 
                return true; 
            }

            if (commander.UnitCalculation.Unit.ShieldMax > 0 && commander.UnitCalculation.Unit.Shield < 25 && AvoidDamage(commander, target, defensivePoint, frame, out action)) // TODO: this only works for protoss, if we want it to work for zerg and terran it needs to change
            {
                return true;
            }

            if (AttackIfCooldownDistanceClose(commander, bestTarget, target, defensivePoint, frame, out action))
            {
                return true;
            }

            return false;
        }

        protected virtual bool AttackIfCooldownDistanceClose(UnitCommander commander, UnitCalculation bestTarget, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var rangeDistance = commander.UnitCalculation.Range + commander.UnitCalculation.Unit.Radius + bestTarget.Unit.Radius;
            var actualDistance = Vector2.Distance(commander.UnitCalculation.Position, bestTarget.Position);
            var gap = actualDistance - rangeDistance;
            var framesToRange = gap / (GetMovementSpeed(commander) / SharkyOptions.FramesPerSecond);
            var framesToShoot = commander.UnitCalculation.Unit.WeaponCooldown - 1;
            if (framesToRange >= framesToShoot && commander.UnitCalculation.Unit.Tag != bestTarget.Unit.Tag && bestTarget.FrameLastSeen == frame)
            {
                DebugService.DrawLine(commander.UnitCalculation.Unit.Pos, bestTarget.Unit.Pos, new Color { R = 250, B = 1, G = 250 });
                action = commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                return true;
            }

            return false;
        }

        public virtual float GetMovementSpeed(UnitCommander commander)
        {
            return commander.UnitCalculation.UnitTypeData.MovementSpeed * 1.4f; // always multiply by 1.4f because of gamespeed time conversion
        }

        /// <summary>
        /// number of cooldown frames the weapon used on this enemy has for it's cooldown (not current cooldown commander.UnitCalculation.Unit.WeaponCooldown)
        /// </summary>
        protected virtual float GetWeaponCooldown(UnitCommander commander, UnitCalculation enemy)
        {
            var weapon = GetWeaponUsed(commander, enemy);
            if (weapon == null)
            {
                return 0;
            }
            return SharkyOptions.FramesPerSecond * (weapon.Speed / 1.4f); //always divide by 1.4f because of gamespeed time conversion
        }

        protected Weapon GetWeaponUsed(UnitCommander commander, UnitCalculation enemy)
        {
            Weapon weapon;

            if (enemy.Unit.IsFlying || enemy.Unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS || enemy.Unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM))
            {
                weapon = commander.UnitCalculation.Weapons.FirstOrDefault(w => w.Type == Weapon.Types.TargetType.Air || w.Type == Weapon.Types.TargetType.Any);
            }
            else
            {
                weapon = commander.UnitCalculation.Weapons.FirstOrDefault(w => w.Type == Weapon.Types.TargetType.Ground || w.Type == Weapon.Types.TargetType.Any);
            }

            return weapon;
        }

        public virtual List<SC2APIProtocol.Action> AttackStayOutOfRange(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }

            if (WeaponReady(commander, frame))
            {
                if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action)) { return action; }
            }

            if (SpecialCaseRetreat(commander, target, defensivePoint, frame, out action)) { return action; }
            if (MoveAway(commander, target, defensivePoint, frame, out action)) { return action; }

            if (AvoidPointlessDamage(commander, target, defensivePoint, formation, frame, out action)) { return action; }

            if (WeaponReady(commander, frame) && !commander.UnitCalculation.EnemiesThreateningDamage.Any() && !commander.UnitCalculation.EnemiesInRangeOfAvoid.Any())
            {
                var safe = true;
                var closestEnemy = commander.UnitCalculation.NearbyEnemies.OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                if (closestEnemy != null && !closestEnemy.UnitClassifications.Contains(UnitClassification.Worker))
                {
                    if (DamageService.CanDamage(closestEnemy, commander.UnitCalculation) && (closestEnemy.Range >= commander.UnitCalculation.Range || closestEnemy.UnitTypeData.MovementSpeed > commander.UnitCalculation.UnitTypeData.MovementSpeed))
                    {
                        safe = false;
                    }
                }
                if (safe && AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }
            }

            if (AvoidAllDamage(commander, target, defensivePoint, frame, out action)) { return action; }
            if (Move(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            if (AvoidDeceleration(commander, target, true, frame, out action)) { return action; }
            return commander.Order(frame, Abilities.ATTACK, target);
        }

        public virtual List<SC2APIProtocol.Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }
            UpdateState(commander, defensivePoint, defensivePoint, null, null, Formation.Normal, frame);
            if (GetInBunker(commander, defensivePoint, frame, out action)) { return action; }
            if (RechargeShieldsAtBattery(commander, defensivePoint, defensivePoint, frame, out action)) { return action; }
            if (HoldStillForRepair(commander, frame, out action)) { return action; }
            if (AttackUnitsMarkedForDeath(commander, frame, out action)) { return action; }
            if (AvoidDeceleration(commander, defensivePoint, true, frame, out action)) { return action; }
            return action;
        }

        public virtual List<SC2APIProtocol.Action> Scout(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool prioritizeVision = false, bool attack = true)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }
            UpdateState(commander, target, defensivePoint, null, null, Formation.Normal, frame);

            if (attack && (!prioritizeVision || MapDataService.SelfVisible(target)))
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
                return MoveToTarget(commander, target, frame);
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
                return MoveToTarget(commander, target, frame);
            }
            else
            {
                // TODO: circle around base
                return Bait(commander, target, defensivePoint, null, frame);
            }
        }

        public virtual List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }
            UpdateState(commander, defensivePoint, defensivePoint, groupCenter, null, Formation.Normal, frame);

            var bestTarget = GetBestTarget(commander, defensivePoint, frame);

            if (ContinueInRangeAttack(commander, frame, out action)) { return action; }

            if (PreOffenseOrder(commander, defensivePoint, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (AvoidTargettedOneHitKills(commander, defensivePoint, defensivePoint, frame, out action)) { return action; }

            if (OffensiveAbility(commander, defensivePoint, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (DoFreeDamage(commander, defensivePoint, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            // TODO: setup a concave above the ramp if there is a ramp, get earch grid point on high ground, make sure at least one unit on each point
            if (commander.UnitCalculation.NearbyEnemies.Any() || Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(defensivePoint.X, defensivePoint.Y)) > 25 || MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) < MapDataService.MapHeight(defensivePoint))
            {
                if (Retreat(commander, defensivePoint, defensivePoint, frame, out action)) { return action; }
                return MoveToTarget(commander, defensivePoint, frame);
            }
            else
            {
                return Idle(commander, defensivePoint, frame);
            }
        }

        protected virtual bool DefendBehindWall(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Range < 3 || !commander.UnitCalculation.NearbyEnemies.Any() || !WeaponReady(commander, frame)) { return false; }

            var closestWall = commander.UnitCalculation.NearbyAllies.Where(a => TargetingData.WallBuildings.Any(w => w.Unit.Tag == a.Unit.Tag)).OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closestWall != null)
            {
                var distanceToDefensivePoint = Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(defensivePoint.X, defensivePoint.Y));
                var wallDistanceToDefensivePoint = Vector2.DistanceSquared(closestWall.Position, new Vector2(defensivePoint.X, defensivePoint.Y));
                if (wallDistanceToDefensivePoint > distanceToDefensivePoint + commander.UnitCalculation.Unit.Radius + commander.UnitCalculation.Unit.Radius + closestWall.Unit.Radius + closestWall.Unit.Radius + 1)
                {
                    var closestEnemy = commander.UnitCalculation.NearbyEnemies.OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                    if (closestEnemy != null)
                    {
                        var distanceToEnemy = Vector2.DistanceSquared(commander.UnitCalculation.Position, closestEnemy.Position);
                        var enemyDistanceToWall = Vector2.DistanceSquared(closestEnemy.Position, closestWall.Position);

                        if (distanceToEnemy > enemyDistanceToWall)
                        {
                            var bestTarget = GetBestTarget(commander, defensivePoint, frame);
                            if (AttackBestTarget(commander, target, defensivePoint, target, bestTarget, frame, out action))
                            {
                                return true;
                            }
                            else
                            {
                                action = commander.Order(frame, Abilities.ATTACK, new Point2D { X = closestWall.Position.X, Y = closestWall.Position.Y });
                            }
                        }
                    }
                }
            }

            return false;
        }

        protected virtual bool DoFreeDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.UnitCalculation.EnemiesThreateningDamage.Any())
            {
                if (!commander.UnitCalculation.NearbyEnemies.Any(e => DamageService.CanDamage(e, commander.UnitCalculation) && (e.UnitTypeData.MovementSpeed >= commander.UnitCalculation.UnitTypeData.MovementSpeed || e.Range >= commander.UnitCalculation.Range)))
                {
                    if (commander.UnitCalculation.NearbyEnemies.Any(e => DamageService.CanDamage(commander.UnitCalculation, e) && e.FrameLastSeen == frame && e.Unit.DisplayType == DisplayType.Visible))
                    {
                        if (AttackBestTarget(commander, target, defensivePoint, target, bestTarget, frame, out action))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public virtual List<SC2APIProtocol.Action> Bait(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }
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

            if (AvoidDeceleration(commander, target, true, frame, out action)) { return action; }

            return commander.Order(frame, Abilities.ATTACK, target);
        }

        public virtual bool Move(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat))
            {
                if (Retreat(commander, target, defensivePoint, frame, out action)) { return true; }
            }

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return true; }

            if (!(formation == Formation.Loose && commander.UnitCalculation.NearbyAllies.Count > 5))
            {
                if (MoveAway(commander, target, defensivePoint, frame, out action)) { return true; }
            }

            return NavigateToTarget(commander, target, groupCenter, bestTarget, formation, frame, out action);
        }

        public virtual bool SpecialCaseMove(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (GetUnstuck(commander, target, defensivePoint, frame, out action)) { return true; }
            if (DealWithInterferenceMatrix(commander, target, defensivePoint, frame, out action)) { return true; }
            if (DealWithParasiticBomb(commander, target, defensivePoint, frame, out action)) { return true; }
            if (AvoidPurificationNovas(commander, target, defensivePoint, frame, out action)) { return true; }

            if (AvoidRavagerShots(commander, target, defensivePoint, frame, out action)) { return true; }

            if (AvoidStorms(commander, target, defensivePoint, frame, out action)) { return true; }

            if (AvoidLiberationZones(commander, target, defensivePoint, frame, out action)) { return true; }

            if (DealWithCyclones(commander, target, defensivePoint, frame, out action)) { return true; }

            if (DealWithSiegedTanks(commander, target, defensivePoint, frame, out action)) { return true; }

            if (AvoidReaperCharges(commander, target, defensivePoint, frame, out action)) { return true; }

            if (RechargeShieldsAtBattery(commander, target, defensivePoint, frame, out action)) { return true; }

            if (HoldStillForRepair(commander, frame, out action)) { return true; }

            if (commander.UnitCalculation.NearbyEnemies.Any() && GetInBunker(commander, target, frame, out action)) { return true; }

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

        private bool GetUnstuck(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.CommanderState != CommanderState.Stuck) { return false; }

            var loader = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.CargoSpaceMax > 0).OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (loader != null)
            {
                action = commander.Order(frame, Abilities.SMART, targetTag: loader.Unit.Tag, allowSpam: true);
                return true;
            }
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

        public virtual bool GetHighGroundVision(UnitCommander commander, Point2D target, Point2D defensivePoint, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (ShouldStayOutOfRange(commander, frame) || MicroPriority == MicroPriority.JustLive || !commander.UnitCalculation.NearbyAllies.Any()) { return false; }
            if (!commander.UnitCalculation.Unit.IsFlying && commander.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_COLOSSUS)
            {
                if (bestTarget != null && !MapDataService.SelfVisible(bestTarget.Unit.Pos) && MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) != MapDataService.MapHeight(bestTarget.Unit.Pos))
                {
                    var badChokes = TargetingData.ChokePoints.Bad.Where(b => Vector2.DistanceSquared(b.Center, bestTarget.Position) < 100 || Vector2.DistanceSquared(b.Center, commander.UnitCalculation.Position) < 100);
                    if (badChokes.Any())
                    {
                        var chokePoint = badChokes.OrderBy(b => Vector2.DistanceSquared(b.Center, target.ToVector2())).First();
                        var distanceToBestTarget = Vector2.DistanceSquared(bestTarget.Position, commander.UnitCalculation.Position);
                        if (distanceToBestTarget < 121 || distanceToBestTarget > Vector2.DistanceSquared(chokePoint.Center, commander.UnitCalculation.Position))
                        {
                            if (MapDataService.MapHeight(new Point2D { X = chokePoint.Center.X, Y = chokePoint.Center.Y }) > MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos))
                            {
                                if (MapDataService.SelfVisible(chokePoint.Center, 4))
                                {
                                    return false;
                                }
                                commander.CurrentPath = null;
                                action = commander.Order(frame, Abilities.MOVE, new Point2D { X = chokePoint.Center.X, Y = chokePoint.Center.Y });
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        protected virtual bool GetHighGroundVisionSupport(UnitCommander commander, Point2D target, Point2D defensivePoint, UnitCalculation bestTarget, UnitCommander targetToSupport, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;

            if (GetHighGroundVision(commander, target, defensivePoint, bestTarget, frame, out action)) { return true; }

            if (ShouldStayOutOfRange(commander, frame) || MicroPriority == MicroPriority.JustLive || !commander.UnitCalculation.NearbyAllies.Any()) { return false; }
            if (!commander.UnitCalculation.Unit.IsFlying && commander.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_COLOSSUS)
            {
                var badChokes = TargetingData.ChokePoints.Bad.Where(b => Vector2.DistanceSquared(b.Center, targetToSupport.UnitCalculation.Position) < 100 || Vector2.DistanceSquared(b.Center, commander.UnitCalculation.Position) < 100);
                if (badChokes.Any())
                {
                    var chokePoint = badChokes.OrderBy(b => Vector2.DistanceSquared(b.Center, target.ToVector2())).First();
                    if (Vector2.Distance(chokePoint.Center, targetToSupport.UnitCalculation.Position) <= targetToSupport.UnitCalculation.Range + 1)
                    {
                        if (MapDataService.SelfVisible(chokePoint.Center, 4))
                        {
                            return false;
                        }
                        commander.CurrentPath = null;
                        action = commander.Order(frame, Abilities.MOVE, new Point2D { X = chokePoint.Center.X, Y = chokePoint.Center.Y });
                        return true;
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
                var shieldBatttery = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && a.Unit.BuildProgress == 1 && a.Unit.Energy > 5 && !a.Unit.Orders.Any()).OrderBy(b => Vector2.DistanceSquared(commander.UnitCalculation.Position, b.Position)).FirstOrDefault();
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

        protected virtual bool HoldStillForRepair(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Repairers.Any() && commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax && commander.UnitCalculation.PreviousUnit.Health < commander.UnitCalculation.Unit.Health)
            {
                action = commander.Order(frame, Abilities.STOP);
                return true;
            }
            return false;
        }

        protected virtual bool GetInBunker(UnitCommander commander, Point2D target, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        public virtual bool NavigateToTarget(UnitCommander commander, Point2D target, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Loaded) { return false; }
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
                    return MoveToAttackTarget(commander, bestTarget, formation, frame, out action);
                }
            }

            if (GroupUpEnabled && GroupUp(commander, target, groupCenter, false, frame, out action)) { return true; }

            if (AvoidDeceleration(commander, target, false, frame, out action)) { return true; }

            if (bestTarget != null && bestTarget.Unit.Tag != commander.UnitCalculation.Unit.Tag && MicroPriority != MicroPriority.NavigateToLocation && MapDataService.SelfVisible(bestTarget.Unit.Pos))
            {
                return MoveToAttackTarget(commander, bestTarget, formation, frame, out action);
            }

            action = MoveToTarget(commander, target, frame);
            return true;
        }

        public virtual bool AvoidDeceleration(UnitCommander commander, Point2D target, bool attackMove, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            // if air unit or worker (units with acceleration) and within X distance of target move at a 45 degree angle from target to avoid decelerating
            if (commander.UnitCalculation.Unit.IsFlying || commander.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker))
            {
                if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), commander.UnitCalculation.Position) < 4)
                {
                    var angle = commander.UnitCalculation.Unit.Facing + .78f;
                    var point = GetPoint(commander.UnitCalculation.Position, angle, 5);

                    if (point.X < 0 || point.Y < 0 || PutsCommanderInDanger(commander, point))
                    {
                        point = GetPoint(commander.UnitCalculation.Position, commander.UnitCalculation.Unit.Facing - .78f, 5);
                    }
                    if (point.X < 0 || point.X > MapDataService.MapData.MapWidth || PutsCommanderInDanger(commander, point))
                    {
                        point.X = commander.UnitCalculation.Position.X;
                    }
                    if (point.Y < 0 || point.Y > MapDataService.MapData.MapHeight || PutsCommanderInDanger(commander, point))
                    {
                        point.Y = commander.UnitCalculation.Position.Y;
                    }

                    if (PutsCommanderInDanger(commander, point))
                    {
                        return false;
                    }
                    
                    if (attackMove)
                    {
                        action = commander.Order(frame, Abilities.ATTACK, point);
                    }
                    else
                    {
                        action = commander.Order(frame, Abilities.MOVE, point);
                    }
                    return true;
                }
            }
            return false;
        }

        protected bool PutsCommanderInDanger(UnitCommander commander, Point2D point)
        {
            var vector = point.ToVector2();
            if (!commander.UnitCalculation.EnemiesInRangeOf.Any() && commander.UnitCalculation.EnemiesThreateningDamage.Any())
            {
                if (commander.UnitCalculation.EnemiesThreateningDamage.Any(e => InRange(e.Position, vector, e.Range + commander.UnitCalculation.Unit.Radius + e.Unit.Radius)))
                {
                    return true;
                }
            }
            return false;
        }

        protected virtual bool MoveToAttackTarget(UnitCommander commander, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (bestTarget == null) { return false; }

            if (commander.UnitCalculation.Unit.IsFlying && bestTarget.Attributes.Contains(SC2APIProtocol.Attribute.Structure) || 
                commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax || commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax ||
                bestTarget.EnemiesInRangeOf.Count < 5 ||
                (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && 
                (commander.UnitCalculation.Unit.IsFlying || commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_REAPER || commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS || MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) == MapDataService.MapHeight(bestTarget.Unit.Pos))))
            {
                if (SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType) ||
                    (!commander.UnitCalculation.EnemiesThreateningDamage.Any() && !DamageService.CanDamage(bestTarget, commander.UnitCalculation)))
                {
                    var point = new Point2D { X = bestTarget.Position.X, Y = bestTarget.Position.Y };
                    if (AvoidDeceleration(commander, point, false, frame, out action)) { return true; }
                    action = MoveToTarget(commander, point, frame);

                    return true;
                }

                Point2D attackPoint;
                if (MicroPriority == MicroPriority.AttackForward || commander.UnitCalculation.Unit.IsHallucination || (formation == Formation.Loose && commander.UnitCalculation.NearbyAllies.Count >= 5))
                {
                    attackPoint = new Point2D { X = bestTarget.Position.X, Y = bestTarget.Position.Y };
                }
                else if (commander.UnitCalculation.Range - 2 > bestTarget.Range && commander.UnitCalculation.UnitTypeData.MovementSpeed >= bestTarget.UnitTypeData.MovementSpeed)
                {
                    attackPoint = GetPositionFromRange(commander, bestTarget.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Range - 2 + bestTarget.Unit.Radius + commander.UnitCalculation.Unit.Radius);
                }
                else
                {
                    attackPoint = GetPositionFromRange(commander, bestTarget.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Range + bestTarget.Unit.Radius + commander.UnitCalculation.Unit.Radius);
                }

                if (commander.UnitCalculation.Unit.IsFlying)
                {
                    if (!commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageAir))
                    {
                        attackPoint = new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y };
                    }
                    if (AvoidDeceleration(commander, attackPoint, false, frame, out action)) { return true; }
                }

                action = MoveToTarget(commander, attackPoint, frame);
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

            if (commander.UnitRole == UnitRole.Leader || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat) { return false; }

            if (formation == Formation.Normal)
            {
                return false;
            }

            if (formation == Formation.Loose)
            {
                var closestAlly = commander.UnitCalculation.NearbyAllies.OrderBy(a => Vector2.DistanceSquared(commander.UnitCalculation.Position, a.Position)).FirstOrDefault();
                if (closestAlly != null)
                {
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, closestAlly.Position) < (LooseFormationDistance * LooseFormationDistance))
                    {
                        var avoidPoint = GetPositionFromRange(commander, closestAlly.Unit.Pos, commander.UnitCalculation.Unit.Pos, LooseFormationDistance + .5f);
                        action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                        return true;
                    }
                }
            }

            if (formation == Formation.Tight && commander.UnitCalculation.NearbyEnemies.Any())
            {
                var vectors = commander.UnitCalculation.NearbyAllies.Where(a => (!a.Unit.IsFlying && !commander.UnitCalculation.Unit.IsFlying && a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) || (commander.UnitCalculation.Unit.IsFlying && a.Unit.UnitType == commander.UnitCalculation.Unit.UnitType)).Where(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position) < GroupUpDistance * GroupUpDistance).Select(u => u.Position);
                if (vectors.Any())
                {
                    var max = 1f;
                    if (!commander.UnitCalculation.Unit.IsFlying)
                    {
                        max = commander.UnitCalculation.Unit.Radius * vectors.Count();
                    }
                    var vectorList = vectors.ToList();
                    vectorList.Add(commander.UnitCalculation.Position);
                    var center = new Point2D { X = vectorList.Average(v => v.X), Y = vectorList.Average(v => v.Y) };
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(center.X, center.Y)) > (max * max))
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

            if (commander.UnitCalculation.TargetPriorityCalculation.Overwhelm || MicroPriority == MicroPriority.AttackForward || commander.UnitCalculation.Unit.IsHallucination)
            {
                if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action)) { return true; }

                if (commander.UnitCalculation.Unit.ShieldMax > 0 && commander.UnitCalculation.Unit.Shield < 25 && AvoidDamage(commander, target, defensivePoint, frame, out action)) // TODO: this only works for protoss, if we want it to work for zerg and terran it needs to change
                {
                    return true;
                }
            }
            else if (MicroPriority == MicroPriority.LiveAndAttack || ShouldStayOutOfRange(commander, frame))
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

        /// <summary>
        /// if unit has more range and greater or equal range than nearest enemy do not attack unless no units threatening damage, make sure unitsthreateneingdamage is correctly calculated
        /// </summary>
        public virtual bool AvoidPointlessDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Range < 3 || MicroPriority == MicroPriority.AttackForward || (formation == Formation.Loose && commander.UnitCalculation.NearbyAllies.Count >= 5)) { return false; }

            if (commander.UnitCalculation.Unit.IsHallucination) { return false; }
            if (commander.UnitCalculation.Unit.Shield > 75 && commander.UnitCalculation.Unit.Health > 75) { return false; }

            var speed = commander.UnitCalculation.UnitTypeData.MovementSpeed;
            if (commander.UnitCalculation.EnemiesThreateningDamage.Any(e => e.UnitTypeData.MovementSpeed <= speed && e.EnemiesInRange.Count <= 1)) // if ally is already in range of the enemy shoot it because it's going to shoot ally
            {
                if (!commander.UnitCalculation.EnemiesInRangeOf.Any() && commander.UnitCalculation.EnemiesThreateningDamage.Any(e => e.Range >= commander.UnitCalculation.Range || e.UnitTypeData.MovementSpeed >= speed)) // TODO: factor in creep and other speed buffs
                {
                    return false;
                }

                var attack = commander.UnitCalculation.EnemiesThreateningDamage.OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();

                if (WeaponReady(commander, frame) && GetDamage(commander.UnitCalculation.Weapons, attack.Unit, attack.UnitTypeData) > attack.Unit.Health + attack.Unit.Shield)
                {
                    return false;
                }

                var distanceToEnemy = Vector2.Distance(attack.Position, commander.UnitCalculation.Position);

                Point2D avoidPoint;
                if (commander.UnitCalculation.Unit.IsFlying)
                {
                    avoidPoint = GetAirAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, distanceToEnemy + 2);
                }
                else
                {
                    avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, distanceToEnemy + 2);
                }

                if (avoidPoint != defensivePoint && avoidPoint != target)
                {
                    if (!WeaponReady(commander, frame) && !commander.UnitCalculation.EnemiesInRangeOf.Any() && AvoidDeceleration(commander, avoidPoint, false, frame, out action)) { return true; }

                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }

                if (commander.RetreatPathFrame + 2 < frame)
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
            }

            return false;
        }

        protected virtual bool AvoidAllDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            if (AvoidEnemiesThreateningDamage(commander, target, defensivePoint, frame, true, out action)) { return true; }
            if (AvoidArmyEnemies(commander, target, defensivePoint, frame, true, out action)) { return true; }
            if (AvoidNearbyEnemies(commander, target, defensivePoint, frame, true, out action)) { return true; }

            return false;
        }

        protected virtual bool AvoidDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            if (AvoidEnemiesThreateningDamage(commander, target, defensivePoint, frame, false, out action)) { return true; }

            if (MaintainRange(commander, defensivePoint, frame, out action)) { return true; }

            return false;
        }


        public virtual bool AvoidDamageList(UnitCommander commander, Point2D target, Point2D defensivePoint, IEnumerable<UnitCalculation> attacks, int frame, bool alwaysRun, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            if (attacks.Any())
            {
                var attack = attacks.OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();  // enemies that are closest to being outranged
                var range = UnitDataService.GetRange(attack.Unit);
                if (attack.Range > range)
                {
                    range = attack.Range;
                }

                if (!alwaysRun && commander.UnitCalculation.Range < range && commander.UnitCalculation.UnitTypeData.MovementSpeed <= attack.UnitTypeData.MovementSpeed)
                {
                    return false; // if we can't get out of range before we attack again don't bother running away
                }

                if (commander.RetreatPathFrame + 1 < frame)
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

                Point2D avoidPoint;
                if (commander.UnitCalculation.Unit.IsFlying)
                {
                    avoidPoint = GetAirAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance);
                }
                else
                {
                    avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance);
                }

                if (avoidPoint != defensivePoint && avoidPoint != target)
                {
                    if (AvoidDeceleration(commander, avoidPoint, false, frame, out action)) { return true; }
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }

            return false;
        }

        protected virtual bool AvoidEnemiesThreateningDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool alwaysRun, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            var attacks = commander.UnitCalculation.EnemiesThreateningDamage.Where(e => e.FrameLastSeen > frame - 5);
            if (AvoidDamageList(commander, target, defensivePoint, attacks, frame, alwaysRun, out action)) { return true; }

            return false;
        }

        protected virtual bool AvoidArmyEnemies(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool alwaysRun, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            var attacks = commander.UnitCalculation.NearbyEnemies.Where(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && DamageService.CanDamage(e, commander.UnitCalculation));
            if (AvoidDamageList(commander, target, defensivePoint, attacks, frame, alwaysRun, out action)) { return true; }

            return false;
        }

        protected virtual bool AvoidNearbyEnemies(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, bool alwaysRun, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            var attacks = commander.UnitCalculation.NearbyEnemies.Where(e => DamageService.CanDamage(e, commander.UnitCalculation));
            if (AvoidDamageList(commander, target, defensivePoint, attacks, frame, alwaysRun, out action)) { return true; }

            return false;
        }

        protected virtual bool MaintainRange(UnitCommander commander, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (MicroPriority == MicroPriority.JustLive || MicroPriority == MicroPriority.AttackForward || commander.UnitCalculation.Unit.IsHallucination)
            {
                return false;
            }

            if (commander.UnitCalculation.Unit.ShieldMax > 0 && commander.UnitCalculation.Unit.Shield < 1)
            {
                return false;
            }

            var range = commander.UnitCalculation.Range;
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(enemyAttack => DamageService.CanDamage(enemyAttack, commander.UnitCalculation) && InRange(commander.UnitCalculation.Position, enemyAttack.Position, range + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius + AvoidDamageDistance));

            var closestEnemy = enemiesInRange.OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closestEnemy == null)
            {
                return false;
            }

            var avoidPoint = GetPositionFromRange(commander, closestEnemy.Unit.Pos, commander.UnitCalculation.Unit.Pos, range + commander.UnitCalculation.Unit.Radius + closestEnemy.Unit.Radius);
            if (!commander.UnitCalculation.Unit.IsFlying && commander.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_COLOSSUS)
            {
                if (MapDataService.MapHeight(avoidPoint) != MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) || MapDataService.MapHeight(avoidPoint) != MapDataService.MapHeight(closestEnemy.Unit.Pos))
                {
                    return false;
                }
            }
            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
            return true;
        }

        public virtual bool Retreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.EnemiesInRange.Any() && WeaponReady(commander, frame) && !SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType)) // keep shooting as you retreat
            {
                var bestTarget = GetBestTarget(commander, target, frame);
                if (bestTarget != null && MapDataService.SelfVisible(bestTarget.Unit.Pos) && bestTarget.FrameLastSeen == frame)
                {
                    action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    return true;
                }
            }

            if (GetInBunker(commander, target, frame, out action)) { return true; }

            var closestEnemy = commander.UnitCalculation.NearbyEnemies.Where(e => DamageService.CanDamage(e, commander.UnitCalculation) && e.FrameLastSeen >= frame - 5).OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closestEnemy == null)
            {
                closestEnemy = commander.UnitCalculation.NearbyEnemies.Where(e => e.FrameLastSeen >= frame - 5).OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            }


            if (closestEnemy != null)
            {
                if (DefendBehindWall(commander, defensivePoint, defensivePoint, frame, out action)) { return true; }

                if (commander.UnitCalculation.NearbyEnemies.All(e => e.Range < commander.UnitCalculation.Range && e.UnitTypeData.MovementSpeed < commander.UnitCalculation.UnitTypeData.MovementSpeed))
                {
                    if (MapDataService.SelfVisible(closestEnemy.Unit.Pos))
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
                                if (MaintainRange(commander, defensivePoint, frame, out action)) { return true; }
                            }
                        }
                    }
                }

                if (commander.RetreatPathFrame + 2 < frame || commander.RetreatPathIndex >= commander.RetreatPath.Count)
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
            }

            action = MoveToTarget(commander, defensivePoint, frame);
            return true;
        }

        public virtual bool FollowPath(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.RetreatPath.Any())
            {
                if (SharkyOptions.Debug)
                {
                    var thing = commander.RetreatPath.ToList();
                    for (int index = 0; index < thing.Count - 1; index++)
                    {
                        DebugService.DrawLine(new Point { X = thing[index].X, Y = thing[index].Y, Z = 16 }, new Point { X = thing[index + 1].X, Y = thing[index + 1].Y, Z = 20 }, new Color { R = 255, G = 255, B = 255 });
                    }
                }

                if (commander.RetreatPathIndex < commander.RetreatPath.Count)
                {
                    var point = commander.RetreatPath[commander.RetreatPathIndex];
                    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = commander.UnitCalculation.Unit.Pos.Z }, 1, new Color { R = 0, G = 0, B = 255 });

                    // TODO: try to avoid turning too quickly and decelerating for flying units
                    //if (commander.UnitCalculation.Unit.IsFlying && commander.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_PHOENIX && !commander.UnitCalculation.EnemiesInRangeOf.Any())
                    //{
                    //    // if angle greater than 45 degrees turn toward it instead of going straight there to avoid deceleration
                    //    var angle = Math.Atan2(commander.UnitCalculation.Position.Y - point.Y, point.X - commander.UnitCalculation.Position.X);
                    //    if (Math.Abs(commander.UnitCalculation.Unit.Facing - angle) > .78f)
                    //    {
                    //        var right = GetPoint(point, commander.UnitCalculation.Unit.Facing + .78f, 2);
                    //        var left = GetPoint(point, commander.UnitCalculation.Unit.Facing - .78f, 2);
                    //        if (right.X > 0 && right.X > 0 && Vector2.DistanceSquared(new Vector2(right.X, right.Y), point) < Vector2.DistanceSquared(new Vector2(left.X, left.Y), point))
                    //        {
                    //            action = commander.Order(frame, Abilities.MOVE, new Point2D { X = right.X, Y = right.Y });
                    //            DebugService.DrawSphere(new Point { X = right.X, Y = right.Y, Z = commander.UnitCalculation.Unit.Pos.Z }, 1, new Color { R = 0, G = 255, B = 255 });
                    //            return true;
                    //        }
                    //        else if (left.X > 0 && left.X > 0)
                    //        {
                    //            action = commander.Order(frame, Abilities.MOVE, new Point2D { X = left.X, Y = left.Y });
                    //            DebugService.DrawSphere(new Point { X = left.X, Y = left.Y, Z = commander.UnitCalculation.Unit.Pos.Z }, 1, new Color { R = 0, G = 255, B = 255 });
                    //            return true;
                    //        }
                    //    }
                    //}
                    action = commander.Order(frame, Abilities.MOVE, new Point2D { X = point.X, Y = point.Y });

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
        public virtual UnitCalculation GetBestTarget(UnitCommander commander, Point2D target, int frame)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            var range = commander.UnitCalculation.Range;

            var attacks = commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType == DisplayType.Visible && AttackersFilter(commander, u)); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Any())
            {
                var oneShotKills = attacks.Where(a => PredictedHealth(a) < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData) && !a.Unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD));
                if (oneShotKills.Any())
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

                bestAttack = GetBestTargetFromList(commander, attacks.Where(a => a.Unit.UnitType != (uint)UnitTypes.PROTOSS_INTERCEPTOR), existingAttackOrder);
                if (bestAttack != null && (bestAttack.UnitClassifications.Contains(UnitClassification.ArmyUnit) || bestAttack.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || (bestAttack.UnitClassifications.Contains(UnitClassification.Worker) && bestAttack.EnemiesInRange.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag))))
                {
                    if (bestAttack.Unit.UnitType != (uint)UnitTypes.TERRAN_SCV && bestAttack.Unit.UnitType != (uint)UnitTypes.TERRAN_MULE && bestAttack.Repairers.Any())
                    {
                        if ((bestAttack.Unit.UnitType != (uint)UnitTypes.TERRAN_SIEGETANKSIEGED && bestAttack.Unit.UnitType != (uint)UnitTypes.TERRAN_SIEGETANK) && bestAttack.Repairers.Any(r => r.Health < bestAttack.SimulatedHitpoints))
                        {
                            var ids = bestAttack.Repairers.Select(r => r.Tag);
                            var repairer = GetBestTargetFromList(commander, bestAttack.NearbyAllies.Where(a => ids.Contains(a.Unit.Tag)), existingAttackOrder);
                            if (repairer != null)
                            {
                                bestAttack = repairer;
                            }
                        }
                    }
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
                && enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation, enemyAttack) && !AvoidedUnitTypes.Contains((UnitTypes)enemyAttack.Unit.UnitType) && AttackersFilter(commander, enemyAttack));

            attacks = outOfRangeAttacks.Where(enemyAttack => enemyAttack.EnemiesInRange.Any() && enemyAttack.Unit.UnitType != (uint)UnitTypes.PROTOSS_INTERCEPTOR);
            if (attacks.Any())
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
            if (attacks.Any())
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
                fakeMainBase.Pos = new Point { X = TargetingData.EnemyMainBasePoint.X, Y = TargetingData.EnemyMainBasePoint.Y, Z = 1 };
                fakeMainBase.Alliance = Alliance.Enemy;
                return new UnitCalculation(fakeMainBase, new List<Unit>(), SharkyUnitData, SharkyOptions, UnitDataService, MapDataService.IsOnCreep(fakeMainBase.Pos), frame);
            }
            var unitsNearEnemyMain = ActiveUnitData.EnemyUnits.Values.Where(e => !AvoidedUnitTypes.Contains((UnitTypes)e.Unit.UnitType) && e.Unit.UnitType != (uint)UnitTypes.ZERG_LARVA && InRange(new Vector2(target.X, target.Y), e.Position, 20));
            if (unitsNearEnemyMain.Any() && InRange(new Vector2(target.X, target.Y), commander.UnitCalculation.Position, 100))
            {
                attacks = unitsNearEnemyMain.Where(enemyAttack => enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation, enemyAttack) && AttackersFilter(commander, enemyAttack));
                if (attacks.Any())
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

        protected UnitCalculation GetBestTarget(UnitCommander commander, List<UnitTypes> targetPriorityList, Point2D target, int frame, float range = -1)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            if (range < 0)
            {
                range = commander.UnitCalculation.Range;
            }

            // damaging units in priority in range or not
            // non damaging units in priority in range, then not in range
            // other stuff in range, then not in range

            var priorityAttacks = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.DisplayType == DisplayType.Visible && targetPriorityList.Contains((UnitTypes)e.Unit.UnitType));
            var priorityDamagers = priorityAttacks.Where(e => e.Damage > 0);
            var priorityOther = priorityAttacks.Where(e => e.Damage == 0);

            var attacks = commander.UnitCalculation.EnemiesInRange.Where(e => priorityDamagers.Any(p => p.Unit.Tag == e.Unit.Tag));

            UnitCalculation bestAttack = null;
            if (attacks.Any())
            {
                var oneShotKills = attacks.Where(a => a.Unit.Health + a.Unit.Shield < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData) && !a.Unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD));
                if (oneShotKills.Any())
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
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = priorityDamagers;
            if (attacks.Any())
            {
                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            // defend proxy units/buildings
            attacks = commander.UnitCalculation.NearbyEnemies.Where(e => e.EnemiesInRange.Any());
            if (attacks.Any())
            {
                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = commander.UnitCalculation.EnemiesInRange.Where(e => priorityOther.Any(p => p.Unit.Tag == e.Unit.Tag));
            if (attacks.Any())
            {
                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = commander.UnitCalculation.EnemiesInRange.Where(e => priorityOther.Any(p => p.Unit.Tag == e.Unit.Tag));
            if (attacks.Any())
            {
                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = priorityOther;
            if (attacks.Any())
            {
                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            return null;
        }

        protected Point2D GetBestTargetAttackPoint(UnitCommander commander, UnitCalculation bestTarget)
        {
            var enemyPosition = new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y };
            // TODO: make sure this is working correctly, not sure if Vector is accurate, or this function is accurate
            if (bestTarget.Velocity > 0 && false)
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

        protected virtual bool IsDistraction(UnitCommander commander, Point2D target, UnitCalculation enemy, int frame)
        {
            if (enemy != null && AvoidedUnitTypes.Contains((UnitTypes)enemy.Unit.UnitType))
            {
                return true;
            }
            if (enemy != null && enemy.Unit.Tag != commander.UnitCalculation.Unit.Tag && !enemy.EnemiesInRange.Any() && !enemy.EnemiesInRangeOf.Any() &&
                !commander.UnitCalculation.EnemiesInRangeOfAvoid.Any() && !commander.UnitCalculation.EnemiesInRange.Any())
            {
                var targetVector = new Vector2(target.X, target.Y);
                if (Vector2.DistanceSquared(targetVector, enemy.Position) > Vector2.DistanceSquared(targetVector, commander.UnitCalculation.Position))
                {
                    return true; // ignore it, it's a distraction and a detour from the target
                }
            }
            return false;
        }

        public virtual bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action))
            {
                return true;
            }

            if (bestTarget != null && bestTarget.Unit.Tag != commander.UnitCalculation.Unit.Tag && bestTarget.Unit.UnitType == (uint)UnitTypes.PROTOSS_CARRIER && bestTarget.FrameLastSeen == frame && commander.UnitCalculation.EnemiesThreateningDamage.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_INTERCEPTOR))
            {
                action = commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                return true;
            }
            if (commander.UnitCalculation.EnemiesThreateningDamage.Any() && !commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                return false;
            }

            if (IgnoreDistractions && IsDistraction(commander, target, bestTarget, frame))
            {
                return false;
            }

            bool priorityEnemyMain = false;
            if (TargetEnemyMainFirst)
            {
                if (MapDataService.LastFrameVisibility(TargetingData.EnemyMainBasePoint) < 5 * 60 * SharkyOptions.FramesPerSecond || (TargetingData.AttackPoint.X == TargetingData.EnemyMainBasePoint.X && TargetingData.AttackPoint.Y == TargetingData.EnemyMainBasePoint.Y))
                {
                    if (Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), commander.UnitCalculation.Position) > 100)
                    {
                        priorityEnemyMain = true;
                        if (bestTarget == null || bestTarget.Unit.Tag == commander.UnitCalculation.Unit.Tag) { return false; }
                        if ((!bestTarget.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !bestTarget.UnitClassifications.Contains(UnitClassification.DefensiveStructure) && !bestTarget.UnitClassifications.Contains(UnitClassification.Worker)))
                        {
                            return false;
                        }
                    }
                }
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

                if (WeaponReady(commander, frame))
                {
                    if (priorityEnemyMain || (bestTarget != null && bestTarget.FrameLastSeen == frame && commander.UnitCalculation.NearbyEnemies.Any(e => AvoidedUnitTypes.Contains((UnitTypes)e.Unit.UnitType))))
                    {
                        action = commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                    }
                    else
                    {
                        action = AttackToTarget(commander, enemyPosition, frame);
                    }
                }
                else
                {
                    if (GroupUpEnabled && GroupUp(commander, target, groupCenter, false, frame, out action)) { return true; }
                    if (AvoidDeceleration(commander, enemyPosition, false, frame, out action)) { return true; }

                    action = MoveToTarget(commander, enemyPosition, frame);
                }
                return true;
            }

            if (GroupUpEnabled && GroupUp(commander, target, groupCenter, true, frame, out action))
            {
                return true;
            }

            if (bestTarget != null && bestTarget.Unit.Tag != commander.UnitCalculation.Unit.Tag && MicroPriority != MicroPriority.NavigateToLocation)
            {
                var enemyPosition = GetBestTargetAttackPoint(commander, bestTarget);
                if (SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType) || commander.UnitCalculation.NearbyEnemies.Any(e => AvoidedUnitTypes.Contains((UnitTypes)e.Unit.UnitType)))
                {
                    action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                    return true;
                }
                if (WeaponReady(commander, frame))
                {
                    var marked = commander.UnitCalculation.NearbyAllies.FirstOrDefault(a => ActiveUnitData.Commanders.ContainsKey(a.Unit.Tag) && ActiveUnitData.Commanders[a.Unit.Tag].UnitRole == UnitRole.Die);
                    if (marked != null)
                    {
                        action = commander.Order(frame, Abilities.ATTACK, targetTag: marked.Unit.Tag);
                    }
                    else if (priorityEnemyMain)
                    {
                        if (bestTarget.Unit.Tag == commander.UnitCalculation.Unit.Tag)
                        {
                            action = MoveToTarget(commander, enemyPosition, frame);
                        }
                        else
                        {
                            action = commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                        }
                    }
                    else
                    {
                        action = commander.Order(frame, Abilities.ATTACK, enemyPosition);
                    }
                }
                else
                {
                    if (GroupUpEnabled && GroupUp(commander, target, groupCenter, false, frame, out action)) { return true; }
                    if (AvoidDeceleration(commander, enemyPosition, false, frame, out action)) { return true; }
                    action = MoveToTarget(commander, enemyPosition, frame);
                }
                return true;
            }

            if (AttackUnitsMarkedForDeath(commander, frame, out action)) { return true; }

            if (AvoidDeceleration(commander, target, true, frame, out action)) { return true; }

            // no damaging targets in range, attack towards the main target
            if (priorityEnemyMain || commander.UnitCalculation.NearbyEnemies.Any(e => AvoidedUnitTypes.Any(a => (uint)a == e.Unit.UnitType)))
            {
                action = MoveToTarget(commander, target, frame);
            }
            else
            {
                action = AttackToTarget(commander, target, frame);
            }
            return true;
        }

        public virtual List<SC2APIProtocol.Action> MoveToTarget(UnitCommander commander, Point2D target, int frame)
        {
            if (!MapDataService.PathWalkable(target))
            {
                return commander.Order(frame, Abilities.MOVE, target);
            }

            if (commander.UnitCalculation.NearbyAllies.Any(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure)) && Vector2.DistanceSquared(commander.UnitCalculation.Position, target.ToVector2()) > 400)
            {
                return commander.Order(frame, Abilities.MOVE, target);
            }

            var moveTo = GetNextPointToTarget(commander, target, frame);
            return commander.Order(frame, Abilities.MOVE, moveTo);
        }

        protected virtual List<SC2APIProtocol.Action> AttackToTarget(UnitCommander commander, Point2D target, int frame)
        {
            if (!MapDataService.PathWalkable(target))
            {
                return commander.Order(frame, Abilities.ATTACK, target);
            }

            if (!AlwaysUsePathing && commander.UnitCalculation.NearbyAllies.Any(a => a.Attributes.Contains(SC2APIProtocol.Attribute.Structure)) && Vector2.DistanceSquared(commander.UnitCalculation.Position, target.ToVector2()) > 400 && !(commander.UnitRole == UnitRole.Leader && commander.UnitCalculation.NearbyAllies.Any(a => !a.Unit.IsFlying)))
            {
                return commander.Order(frame, Abilities.ATTACK, target);
            }

            var moveTo = GetNextPointToTarget(commander, target, frame);
            return commander.Order(frame, Abilities.ATTACK, moveTo);
        }

        protected virtual Point2D GetNextPointToTarget(UnitCommander commander, Point2D target, int frame)
        {
            return AttackPathingService.GetNextPointToTarget(commander, target, frame);
        }

        protected virtual bool GroupUp(UnitCommander commander, Point2D target, Point2D groupCenter, bool attack, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (!AttackData.GroupUpEnabled || commander.UnitCalculation.NearbyEnemies.Any() || groupCenter == null)
            {
                return false;
            }

            if (frame > SharkyOptions.FramesPerSecond * 10 * 60 && ActiveUnitData.EnemyUnits.Count < 10)
            {
                return false; // stop grouping up when searching for the last enemy units to finish the game
            }
            // TOOD: don't just group up in the center, line up to form a concave or perpendicular line to target
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
                    if (AvoidDeceleration(commander, groupCenter, true, frame, out action)) { return true; }
                    action = commander.Order(frame, Abilities.ATTACK, groupCenter);
                }
                else
                {
                    if (AvoidDeceleration(commander, groupCenter, false, frame, out action)) { return true; }
                    action = commander.Order(frame, Abilities.MOVE, groupCenter);
                }

                return true;
            }

            return false;
        }

        public virtual bool GroupUpBasedOnState(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.CommanderState == CommanderState.Grouping && groupCenter != null)
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

        public virtual bool AttackBestTargetInRange(UnitCommander commander, Point2D target, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (bestTarget != null)
            {
                if (commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && bestTarget.Unit.DisplayType == DisplayType.Visible && MapDataService.SelfVisible(bestTarget.Unit.Pos) && bestTarget.FrameLastSeen == frame)
                {
                    bestTarget.IncomingDamage += GetDamage(commander.UnitCalculation.Weapons, bestTarget.Unit, bestTarget.UnitTypeData);
                    if (WeaponReady(commander, frame))
                    {
                        if (commander.UnitRole == UnitRole.Leader) { CameraManager.SetCamera(commander.UnitCalculation.Position, bestTarget.Position); }
                        action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                        commander.LastInRangeAttackFrame = frame;
                    }
                    else
                    {
                        if (AvoidDeceleration(commander, new Point2D { X = commander.UnitCalculation.Position.X, Y = commander.UnitCalculation.Position.Y }, false, frame, out action)) { return true; }
                        action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    }
                    return true;
                }
                else if (WeaponReady(commander, frame) && commander.UnitCalculation.EnemiesInRange.Any(e => e.FrameLastSeen == frame && e.Unit.UnitType != (uint)UnitTypes.PROTOSS_INTERCEPTOR))
                {
                    var bestInRange = GetBestTargetFromList(commander, commander.UnitCalculation.EnemiesInRange.Where(e => e.FrameLastSeen == frame && e.Unit.UnitType != (uint)UnitTypes.PROTOSS_INTERCEPTOR), null);
                    if (bestInRange != null)
                    {
                        if (commander.UnitRole == UnitRole.Leader) { CameraManager.SetCamera(commander.UnitCalculation.Position, bestTarget.Position); }
                        action = commander.Order(frame, Abilities.ATTACK, null, bestInRange.Unit.Tag);
                        commander.LastInRangeAttackFrame = frame;
                        return true;
                    }
                }
            }

            return false;
        }

        public virtual UnitCalculation GetBestTargetFromList(UnitCommander commander, IEnumerable<UnitCalculation> attacks, UnitOrder existingAttackOrder)
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
                var scvs = attacks.Where(u => u.UnitClassifications.Contains(UnitClassification.Worker)).OrderBy(u => u.Unit.Health).ThenBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position));
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

                var buildingScv = scvs.FirstOrDefault(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV &&
                                e.NearbyAllies.Any(a => a.Unit.BuildProgress < 1 && Vector2.DistanceSquared(a.Position, e.Position) < ((e.Unit.Radius + a.Unit.Radius + 1) * (e.Unit.Radius + a.Unit.Radius + 1))));
                if (buildingScv != null)
                {
                    return buildingScv;
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

                if (airAttackers.Any())
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
                            if (bestDpsReduction == null)
                            {
                                return null;
                            }
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
                if (groundAttackers.Any())
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
            if (threats.Any())
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
            if (workers.Any())
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
            if (weapon == null) { return 999f; }
            float bonus = 0;
            if (unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD))
            {
                bonus += 100;
            }
            var damage = GetDamage(weapon, unit, unitTypeData);
            return (unit.Health + unit.Shield + bonus) / (damage / weapon.Speed);
        }

        public virtual float GetDamage(List<Weapon> weapons, Unit unit, UnitTypeData unitTypeData)
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
            if (weapon == null || weapon.Damage == 0 || unitTypeData == null)
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

        public virtual bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown < 2;
        }

        public virtual bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        public virtual bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        public virtual Formation GetDesiredFormation(UnitCommander commander)
        {
            if (commander.UnitCalculation.Unit.IsFlying)
            {
                if (MapDataService.GetCells(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, 5).Any(e => e.EnemyAirSplashDpsInRange > 0))
                {
                    return Formation.Loose;
                }
                else
                {
                    if (commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == commander.UnitCalculation.Unit.UnitType))
                    {
                        return Formation.Tight;
                    }
                    return Formation.Normal;
                }
            }

            var splashDps = MapDataService.EnemyGroundSplashDpsInRange(commander.UnitCalculation.Unit.Pos);
            var zerglingDps = 0f;
            if (EnemyData.EnemyRace == Race.Zerg)
            {
                if (splashDps == 0 && commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING || e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLINGBURROWED))
                {
                    zerglingDps = 10f;
                }
                else
                {
                    zerglingDps = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING || e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLINGBURROWED).Sum(e => e.Dps);
                }
            }

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
                if (commander.UnitCalculation.EnemiesInRangeOf.Any())
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

        public virtual bool AvoidTargettedOneHitKills(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            var attack = commander.UnitCalculation.Attackers.Where(a => a.Damage > commander.UnitCalculation.Unit.Health + commander.UnitCalculation.Unit.Shield).OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();
            if (attack == null)
            {
                attack = commander.UnitCalculation.EnemiesThreateningDamage.Where(a => a.Damage > commander.UnitCalculation.Unit.Health + commander.UnitCalculation.Unit.Shield).OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();
            }
            if (attack != null)
            {
                if (WeaponReady(commander, frame))
                {
                    if (attack.EnemiesInRange.Any())
                    {
                        return false; // other units are in range, it is probably attacking them so get some extra damage in
                    }
                    if (attack.EnemiesInRangeOf.Any(e => e.Unit.Tag == commander.UnitCalculation.Unit.Tag) && GetDamage(commander.UnitCalculation.Weapons, attack.Unit, attack.UnitTypeData) >= attack.Unit.Health + attack.Unit.Shield)
                    {
                        return false; // just kill it before it kills you
                    }
                }

                if (commander.UnitCalculation.Unit.IsFlying)
                {
                    var avoidPoint = GetAirAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + 4);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
                else
                {
                    var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + 4);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }

            return false;
        }

        protected virtual bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if ((MicroPriority == MicroPriority.AttackForward || commander.UnitCalculation.Unit.IsHallucination) && commander.UnitCalculation.Unit.Health > commander.UnitCalculation.Unit.HealthMax / 4.0) { return false; }
            var attack = commander.UnitCalculation.Attackers.OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();
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

        protected virtual bool DealWithInterferenceMatrix(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (!commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.INTERFERENCEMATRIX)) { return false; }

            return AvoidAllDamage(commander, target, defensivePoint, frame, out action);
        }

        protected virtual bool DealWithParasiticBomb(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (!commander.UnitCalculation.Unit.IsFlying || !commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.PARASITICBOMB)) { return false; }

            var closestAlly = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.IsFlying).OrderBy(a => Vector2.DistanceSquared(commander.UnitCalculation.Position, a.Position)).FirstOrDefault();
            if (closestAlly != null)
            {
                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, closestAlly.Position) < (3f + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius) * (3f + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius))
                {
                    var avoidPoint = GetPositionFromRange(commander, closestAlly.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius + 3.5f);
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
            if (nova != null)
            {
                var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, nova.Unit.Pos, target, defensivePoint, 5);
                action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                return true;
            }

            if (commander.UnitCalculation.Unit.UnitType != (uint)UnitTypes.PROTOSS_DISRUPTOR)
            {
                nova = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED && a.Unit.BuffDurationRemain < a.Unit.BuffDurationMax / 2 && Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position) < 25).OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                if (nova != null)
                {
                    var novaAttackPoint = nova.Unit.Orders.FirstOrDefault(o => o.TargetWorldSpacePos != null);
                    if (novaAttackPoint != null)
                    {
                        if (Vector2.DistanceSquared(novaAttackPoint.TargetWorldSpacePos.ToVector2(), commander.UnitCalculation.Position) < Vector2.DistanceSquared(nova.Position, commander.UnitCalculation.Position))
                        {
                            var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, novaAttackPoint.TargetWorldSpacePos, target, defensivePoint, 5);
                            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                            return true;
                        }
                        else
                        {
                            var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, nova.Unit.Pos, target, defensivePoint, 5);
                            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                            return true;
                        }
                    }
                    else
                    {
                        var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, nova.Unit.Pos, target, defensivePoint, 5);
                        action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual bool AvoidRavagerShots(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var bile = SharkyUnitData.CorrosiveBiles.OrderBy(bile => Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(bile.Key.X, bile.Key.Y))).FirstOrDefault();
            if (bile.Key != null)
            {
                if (Vector2.DistanceSquared(new Vector2(bile.Key.X, bile.Key.Y), commander.UnitCalculation.Position) <= (2f + commander.UnitCalculation.Unit.Radius) * (2f + commander.UnitCalculation.Unit.Radius))
                {
                    Point2D avoidPoint;
                    if (commander.UnitCalculation.Unit.IsFlying)
                    {
                        avoidPoint = GetAirAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, new Point { X = bile.Key.X, Y = bile.Key.Y, Z = 1 }, target, defensivePoint, 6);
                    }
                    else
                    {
                        avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, new Point { X = bile.Key.X, Y = bile.Key.Y, Z = 1 }, target, defensivePoint, 6);

                    }
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
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

        protected virtual bool AvoidLiberationZones(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.IsFlying) { return false; }

            foreach (var effect in SharkyUnitData.Effects)
            {
                if (effect.EffectId == (uint)Effects.LIBERATIONZONE)
                {
                    if (Vector2.DistanceSquared(new Vector2(effect.Pos[0].X, effect.Pos[0].Y), commander.UnitCalculation.Position) <= (effect.Radius + commander.UnitCalculation.Unit.Radius + .5) * (effect.Radius + commander.UnitCalculation.Unit.Radius + .5) )
                    {
                        Point2D avoidPoint;

                        avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, new Point { X = effect.Pos[0].X, Y = effect.Pos[0].Y, Z = 1 }, target, defensivePoint, effect.Radius + commander.UnitCalculation.Unit.Radius + .5f);

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
            if (enemyCyclones.Any() && MicroPriority != MicroPriority.StayOutOfRange && (commander.UnitCalculation.TargetPriorityCalculation.AirWinnability > 1 || commander.UnitCalculation.TargetPriorityCalculation.GroundWinnability > 1))
            {
                var cycloneDps = enemyCyclones.Sum(e => e.Dps);
                var otherDps = commander.UnitCalculation.NearbyEnemies.Where(u => u.Unit.UnitType != (uint)UnitTypes.TERRAN_CYCLONE && InRange(commander.UnitCalculation.Position, u.Position, commander.UnitCalculation.Unit.Radius + lockOnRange)).Sum(e => e.Dps);

                if (cycloneDps > otherDps)
                {
                    var closestCyclone = enemyCyclones.OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position)).FirstOrDefault(e => e.FrameLastSeen == frame);
                    if (closestCyclone != null)
                    {
                        action = commander.Order(frame, Abilities.ATTACK, null, closestCyclone.Unit.Tag);
                        return true;
                    }
                }
            }

            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.LOCKON))
            {
                var cyclone = commander.UnitCalculation.EnemiesInRange.FirstOrDefault(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_CYCLONE && e.Unit.Health < 25 && e.FrameLastSeen == frame);
                if (cyclone != null)
                {
                    action = commander.Order(frame, Abilities.ATTACK, null, cyclone.Unit.Tag);
                    return true;
                }
                var prism = commander.UnitCalculation.NearbyAllies.FirstOrDefault(a => (a.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || a.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING) && Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position) <= 25);
                if (prism != null)
                {
                    action = commander.Order(frame, Abilities.SMART, null, prism.Unit.Tag);
                    return true;
                }
                cyclone = commander.UnitCalculation.EnemiesInRange.FirstOrDefault(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_CYCLONE && e.FrameLastSeen == frame);
                if (cyclone != null)
                {
                    var avoidPoint = GetPositionFromRange(commander, commander.UnitCalculation.Unit.Pos, cyclone.Unit.Pos, 9);
                    if (MapDataService.MapHeight(avoidPoint) != MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos))
                    {
                        action = commander.Order(frame, Abilities.ATTACK, null, cyclone.Unit.Tag);
                        return true;
                    }
                }
                if (Retreat(commander, defensivePoint, defensivePoint, frame, out action)) { return true; }
            }
            return false;
        }

        protected virtual bool DealWithSiegedTanks(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.IsFlying) { return false; }

            if (!commander.UnitCalculation.Unit.IsHallucination && (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat || !MapDataService.InEnemyVision(commander.UnitCalculation.Unit.Pos)))
            {
                return false;
            }

            if (!commander.UnitCalculation.Unit.IsHallucination && commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                // if only siege tanks, just go on them
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.Damage > 0 && e.Unit.UnitType != (uint)UnitTypes.TERRAN_SIEGETANKSIEGED))
                {
                    return false;
                }
            }

            if (!commander.UnitCalculation.Unit.IsHallucination && WeaponReady(commander, frame)) { return false; }

            var enemySiegedTanks = commander.UnitCalculation.EnemiesInRangeOf.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED);

            var siegedCount = enemySiegedTanks.Count();
            if (siegedCount == 0)
            {
                return false;
            }

            var estimatedSiegeDps = siegedCount * 75;

            var otherEnemyAttacks = commander.UnitCalculation.EnemiesInRangeOf.Where(u => u.Unit.UnitType != (uint)UnitTypes.TERRAN_SIEGETANKSIEGED);
            var otherDps = otherEnemyAttacks.Sum(e => e.Dps);

            if (commander.UnitCalculation.Unit.IsHallucination || estimatedSiegeDps > otherDps)
            {
                var closestSiegePosition = enemySiegedTanks.OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                action = commander.Order(frame, Abilities.MOVE, new Point2D { X = closestSiegePosition.Unit.Pos.X, Y = closestSiegePosition.Unit.Pos.Y });
                return true;
            }

            return false;
        }

        public virtual Point2D GetPositionFromRange(UnitCommander commander, Point target, Point position, float range, float angleOffset = 0)
        {
            return GetPositionFromRange(target.X, target.Y, position.X, position.Y, range, angleOffset);
        }

        protected virtual Point2D GetPositionFromRange(UnitCommander commander, Point target, Vector2 position, float range, float angleOffset = 0)
        {
            return GetPositionFromRange(target.X, target.Y, position.X, position.Y, range, angleOffset);
        }

        protected virtual Point2D GetPositionFromRange(Point2D target, Point position, float range, float angleOffset = 0)
        {
            return GetPositionFromRange(target.X, target.Y, position.X, position.Y, range, angleOffset);
        }

        protected virtual Point2D GetPositionFromRange(float targetX, float targetY, float positionX, float positionY, float range, float angleOffset = 0)
        {
            var angle = Math.Atan2(targetY - positionY, positionX - targetX) + angleOffset;
            var x = range * Math.Cos(angle);
            var y = range * Math.Sin(angle);
            return new Point2D { X = targetX + (float)x, Y = targetY - (float)y };
        }

        protected virtual Point2D GetGroundAvoidPoint(UnitCommander commander, Point start, Point avoid, Point2D target, Point2D defensivePoint, float range)
        {
            var avoidPoint = GetPositionFromRange(commander, avoid, start, range);
            if (!MapDataService.PathWalkable(start, avoidPoint))
            {
                // TODO: use retreatpath
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
            if (PredictedHealth(enemyAttack) <= 0 || AvoidedUnitTypes.Contains((UnitTypes)enemyAttack.Unit.UnitType))
            {
                return false;
            }

            return true;
        }

        protected virtual bool AttackersFilter(UnitCommander commander, UnitCalculation enemyAttack)
        {
            if (PredictedHealth(enemyAttack) <= 0 || AvoidedUnitTypes.Contains((UnitTypes)enemyAttack.Unit.UnitType) || enemyAttack.Unit.BuffIds.Contains((uint)Buffs.ORACLESTASISTRAPTARGET))
            {
                return false;
            }
            return true;
        }

        protected virtual bool AirAttackersFilter(UnitCommander commander, UnitCalculation enemyAttack)
        {
            if (PredictedHealth(enemyAttack) <= 0 || AvoidedUnitTypes.Contains((UnitTypes)enemyAttack.Unit.UnitType))
            {
                return false;
            }

            return true;
        }

        protected bool InRange(Vector2 targetLocation, Vector2 unitLocation, float range, float? minRange = null)
        {
            return Vector2.DistanceSquared(targetLocation, unitLocation) <= (range * range) && (minRange is null || Vector2.DistanceSquared(targetLocation, unitLocation) >= (minRange * minRange));
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

        protected virtual bool AttackUnitsMarkedForDeath(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (!commander.UnitCalculation.DamageGround) { return false; }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var markedForDeath = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitRole == UnitRole.Die);
            if (markedForDeath != null)
            {
                if (commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.Tag == markedForDeath.UnitCalculation.Unit.Tag))
                {
                    action = commander.Order(frame, Abilities.ATTACK, targetTag: markedForDeath.UnitCalculation.Unit.Tag);
                    return true;
                }
            }
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 25)
            {
                Console.WriteLine($"markedForDeathSearch {stopwatch.ElapsedMilliseconds}");
            }

            return false;
        }

        public virtual List<SC2APIProtocol.Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }

            var unitToSupport = GetSupportTarget(commander, supportTargets, target, defensivePoint);

            if (unitToSupport == null)
            {
                return Attack(commander, target, defensivePoint, groupCenter, frame);
            }

            var supportPoint = GetSupportSpot(commander, unitToSupport.UnitCalculation, target, defensivePoint);
            var formation = GetDesiredFormation(commander);
            var bestTarget = GetBestTarget(commander, unitToSupport, supportPoint, frame);

            if (!commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame))
            {
                if (commander.UnitCalculation.Unit.IsFlying)
                {
                    return commander.Order(frame, Abilities.MOVE, new Point2D { X = unitToSupport.UnitCalculation.Position.X, Y = unitToSupport.UnitCalculation.Position.Y });
                }

                if (GetHighGroundVisionSupport(commander, target, defensivePoint, bestTarget, unitToSupport, frame, out action)) { return action; }

                if (AttackUnitsMarkedForDeath(commander, frame, out action)) { return action; }

                if (formation == Formation.Loose)
                {
                    if (GetInFormation(commander, formation, frame, out action)) { return action; }
                }

                return commander.Order(frame, Abilities.MOVE, targetTag: unitToSupport.UnitCalculation.Unit.Tag);
            }

            if (SpecialCaseMove(commander, supportPoint, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            if (PreOffenseOrder(commander, supportPoint, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (AvoidTargettedOneHitKills(commander, supportPoint, defensivePoint, frame, out action)) { return action; }

            if (OffensiveAbility(commander, supportPoint, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (ShouldStayOutOfRange(commander, frame))
            {
                if (SpecialCaseRetreat(commander, supportPoint, defensivePoint, frame, out action)) { return action; }
                if (MoveAway(commander, supportPoint, defensivePoint, frame, out action)) { return action; }
            }

            bool weaponReady = WeaponReady(commander, frame);

            if (weaponReady && commander.UnitCalculation.EnemiesInRange.Any() || (bestTarget != null && bestTarget.EnemiesInRange.Any(e => supportTargets.Any(s => s.UnitCalculation.Unit.Tag == e.Unit.Tag))))
            {
                if (AttackBestTarget(commander, supportPoint, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }
            }

            if (AvoidPointlessDamage(commander, target, defensivePoint, formation, frame, out action)) { return action; }

            if (DoNotSuicide(commander, supportPoint, defensivePoint, frame, out action)) { return action; }

            var distanceSquredToSupportUnit = Vector2.DistanceSquared(unitToSupport.UnitCalculation.Position, commander.UnitCalculation.Position);
            // don't initiate the attack, just defend yourself and the support target
            // TODO: only attack unitToSupport.UnitCalculation.EnemiesInRangeOf
            if (weaponReady && distanceSquredToSupportUnit < MaximumSupportDistanceSqaured && unitToSupport.UnitCalculation.EnemiesInRangeOf.Any())
            {
                if (AttackBestTarget(commander, supportPoint, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }
            }

            if (distanceSquredToSupportUnit > MaximumSupportDistanceSqaured)
            {
                bestTarget = null;
            }

            if (distanceSquredToSupportUnit < 25 && !unitToSupport.UnitCalculation.Unit.IsFlying)
            {
                return new List<SC2APIProtocol.Action>(); // don't crowd the support unit
            }

            if (Move(commander, supportPoint, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            if (AvoidDeceleration(commander, supportPoint, true, frame, out action)) { return action; }
            return commander.Order(frame, Abilities.ATTACK, supportPoint);
        }

        protected virtual bool ShouldStayOutOfRange(UnitCommander commander, int frame)
        {
            return MicroPriority == MicroPriority.StayOutOfRange;
        }

        protected virtual bool DoNotSuicide(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.TargetPriorityCalculation.Overwhelm || MicroPriority == MicroPriority.AttackForward || commander.UnitCalculation.Unit.IsHallucination) { return false; }

            if (commander.UnitCalculation.EnemiesThreateningDamage.Any() || commander.UnitCalculation.EnemiesInRangeOfAvoid.Any())
            {
                if ((commander.UnitCalculation.Unit.IsFlying && commander.UnitCalculation.TargetPriorityCalculation.AirWinnability < 1) ||
                    (!commander.UnitCalculation.Unit.IsFlying && commander.UnitCalculation.TargetPriorityCalculation.GroundWinnability < 1) ||
                    commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax / 2)
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
            }
            return false;
        }

        public virtual UnitCommander GetSupportTarget(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint)
        {
            // support targets are ordered before they're passed in
            return supportTargets.FirstOrDefault();
        }

        protected virtual Point2D GetSupportSpot(UnitCommander commander, UnitCalculation unitToSupport, Point2D target, Point2D defensivePoint)
        {
            if (commander.UnitCalculation.Unit.UnitType == unitToSupport.Unit.UnitType && commander.UnitCalculation.Unit.IsFlying)
            {
                return new Point2D { X = unitToSupport.Position.X, Y = unitToSupport.Position.Y };
            }

            if (commander.UnitCalculation.Range < unitToSupport.Range)
            {
                return new Point2D { X = unitToSupport.Position.X, Y = unitToSupport.Position.Y };
            }

            var angle = Math.Atan2(unitToSupport.Position.Y - defensivePoint.Y, defensivePoint.X - unitToSupport.Position.X);
            var x = commander.UnitCalculation.Range * Math.Cos(angle);
            var y = commander.UnitCalculation.Range * Math.Sin(angle);

            var supportPoint = new Point2D { X = unitToSupport.Position.X + (float)x, Y = unitToSupport.Position.Y - (float)y };
            if (MapDataService.MapHeight(supportPoint) != MapDataService.MapHeight(unitToSupport.Unit.Pos))
            {
                supportPoint = new Point2D { X = unitToSupport.Position.X, Y = unitToSupport.Position.Y };
            }

            return supportPoint;
        }

        protected virtual UnitCalculation GetBestTarget(UnitCommander commander, UnitCommander unitToSupport, Point2D target, int frame)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.FirstOrDefault(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK);

            var range = commander.UnitCalculation.Range;

            var attacks = commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType == DisplayType.Visible && AttackersFilter(commander, u)); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Any())
            {
                var oneShotKills = attacks.Where(a => PredictedHealth(a) < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData) && !a.Unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD));
                if (oneShotKills.Any())
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

            attacks = unitToSupport.UnitCalculation.EnemiesInRangeOf.Where(enemyAttack => enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation, enemyAttack) && !InRange(enemyAttack.Position, commander.UnitCalculation.Position, range + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius) && AttackersFilter(commander, enemyAttack));
            if (attacks.Any())
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

            attacks = commander.UnitCalculation.EnemiesInRangeOf.Where(enemyAttack => enemyAttack.Unit.DisplayType == DisplayType.Visible && DamageService.CanDamage(commander.UnitCalculation, enemyAttack) && !InRange(enemyAttack.Position, commander.UnitCalculation.Position, range + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius) && AttackersFilter(commander, enemyAttack));
            if (attacks.Any())
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
            if (commander.UnitCalculation.Loaded) { return action; }

            var bestTarget = GetBestHarassTarget(commander, target);

            if (ContinueInRangeAttack(commander, frame, out action)) { return action; }

            if (SpecialCaseMove(commander, target, defensivePoint, null, bestTarget, Formation.Normal, frame, out action)) { return action; }
            if (PreOffenseOrder(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
            if (AvoidTargettedOneHitKills(commander, target, defensivePoint, frame, out action)) { return action; }
            if (OffensiveAbility(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }

            if (WeaponReady(commander, frame))
            {
                if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action)) { return action; }
                if (bestTarget != null && bestTarget.UnitClassifications.Contains(UnitClassification.Worker))
                {
                    if (ShouldStayOutOfRange(commander, frame) && AvoidAllDamage(commander, target, defensivePoint, frame, out action)) { return action; }

                    if (AttackBestTarget(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
                }
            }

            if (AvoidEnemiesThreateningDamage(commander, target, defensivePoint, frame, true, out action)) { return action; }
            if (AvoidArmyEnemies(commander, target, defensivePoint, frame, true, out action)) { return action; }

            var formation = GetDesiredFormation(commander);
            if (Move(commander, target, defensivePoint, null, bestTarget, formation, frame, out action)) { return action; }

            return MoveToTarget(commander, target, frame);
        }

        public virtual UnitCalculation GetBestHarassTarget(UnitCommander commander, Point2D target)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            var range = commander.UnitCalculation.Range;

            var attacks = new List<UnitCalculation>(commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType != DisplayType.Hidden && u.UnitClassifications.Contains(UnitClassification.Worker) && AttackersFilter(commander, u))); // units that are in range right now

            UnitCalculation bestAttack = null;
            if (attacks.Count > 0)
            {
                var oneShotKills = attacks.Where(a => PredictedHealth(a) < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData));
                if (oneShotKills.Any())
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
            if (commander.UnitCalculation.Loaded) { return action; }

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, null, Formation.Normal, frame, out action)) { return action; }

            if (PreOffenseOrder(commander, target, defensivePoint, groupCenter, null, frame, out action)) { return action; }

            if (commander.UnitCalculation.NearbyEnemies.Any(e => (e.FrameLastSeen > (frame - (SharkyOptions.FramesPerSecond * 60)) || e.Attributes.Contains(SC2APIProtocol.Attribute.Structure)) && DamageService.CanDamage(e, commander.UnitCalculation)))
            {
                if (commander.RetreatPathFrame + 2 < frame)
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

            return targetPosition + targetVelocity * t;
        }

        public virtual void UpdateState(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame)
        {
            if (commander.CommanderState == CommanderState.Stuck)
            {
                if (Vector2.DistanceSquared(commander.StuckCheck.StuckPosition, commander.UnitCalculation.Position) > 4)
                {
                    commander.CommanderState = CommanderState.None;
                }
            }
            else
            {
                if (commander.StuckCheck.AttemptFrame != 0 && commander.StuckCheck.AttemptFrame + 500 < frame && Vector2.DistanceSquared(commander.StuckCheck.StuckPosition, commander.UnitCalculation.Position) < 4)
                {
                    commander.CommanderState = CommanderState.Stuck;
                }
            }
        }

        public override string ToString()
        {
            return $"{MicroPriority} {(GroupUpEnabled ? "group up" : string.Empty)}";
        }

        public List<SC2APIProtocol.Action> AttackWithinArea(UnitCommander commander, List<Point2D> area, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }

            var formation = GetDesiredFormation(commander);

            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();
            var attacks = commander.UnitCalculation.NearbyEnemies.Where(e => DamageService.CanDamage(commander.UnitCalculation, e) && area.Any(point => Vector2.DistanceSquared(e.Position, point.ToVector2()) < commander.UnitCalculation.Range * commander.UnitCalculation.Range));
            var bestTarget = GetBestTargetFromList(commander, attacks, existingAttackOrder);

            UpdateState(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame);

            if (GroupUpBasedOnState(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            if (PreOffenseOrder(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (AvoidTargettedOneHitKills(commander, target, defensivePoint, frame, out action)) { return action; }

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (ShouldStayOutOfRange(commander, frame))
            {
                return AttackStayOutOfRange(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame);
            }

            if (TargetEnemyMainFirst && MapDataService.LastFrameVisibility(TargetingData.EnemyMainBasePoint) < 5 * 60 * SharkyOptions.FramesPerSecond)
            {
                target = TargetingData.EnemyMainBasePoint;
            }

            if (AvoidPointlessDamage(commander, target, defensivePoint, formation, frame, out action)) { return action; }

            if (WeaponReady(commander, frame))
            {
                if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action)) { return action; }
                if (GetInFormation(commander, formation, frame, out action)) { return action; }
                if (AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; } // TODO: best target within area?
            }

            var outOfAreaAvoids = commander.UnitCalculation.EnemiesThreateningDamage.Where(e => !area.Any(point => Vector2.DistanceSquared(e.Position, point.ToVector2()) < commander.UnitCalculation.Range * commander.UnitCalculation.Range));
            if (outOfAreaAvoids.Any())
            {
                if (AvoidDamageList(commander, target, target, outOfAreaAvoids, frame, false, out action)) { return action; }
            }

            if (MoveToAttackOnCooldown(commander, bestTarget, target, defensivePoint, frame, out action)) { return action; } // TODO: within area

            if (Move(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; } // TODO: within area

            if (AvoidDeceleration(commander, target, true, frame, out action)) { return action; }
            return commander.Order(frame, Abilities.ATTACK, target);
        }

        public virtual bool ContinueInRangeAttack(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (WeaponReady(commander, frame) && commander.LastTargetTag > 0 && commander.LastAbility == Abilities.ATTACK && commander.LastInRangeAttackFrame == commander.LastOrderFrame)
            {
                var enemy = commander.UnitCalculation.NearbyEnemies.FirstOrDefault(e => e.Unit.Tag == commander.LastTargetTag);

                var attackOrder = commander.UnitCalculation.Unit.Orders.FirstOrDefault(o => o.AbilityId == (uint)Abilities.ATTACK_ATTACK);
                if (attackOrder == null && frame - commander.LastOrderFrame < 3)
                {
                    if (enemy != null)
                    {
                        enemy.IncomingDamage += GetDamage(commander.UnitCalculation.Weapons, enemy.Unit, enemy.UnitTypeData);
                    }
                    action = new List<SC2APIProtocol.Action>();
                    return true;
                }

                if (attackOrder != null && frame - commander.LastOrderFrame < 6)
                {
                    if (enemy != null)
                    {
                        enemy.IncomingDamage += GetDamage(commander.UnitCalculation.Weapons, enemy.Unit, enemy.UnitTypeData);
                    }
                    action = new List<SC2APIProtocol.Action>();
                    return true;
                }
            }

            return false;
        }

        public virtual Point2D GetAttackPosition(UnitCommander commander, UnitCalculation bestTarget, int frame)
        {
            return bestTarget.Position.ToPoint2D();
        }

        public virtual float GetDesiredAttackRange(UnitCommander commander, UnitCalculation bestTarget, int frame)
        {
            return commander.UnitCalculation.Range + commander.UnitCalculation.Unit.Radius + bestTarget.Unit.Radius;
        }
    }
}
