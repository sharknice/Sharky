using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Zerg
{
    public class RoachMicroController : IndividualMicroController
    {
        public RoachMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        protected override bool AvoidPointlessDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<Action> action)
        {
            action = null;

            if (commander.UnitCalculation.EnemiesThreateningDamage.All(e => e.Range > 1))
            {
                return false;
            }

            return base.AvoidPointlessDamage(commander, target, defensivePoint, frame, out action);
        }
    }
}
