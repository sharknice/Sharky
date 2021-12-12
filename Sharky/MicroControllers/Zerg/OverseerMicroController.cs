using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Protoss
{
    public class OverseerMicroController : FlyingDetectorMicroController
    {
        public OverseerMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Energy < 125)
            {
                return false;
            }

            var activeBuilding = commander.UnitCalculation.NearbyEnemies.Take(25).FirstOrDefault(e => e.Unit.IsActive && e.Attributes.Contains(Attribute.Structure) && !e.Unit.BuffIds.Contains((uint)Buffs.CONTAMINATED));
            if (activeBuilding != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_CONTAMINATE, targetTag: activeBuilding.Unit.Tag);
                return true;
            }

            return false;
        }
    }
}
