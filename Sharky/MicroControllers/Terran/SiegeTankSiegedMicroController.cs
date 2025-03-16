namespace Sharky.MicroControllers.Terran
{
    public class SiegeTankSiegedMicroController : IndividualMicroController
    {
        int LastUnseigeFrame = -1000;

        public SiegeTankSiegedMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        public override List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            var formation = GetDesiredFormation(commander);
            var bestTarget = GetBestTarget(commander, target, frame);

            UpdateState(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame);

            if (PreOffenseOrder(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (WeaponReady(commander, frame))
            {
                if (AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }
            }

            if (AvoidLiberationZones(commander, target, defensivePoint, frame, out action)) { return action; }

            return action;
        }

        protected override bool AvoidLiberationZones(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.EnemiesInRange.Count(e => !e.Attributes.Contains(SC2Attribute.Structure) && e.EnemiesInRange.Any(u => u.Unit.Tag == commander.UnitCalculation.Unit.Tag)) > 0)
            {
                return false;
            }

            foreach (var effect in SharkyUnitData.Effects)
            {
                if (effect.EffectId == (uint)Effects.LIBERATIONZONE)
                {
                    if (Vector2.DistanceSquared(new Vector2(effect.Pos[0].X, effect.Pos[0].Y), commander.UnitCalculation.Position) <= (effect.Radius + commander.UnitCalculation.Unit.Radius) * (effect.Radius + commander.UnitCalculation.Unit.Radius))
                    {
                        action = commander.Order(frame, Abilities.MORPH_UNSIEGE);
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (LastUnseigeFrame + 120 > frame)
            {
                return false; // don't unseige more than one tank at a time
            }

            if (commander.UnitRole == UnitRole.Leader && LastUnseigeFrame + 500 > frame)
            {
                if (!commander.UnitCalculation.EnemiesInRange.Any(e => e.FrameLastSeen > frame - 25))
                {
                    LastUnseigeFrame = frame;
                    CameraManager.SetCamera(commander.UnitCalculation.Position);
                    action = commander.Order(frame, Abilities.MORPH_UNSIEGE);
                    return true;
                }
            }

            // if there are nearby enemies keep some tanks seieged to cover as the others move forward
            if (commander.UnitCalculation.NearbyEnemies.Any(e => !e.Unit.IsFlying && e.Damage > 0)) 
            {
                var unseiged = commander.UnitCalculation.NearbyAllies.Count(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK);
                var seiged = commander.UnitCalculation.NearbyAllies.Count(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED);

                if (unseiged > 0 && seiged <= unseiged) // keep more tanks sieged than unseiged
                {               
                    if (!TargetingData.ChokePoints.Bad.Any(b => Vector2.DistanceSquared(b.Center, commander.UnitCalculation.Position) < 9) && !TargetingData.ChokePoints.Good.Any(b => Vector2.DistanceSquared(b.Center, commander.UnitCalculation.Position) < 9))
                    {
                        return false; // don't return false if on a ramp
                    }
                }
            }

            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame && !e.Unit.IsFlying && e.DamageGround && e.PreviousUnitCalculation != null && Vector2.DistanceSquared(e.PreviousUnitCalculation.Position, commander.UnitCalculation.Position) > Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)))
            {
                return false;
            }

            if (commander.UnitCalculation.EnemiesInRange.Count(e => e.Damage > 0 || Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < commander.UnitCalculation.Range * commander.UnitCalculation.Range) == 0) // get a little bit closer to buildings
            {
                if (commander.UnitCalculation.NearbyEnemies.Any(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < Vector2.DistanceSquared(new Vector2(e.PreviousUnit.Pos.X, e.PreviousUnit.Pos.Y), commander.UnitCalculation.Position)))
                {
                    return false;
                }
                LastUnseigeFrame = frame;
                action = commander.Order(frame, Abilities.MORPH_UNSIEGE);
                return true;
            }

            return false;
        }

        protected override UnitCalculation GetBestDpsReduction(UnitCommander commander, Weapon weapon, IEnumerable<UnitCalculation> primaryTargets, IEnumerable<UnitCalculation> secondaryTargets)
        {
            float innerSplashRadius = 0.4687f;
            float middleSplashRadius = 0.7812f;
            float outerSplashRadius = 1.25f;
            var dpsReductions = new Dictionary<ulong, float>();
            foreach (var enemyAttack in primaryTargets.Where(e => !e.Unit.IsFlying))
            {
                float dpsReduction = 0;
                foreach (var splashedEnemy in secondaryTargets.Where(e => !e.Unit.IsFlying))
                {
                    if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + innerSplashRadius) * (splashedEnemy.Unit.Radius + innerSplashRadius))
                    {
                        dpsReduction += splashedEnemy.Dps / GetDamage(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]);
                    }
                    else if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + middleSplashRadius) * (splashedEnemy.Unit.Radius + middleSplashRadius))
                    {
                        dpsReduction += splashedEnemy.Dps / (GetDamage(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]) * .5f);
                    }
                    else if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + outerSplashRadius) * (splashedEnemy.Unit.Radius + outerSplashRadius))
                    {
                        dpsReduction += splashedEnemy.Dps / (GetDamage(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]) * .25f);
                    }
                }
                foreach (var splashedFriendly in commander.UnitCalculation.NearbyAllies.Take(25).Where(e => !e.Unit.IsFlying))
                {
                    if (Vector2.DistanceSquared(splashedFriendly.Position, enemyAttack.Position) < (splashedFriendly.Unit.Radius + innerSplashRadius) * (splashedFriendly.Unit.Radius + innerSplashRadius))
                    {
                        dpsReduction -= splashedFriendly.Dps / GetDamage(weapon, splashedFriendly.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedFriendly.Unit.UnitType]);
                    }
                    else if (Vector2.DistanceSquared(splashedFriendly.Position, enemyAttack.Position) < (splashedFriendly.Unit.Radius + middleSplashRadius) * (splashedFriendly.Unit.Radius + middleSplashRadius))
                    {
                        dpsReduction -= splashedFriendly.Dps / (GetDamage(weapon, splashedFriendly.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedFriendly.Unit.UnitType]) * .5f);
                    }
                    else if (Vector2.DistanceSquared(splashedFriendly.Position, enemyAttack.Position) < (splashedFriendly.Unit.Radius + outerSplashRadius) * (splashedFriendly.Unit.Radius + outerSplashRadius))
                    {
                        dpsReduction -= splashedFriendly.Dps / (GetDamage(weapon, splashedFriendly.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedFriendly.Unit.UnitType]) * .25f);
                    }
                }
                dpsReductions[enemyAttack.Unit.Tag] = dpsReduction;
            }

            var best = dpsReductions.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            return primaryTargets.FirstOrDefault(t => t.Unit.Tag == best);
        }

        public override List<SC2APIProtocol.Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var supportTarget = supportTargets.FirstOrDefault();
            if (supportTarget != null)
            {
                if (!commander.UnitCalculation.EnemiesInRange.Any() && Vector2.Distance(commander.UnitCalculation.Position, supportTarget.UnitCalculation.Position) > 15)
                {
                    return commander.Order(frame, Abilities.MORPH_UNSIEGE);
                }
            }
            return Attack(commander, target, defensivePoint, groupCenter, frame);
        }

        public override bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action))
            {
                return true;
            }

            return false;
        }

        public override void UpdateState(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame)
        {
            if (commander.CommanderState == CommanderState.Grouping)
            {
                commander.CommanderState = CommanderState.None;
            }
        }

        public override bool SpecialCaseMove(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        public override bool AvoidTargetedOneHitKills(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        protected override bool AvoidTargetedDamage(UnitCommander commander, Point2D target, UnitCalculation bestTarget, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        public override bool Retreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(defensivePoint.X, defensivePoint.Y)) > 36 || MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) < MapDataService.MapHeight(defensivePoint) ||
                (TargetingData.ChokePoints.Bad.Any(b => Vector2.DistanceSquared(b.Center, commander.UnitCalculation.Position) < 9) || TargetingData.ChokePoints.Good.Any(b => Vector2.DistanceSquared(b.Center, commander.UnitCalculation.Position) < 9)
))
            {
                if (OffensiveAbility(commander, null, defensivePoint, groupCenter, null, frame, out action)) { return action; }
            }
            else
            {
                return Idle(commander, defensivePoint, frame);
            }

            return action;
        }
    }
}
