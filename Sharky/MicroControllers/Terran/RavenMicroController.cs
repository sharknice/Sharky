namespace Sharky.MicroControllers.Terran
{
    public class RavenMicroController : FlyingDetectorMicroController
    {
        public RavenMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (InterferenceMatrix(commander, frame, bestTarget, out action))
            {
                ChatService.TagAbility("interference");
                return true;
            }

            if (AntiArmorMissile(commander, frame, bestTarget, out action))
            {
                ChatService.TagAbility("armormissile");
                return true;
            }

            if (AutoTurret(commander, frame, bestTarget, out action))
            {
                ChatService.TagAbility("autoturret");
                return true;
            }

            return false;
        }

        bool InterferenceMatrix(UnitCommander commander, int frame, UnitCalculation bestTarget, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Energy >= 75)
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
                        action = commander.Order(frame, Abilities.INTERFERENCEMATRIX, targetTag: target.Unit.Tag);
                        return true;
                    }
                }
            }

            return false;
        }

        bool AntiArmorMissile(UnitCommander commander, int frame, UnitCalculation bestTarget, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Energy >= 75)
            {

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
