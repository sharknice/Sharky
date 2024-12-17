namespace Sharky.MicroControllers.Terran
{
    public class GhostMicroController : IndividualMicroController
    {
        int LastEmpFrame = -1000;
        int LastSnipeFrame = -1000;

        float EmpRange = 10f;
        float EmpRadius = 1.5f;
        float SnipeRange = 10f;

        public GhostMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return true; }

            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.PERSONALCLOAKING))
            {
                if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.GHOSTCLOAK)) // already cloaked
                {
                    if (!commander.UnitCalculation.NearbyEnemies.Any() && commander.UnitCalculation.NearbyAllies.Any(a => a.Attributes.Contains(SC2Attribute.Structure)))
                    {
                        action = commander.Order(frame, Abilities.BEHAVIOR_CLOAKOFF);
                        return true;
                    }

                    return false;
                }

                if (commander.UnitCalculation.Unit.Energy > 30 && (commander.UnitCalculation.EnemiesInRangeOf.Any() || commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR))) // if enemies can hit it, cloak
                {
                    TagService.TagAbility("ghost_cloak");
                    action = commander.Order(frame, Abilities.BEHAVIOR_CLOAKON);
                    return true;
                }
            }

            return false;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (EMP(commander, frame, out action))
            {
                TagService.TagAbility("EMP");
                return true;
            }

            if (Snipe(commander, frame, out action))
            {
                TagService.TagAbility("snipe");
                return true;
            }
            
            return false;
        }

        bool EMP(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy < 75)
            {
                return false;
            }


            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_EMP) || commander.LastAbility == Abilities.EFFECT_EMP && commander.LastOrderFrame + 10 > frame)
            {
                return true;
            }

            if (LastEmpFrame + 10 > frame)
            {
                return false;
            }

            var vector = commander.UnitCalculation.Position;
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.Energy >= 50 && !e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && e.FrameLastSeen == frame && Vector2.Distance(e.Position, vector) <= EmpRange + EmpRadius).OrderByDescending(e => e.Unit.Energy).ThenBy(e => Vector2.DistanceSquared(e.Position, vector));

            foreach ( var enemy in enemiesInRange)
            {
                if (enemy.Unit.Energy >= 75 || enemy.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR)
                {
                    LastEmpFrame = frame;
                    CameraManager.SetCamera(enemy.Position);
                    action = commander.Order(frame, Abilities.EFFECT_EMP, enemy.Position.ToPoint2D());
                    return true;
                }
                if (enemy.NearbyAllies.Any(a => Vector2.Distance(a.Position, enemy.Position) <= EmpRadius && (a.Unit.Energy > 25 || a.Unit.Shield > 75)))
                {
                    LastEmpFrame = frame;
                    CameraManager.SetCamera(enemy.Position);
                    action = commander.Order(frame, Abilities.EFFECT_EMP, enemy.Position.ToPoint2D());
                    return true;
                }
            }

            enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.Shield >= 75 && e.FrameLastSeen == frame && Vector2.Distance(e.Position, vector) <= EmpRange + EmpRadius).OrderByDescending(e => e.Unit.Shield).ThenBy(e => Vector2.DistanceSquared(e.Position, vector));

            foreach (var enemy in enemiesInRange)
            {
                if (enemy.NearbyAllies.Where(a => Vector2.Distance(a.Position, enemy.Position) <= EmpRadius).Sum(e => Math.Max(e.Unit.Shield, 100f)) > 500)
                {
                    LastEmpFrame = frame;
                    CameraManager.SetCamera(enemy.Position);
                    action = commander.Order(frame, Abilities.EFFECT_EMP, enemy.Position.ToPoint2D());
                    return true;
                }
            }

            return false;
        }

        bool Snipe(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy < 50)
            {
                return false;
            }

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_GHOSTSNIPE) || commander.LastAbility == Abilities.EFFECT_GHOSTSNIPE && commander.LastOrderFrame + 10 > frame)
            {
                return true;
            }

            if (LastSnipeFrame + 10 > frame && LastEmpFrame + 10 > frame)
            {
                return false;
            }

            var vector = commander.UnitCalculation.Position;
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.Energy >= 50 && e.Attributes.Contains(SC2APIProtocol.Attribute.Biological) && e.FrameLastSeen == frame && Vector2.Distance(e.Position, vector) <= SnipeRange).OrderByDescending(e => e.Unit.Energy).ThenBy(e => Vector2.DistanceSquared(e.Position, vector));

            foreach (var enemy in enemiesInRange)
            {
                if (enemy.Unit.Energy >= 75 || enemy.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR || enemy.Unit.UnitType == (uint)UnitTypes.ZERG_INFESTOR || enemy.Unit.UnitType == (uint)UnitTypes.ZERG_INFESTORBURROWED)
                {
                    LastSnipeFrame = frame;
                    CameraManager.SetCamera(enemy.Position);
                    action = commander.Order(frame, Abilities.EFFECT_GHOSTSNIPE, targetTag: enemy.Unit.Tag);
                    return true;
                }
            }

            foreach (var enemy in enemiesInRange)
            {
                if (enemy.Unit.Health > 100)
                {
                    LastSnipeFrame = frame;
                    CameraManager.SetCamera(enemy.Position);
                    action = commander.Order(frame, Abilities.EFFECT_GHOSTSNIPE, targetTag: enemy.Unit.Tag);
                    return true;
                }
            }

            return false;
        }
    }
}
