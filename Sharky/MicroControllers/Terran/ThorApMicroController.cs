﻿namespace Sharky.MicroControllers.Terran
{
    public class ThorApMicroController : ThorMicroController
    {
        public ThorApMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            return false;
        }
    }
}
