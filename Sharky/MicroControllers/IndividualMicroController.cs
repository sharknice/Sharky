using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers
{
    public class IndividualMicroController : IIndividualMicroController
    {
        protected MapDataService MapDataService;

        public IndividualMicroController(MapDataService mapDataService)
        {
            MapDataService = mapDataService;
        }

        public List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            // TODO: all the SHarkMicroController.cs stuff

            return new List<SC2APIProtocol.Action>();
        }

        public List<SC2APIProtocol.Action> Idle(UnitCommander commanders, Point2D target, Point2D defensivePoint, int frame)
        {
            return new List<SC2APIProtocol.Action>();
        }

        public List<SC2APIProtocol.Action> Retreat(UnitCommander commanders, Point2D defensivePoint, int frame)
        {
            return new List<SC2APIProtocol.Action>();
        }

        protected Formation GetDesiredFormation(UnitCalculation unitCalculation)
        {
            if (unitCalculation.Unit.IsFlying)
            {
                if (MapDataService.GetCells(unitCalculation.Unit.Pos.X, unitCalculation.Unit.Pos.Y, 5).Any(e => e.EnemyAirSplashDpsInRange > 0))
                {
                    return Formation.Loose;
                }
                else
                {
                    return Formation.Normal;
                }
            }

            var zerglingDps = unitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLING || e.Unit.UnitType == (uint)UnitTypes.ZERG_ZERGLINGBURROWED).Sum(e => e.Dps)
            var splashDps = unitCalculation.NearbyEnemies.Where(e => UnitTypes.GroundSplashDamagers.Contains(e.Unit.UnitType)).Sum(e => e.Dps);

            if (zerglingDps > splashDps)
            {
                return Formation.Tight;
            }
            if (splashDps > 0)
            {
                return Formation.Loose;
            }

            return Formation.Normal;
        }
    }
}
