namespace Sharky.MicroControllers.Zerg
{
    public class OverseerMicroController : FlyingDetectorMicroController
    {
        public OverseerMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            return Contaminate(commander, frame, out action) || Changeling(commander, frame, out action);
        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2Action> action)
        {
            return Changeling(commander, frame, out action);
        }

        private bool Contaminate(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (commander.UnitCalculation.Unit.Energy >= 125)
            {
                var activeBuilding = commander.UnitCalculation.NearbyEnemies.Take(25).FirstOrDefault(e => e.Unit.IsActive 
                && e.Attributes.Contains(SC2Attribute.Structure) 
                && !e.Unit.BuffIds.Contains((uint)Buffs.CONTAMINATED) 
                && e.Unit.UnitType != (int)UnitTypes.ZERG_CREEPTUMOR
                && e.Unit.UnitType != (int)UnitTypes.ZERG_CREEPTUMORBURROWED
                && e.Unit.UnitType != (int)UnitTypes.ZERG_CREEPTUMORQUEEN);
                
                if (activeBuilding != null)
                {
                    CameraManager.SetCamera(activeBuilding.Position);
                    TagService.TagAbility("contaminate");
                    action = commander.Order(frame, Abilities.EFFECT_CONTAMINATE, targetTag: activeBuilding.Unit.Tag);
                    return true;
                }
            }

            action = null;
            return false;
        }

        private bool Changeling(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (commander.UnitCalculation.Unit.Energy >= 170)
            {
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                TagService.TagAbility("changeling");
                action = commander.Order(frame, Abilities.EFFECT_SPAWNCHANGELING);
                return true;
            }

            action = null;
            return false;
        }
    }
}
