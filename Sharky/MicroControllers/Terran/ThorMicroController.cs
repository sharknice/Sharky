using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Terran
{
    public class ThorMicroController : IndividualMicroController
    {
        EnemyData EnemyData;

        public ThorMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            EnemyData = defaultSharkyBot.EnemyData;
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown == 0 || commander.UnitCalculation.Unit.WeaponCooldown > 2; // a thor has multiple attacks, don't cancel the animation early
        }

        protected override bool AvoidPointlessDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (EnemyData.EnemyRace == Race.Protoss)
            {
                ChatService.Tag("a_high_impact");
                action = commander.Order(frame, Abilities.MORPH_THORHIGHIMPACTMODE);
                return true;
            }
            else if (EnemyData.EnemyRace == Race.Terran)
            {
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_BATTLECRUISER))
                {
                    ChatService.Tag("a_high_impact");
                    action = commander.Order(frame, Abilities.MORPH_THORHIGHIMPACTMODE);
                    return true;
                }
            }

            return false;
        }
    }
}
