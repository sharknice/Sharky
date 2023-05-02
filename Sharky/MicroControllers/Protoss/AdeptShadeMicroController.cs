using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class AdeptShadeMicroController : IndividualMicroController
    {
        public bool FakeHarass { get; set; }

        public AdeptShadeMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 5;
            FakeHarass = false;
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return false;
        }

        protected override bool AvoidReaperCharges(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.BuffDurationRemain > 5 || commander.ParentUnitCalculation == null) { return false; }

            if (FakeHarass && commander.UnitCalculation.NearbyEnemies.Any())
            {
                action = commander.Order(frame, Abilities.CANCEL, allowSpam: true);
                return true;
            }

            if (!commander.ParentUnitCalculation.EnemiesInRangeOfAvoid.Any(e => !e.UnitClassifications.Contains(UnitClassification.Worker)) && commander.ParentUnitCalculation.EnemiesInRange.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))
            {
                action = commander.Order(frame, Abilities.CANCEL, allowSpam: true);
                return true;
            }

            if (commander.UnitCalculation.EnemiesInRangeOfAvoid.Any(e => !e.UnitClassifications.Contains(UnitClassification.Worker)) && !commander.ParentUnitCalculation.EnemiesInRangeOfAvoid.Any(e => !e.UnitClassifications.Contains(UnitClassification.Worker)))
            {
                action = commander.Order(frame, Abilities.CANCEL, allowSpam: true);
                return true;
            }
            if (commander.ParentUnitCalculation.EnemiesInRangeOf.Count(e => !e.UnitClassifications.Contains(UnitClassification.Worker)) < commander.UnitCalculation.EnemiesInRangeOf.Count(e => !e.UnitClassifications.Contains(UnitClassification.Worker)))
            {
                action = commander.Order(frame, Abilities.CANCEL, allowSpam: true);
                return true;
            }

            var threatToShade = commander.UnitCalculation.EnemiesThreateningDamage.Where(e => !e.UnitClassifications.Contains(UnitClassification.Worker)).Sum(e => e.Damage);
            var threatToAdept = commander.ParentUnitCalculation.EnemiesThreateningDamage.Where(e => !e.UnitClassifications.Contains(UnitClassification.Worker)).Sum(e => e.Damage);
            if (threatToShade > threatToAdept)
            {
                action = commander.Order(frame, Abilities.CANCEL, allowSpam: true);
                return true;               
            }

            return false;
        }

        protected override bool Move(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return true; }

            return NavigateToTarget(commander, target, groupCenter, bestTarget, formation, frame, out action);
        }

        public override List<Action> NavigateToPoint(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            var bestTarget = GetBestHarassTarget(commander, target);
            if (PreOffenseOrder(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }

            if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(target.X, target.Y)) < 16)
            {
                if (SpecialCaseMove(commander, target, defensivePoint, null, bestTarget, Formation.Normal, frame, out action)) { return action; }
                if (AvoidAllDamage(commander, target, defensivePoint, frame, out action)) { return action; }
            }

            NavigateToTarget(commander, target, groupCenter, null, Formation.Normal, frame, out action);

            return action;
        }

        protected override bool SpecialCaseMove(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }
    }
}
