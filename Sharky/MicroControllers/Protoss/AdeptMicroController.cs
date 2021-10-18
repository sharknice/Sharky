using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Protoss
{
    public class AdeptMicroController : IndividualMicroController
    {
        public AdeptMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (target == defensivePoint && commander.UnitCalculation.NearbyEnemies.Count() == 0) 
            { 
                return false; 
            }

            if (commander.AbilityOffCooldown(Abilities.EFFECT_ADEPTPHASESHIFT, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                action = commander.Order(frame, Abilities.EFFECT_ADEPTPHASESHIFT, target);
                return true;
            }

            return false;
        }
    }
}
