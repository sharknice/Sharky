namespace Sharky.MicroControllers.Terran
{
    public class RavenMicroController : FlyingDetectorMicroController
    {
        float AntiArmorMissileRadius = 2.88f;
        int LastAntiArmorCastFrame = -1000;
        int LastInteferenceMatrixCastFrame = -1000;

        public RavenMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (AntiArmorMissile(commander, frame, bestTarget, 10, out action))
            {
                TagService.TagAbility("armormissile");
                return true;
            }

            if (InterferenceMatrix(commander, frame, bestTarget, out action))
            {
                TagService.TagAbility("interference");
                return true;
            }

            if (AntiArmorMissile(commander, frame, bestTarget, 5, out action))
            {
                TagService.TagAbility("armormissile");
                return true;
            }

            if (AutoTurret(commander, frame, bestTarget, out action))
            {
                TagService.TagAbility("autoturret");
                return true;
            }

            return false;
        }

        bool InterferenceMatrix(UnitCommander commander, int frame, UnitCalculation bestTarget, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            return false; // TODO: need the upgrade for interference matrix
            // if (!SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.RAVENCORVIDREACTOR)) { return false; } 

            if (commander.UnitCalculation.Unit.Energy >= 75 && LastInteferenceMatrixCastFrame + 25 < frame)
            {
                if (commander.UnitCalculation.NearbyAllies.Count() >= 5)
                {
                    var target = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Unit.BuffIds.Contains((uint)Buffs.INTERFERENCEMATRIX) &&
                        (e.Unit.UnitType == (uint)UnitTypes.ZERG_VIPER || e.Unit.UnitType == (uint)UnitTypes.ZERG_INFESTOR ||
                        e.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED || e.Unit.UnitType == (uint)UnitTypes.TERRAN_RAVEN ||
                        e.Unit.UnitType == (uint)UnitTypes.PROTOSS_IMMORTAL || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_CARRIER || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_TEMPEST || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING)
                        && Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) <= 100).OrderByDescending(e => e.Unit.Energy).ThenBy(e => e.Unit.Health).FirstOrDefault();
                    if (target != null)
                    {
                        CameraManager.SetCamera(target.Position);
                        action = commander.Order(frame, Abilities.INTERFERENCEMATRIX, targetTag: target.Unit.Tag);
                        LastInteferenceMatrixCastFrame = frame;
                        return true;
                    }
                }
            }

            return false;
        }

        bool AntiArmorMissile(UnitCommander commander, int frame, UnitCalculation bestTarget, int hitThreshold, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.ANTIARMORMISSILE) || (commander.LastAbility == Abilities.ANTIARMORMISSILE && commander.LastOrderFrame >= frame - 2))
            {
                return true;
            }

            if (commander.UnitCalculation.Unit.Energy >= 75 && LastAntiArmorCastFrame + 25 < frame)
            {
                var best = commander.UnitCalculation.NearbyEnemies.Where(e => !e.Unit.IsHallucination && e.Damage > 0 && !e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.Distance(commander.UnitCalculation.Position, e.Position) < 11)
                    .OrderByDescending(e =>  e.NearbyAllies.Count(a => Vector2.Distance(e.Position, a.Position) <= AntiArmorMissileRadius)).FirstOrDefault();

                if (best != null && best.NearbyAllies.Count(a => Vector2.Distance(best.Position, a.Position) <= AntiArmorMissileRadius) - best.NearbyEnemies.Count(a => Vector2.Distance(best.Position, a.Position) <= AntiArmorMissileRadius) >= hitThreshold)
                {
                    CameraManager.SetCamera(best.Position);
                    action = commander.Order(frame, Abilities.ANTIARMORMISSILE, targetTag: best.Unit.Tag);
                    LastAntiArmorCastFrame = frame;
                    return true;
                }
            }

            return false;
        }

        bool AutoTurret(UnitCommander commander, int frame, UnitCalculation bestTarget, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Energy >= 50)
            {

            }

            return false;
        }
    }
}
