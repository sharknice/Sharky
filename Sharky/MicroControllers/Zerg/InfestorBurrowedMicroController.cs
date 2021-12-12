using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Zerg
{
    public class InfestorBurrowedMicroController : IndividualMicroController
    {
        private int lastFungalFrame = 0;

        public InfestorBurrowedMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.UnitCalculation.NearbyEnemies.Any() || commander.UnitCalculation.Unit.Energy >= 75)
            {
                action = commander.Order(frame, Abilities.BURROWUP);
                return true;
            }

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return true;
            }

            return false;
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return false;
        }
    }
}
