namespace Sharky.MicroControllers.Protoss
{
    public class HighTemplarMicroController : IndividualMicroController
    {
        private int StormRangeSquared = 100; // little extra range for splash
        private double StormRadius = 1.5;
        protected int FeedbackRangeSquared = 121; // actually range 10, but give an extra 1 range to get first feedback in
        private int lastStormFrame = 0;

        protected Dictionary<ulong, int> FeedBacks = new Dictionary<ulong, int>();

        public HighTemplarMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return true; }

            if (commander.UnitCalculation.Unit.Shield < 20)
            {
                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            if (commander.UnitCalculation.Unit.Energy < 40 ||
                (!SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.PSISTORMTECH) && !commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.Energy > 10) && // stay in the back if can't use spells on anything
                commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)) && commander.UnitCalculation.NearbyAllies.Count(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) > 2))
            {
                if (Retreat(commander, target, defensivePoint, frame, out action)) { return true; }
            }

            return false;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (Storm(commander, frame, out action))
            {
                TagService.TagAbility("storm");
                return true;
            }

            if (Feedback(commander, frame, out action))
            {
                TagService.TagAbility("feedback");
                return true;
            }

            if (Merge(commander, frame, out action))
            {
                TagService.TagAbility("merge_archon");
                return true;
            }

            return false;
        }

        bool Merge(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy > 40 || commander.UnitCalculation.NearbyEnemies.Count() == 0)
            {
                return false;
            }

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.MORPH_ARCHON || o.AbilityId == (uint)Abilities.MORPH_ARCHON2))
            {
                return true;
            }

            var otherHighTemplar = commander.UnitCalculation.NearbyAllies.Take(25).Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR && a.Unit.Energy <= 40);

            if (otherHighTemplar.Any())
            {
                var target = otherHighTemplar.OrderBy(o => Vector2.DistanceSquared(o.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                if (target != null)
                {
                    var merge = commander.Merge(target.Unit.Tag);
                    if (merge != null)
                    {
                        commander.UnitRole = UnitRole.Morph;
                        action = new List<SC2Action> { merge };
                    }
                    return true;
                }
            }

            return false;
        }

        bool Feedback(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy < 50)
            {
                return false;
            }

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_FEEDBACK) || commander.LastAbility == Abilities.EFFECT_FEEDBACK && commander.LastOrderFrame + 10 > frame) 
            {
                var tag = commander.UnitCalculation.Unit.Orders.FirstOrDefault(o => o.AbilityId == (uint)Abilities.EFFECT_FEEDBACK);
                if (tag != null)
                {
                    FeedBacks[tag.TargetUnitTag] = frame;
                }
                return true;
            }

            RemoveExpiredFeedbacks(frame);

            var vector = commander.UnitCalculation.Position;
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.Energy > 1 && e.FrameLastSeen == frame && !FeedBacks.ContainsKey(e.Unit.Tag) && Vector2.DistanceSquared(e.Position, vector) < FeedbackRangeSquared).OrderByDescending(e => e.Unit.Energy).ThenBy(e => Vector2.DistanceSquared(e.Position, vector));

            var oneShotKill = enemiesInRange.Where(e => e.Unit.Energy * .5 > e.Unit.Health + e.Unit.Shield).FirstOrDefault();
            if (oneShotKill != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_FEEDBACK, null, oneShotKill.Unit.Tag);
                return true;
            }
            var target = enemiesInRange.FirstOrDefault(e => e.Unit.UnitType != (uint)UnitTypes.ZERG_OVERSEER && e.Unit.Energy > 50);
            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.KillDetection)
            {
                var detector = enemiesInRange.FirstOrDefault(e => e.UnitClassifications.Contains(UnitClassification.Detector));
                if (detector != null)
                {
                    target = detector;
                }
            }

            if (target != null && target.Unit.Energy >= 50)
            {
                FeedBacks[target.Unit.Tag] = frame;
                CameraManager.SetCamera(target.Unit.Pos);
                action = commander.Order(frame, Abilities.EFFECT_FEEDBACK, null, target.Unit.Tag);
                return true;
            }

            return false;
        }

        private void RemoveExpiredFeedbacks(int frame)
        {
            var expired = FeedBacks.Where(f => f.Value + 10 < frame).Select(f => f.Key).ToList();
            foreach (var tag in expired)
            {
                FeedBacks.Remove(tag);
            }
        }

        bool Storm(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy < 75 || !SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.PSISTORMTECH))
            {
                return false;
            }

            if (!commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_PSISTORM) && !commander.UnitCalculation.EnemiesThreateningDamage.Any())
            {
                if (!commander.AbilityOffCooldown(Abilities.EFFECT_PSISTORM, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
                {
                    return false;
                }

                if (lastStormFrame >= frame - 5)
                {
                    return false;
                }
            }

            var enemies = commander.UnitCalculation.NearbyEnemies.Where(a => !a.Attributes.Contains(SC2Attribute.Structure) && !a.Unit.BuffIds.Contains((uint)Buffs.PSISTORM) && !a.Unit.BuffIds.Contains((uint)Buffs.ORACLESTASISTRAPTARGET) && a.Unit.UnitType != (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT && Vector2.DistanceSquared(commander.UnitCalculation.Position, a.Position) < StormRangeSquared).OrderByDescending(u => u.Unit.Health);
            if (enemies.Count() > 2)
            {
                var bestAttack = GetBestAttack(commander.UnitCalculation, enemies);

                if (bestAttack != null)
                {
                    CameraManager.SetCamera(bestAttack);
                    action = commander.Order(frame, Abilities.EFFECT_PSISTORM, bestAttack);
                    lastStormFrame = frame;
                    return true;
                }
            }

            return false;
        }

        protected Point2D GetBestAttack(UnitCalculation potentialAttack, IEnumerable<UnitCalculation> enemies)
        {
            var killCounts = new Dictionary<Point, float>();
            foreach (var enemyAttack in enemies)
            {
                int hitCount = 0;
                foreach (var splashedEnemy in enemyAttack.NearbyAllies.Where(a => !a.Attributes.Contains(SC2Attribute.Structure) && !a.Unit.BuffIds.Contains((uint)Buffs.PSISTORM)))
                {
                    if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + StormRadius) * (splashedEnemy.Unit.Radius + StormRadius))
                    {
                        hitCount++;
                    }
                }
                foreach (var splashedAlly in potentialAttack.NearbyAllies.Where(a => !a.Attributes.Contains(SC2Attribute.Structure)))
                {
                    if (Vector2.DistanceSquared(splashedAlly.Position, enemyAttack.Position) < (splashedAlly.Unit.Radius + StormRadius) * (splashedAlly.Unit.Radius + StormRadius))
                    {
                        hitCount-=3;
                    }
                }
                killCounts[enemyAttack.Unit.Pos] = hitCount;
            }

            var best = killCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            if (best.Value < 3 && !(potentialAttack.EnemiesThreateningDamage.Any() && potentialAttack.Unit.Shield < potentialAttack.Unit.ShieldMax)) // only attack if going to hit >= 3 units
            {
                return null;
            }
            return new Point2D { X = best.Key.X, Y = best.Key.Y };
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2Action> actions = null;
            if (OffensiveAbility(commander, defensivePoint, defensivePoint, groupCenter, null, frame, out actions))
            {
                return actions;
            }

            return base.Retreat(commander, defensivePoint, groupCenter, frame);
        }
    }
}
