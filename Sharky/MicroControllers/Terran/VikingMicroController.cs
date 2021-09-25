using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Terran
{
    public class VikingMicroController : IndividualMicroController
    {
        public VikingMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.NearbyEnemies.Count() > 0 &&
                !commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.IsFlying) && 
                !commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageGround && e.UnitClassifications.Any(c => c == UnitClassification.ArmyUnit || c == UnitClassification.DefensiveStructure)))
            {
                action = commander.Order(frame, Abilities.MORPH_VIKINGASSAULTMODE);
                return true;
            }
            
            return false;
        }

        protected override Point2D GetSupportSpot(UnitCommander commander, UnitCommander unitToSupport, Point2D target, Point2D defensivePoint)
        {
            var angle = Math.Atan2(unitToSupport.UnitCalculation.Position.Y - defensivePoint.Y, defensivePoint.X - unitToSupport.UnitCalculation.Position.X);
            var x = -3f * Math.Cos(angle);
            var y = -3f * Math.Sin(angle);

            var supportPoint = new Point2D { X = unitToSupport.UnitCalculation.Position.X + (float)x, Y = unitToSupport.UnitCalculation.Position.Y - (float)y };

            return supportPoint;
        }
    }
}
