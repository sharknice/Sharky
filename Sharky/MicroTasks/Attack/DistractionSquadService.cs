namespace Sharky.MicroTasks.Attack
{
    public class DistractionSquadService
    {
        TargetingData TargetingData;
        BaseData BaseData;
        IMicroController MicroController;

        public bool Enabled { get; set; }
        public List<UnitCommander> DistractionSquad { get; set; }
        public List<DesiredUnitsClaim> DistractionSquadClaim { get; set; }
        public DistractionSquadState DistractionSquadState { get; private set; }

        Point2D DistractionTarget { get; set; }
        Point2D RegroupPoint { get; set; }

        public DistractionSquadService(TargetingData targetingData, BaseData baseData, IMicroController microController)
        {
            TargetingData = targetingData;
            BaseData = baseData;
            MicroController = microController;

            DistractionSquad = new List<UnitCommander>();
            DistractionSquadClaim = new List<DesiredUnitsClaim> { new DesiredUnitsClaim(UnitTypes.PROTOSS_STALKER, 4) };
            DistractionSquadState = DistractionSquadState.NotDistracting;
            Enabled = false;
        }

        public void UpdateDistractionSquad(IEnumerable<UnitCommander> otherUnits)
        {
            if (!Enabled) { return; }

            if (DistractionSquad.Any())
            {
                var deadUnits = DistractionSquad.Count(d => !otherUnits.Any(u => d.UnitCalculation.Unit.Tag == u.UnitCalculation.Unit.Tag));
                if (deadUnits > 0)
                {
                    DistractionSquad.RemoveAll(d => !otherUnits.Any(u => d.UnitCalculation.Unit.Tag == u.UnitCalculation.Unit.Tag));
                    DistractionSquadState = DistractionSquadState.Regrouping;
                }
            }

            foreach (var commander in otherUnits.Where(c => !DistractionSquad.Any(d => d.UnitCalculation.Unit.Tag == c.UnitCalculation.Unit.Tag)))
            {
                var unitType = commander.UnitCalculation.Unit.UnitType;
                foreach (var desiredUnitClaim in DistractionSquadClaim)
                {
                    if ((uint)desiredUnitClaim.UnitType == unitType && !commander.UnitCalculation.Unit.IsHallucination && DistractionSquad.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType) < desiredUnitClaim.Count)
                    {
                        DistractionSquad.Add(commander);
                    }
                }
            }
        }

        void UpdateState()
        {
            if (!Enabled)
            {
                DistractionSquadState = DistractionSquadState.NotDistracting;
                return;
            }

            if (DistractionSquadState == DistractionSquadState.NotDistracting)
            {
                if (DistractionSquad.Count() >= DistractionSquadClaim.Sum(c => c.Count))
                {
                    DistractionSquadState = DistractionSquadState.Regrouping;
                }
            }

            if (DistractionSquadState == DistractionSquadState.Regrouping)
            {
                if (DistractionSquad.Count() >= DistractionSquadClaim.Sum(c => c.Count))
                {
                    if (RegroupPoint != null && DistractionSquad.Any(u => Vector2.DistanceSquared(new Vector2(RegroupPoint.X, RegroupPoint.Y), u.UnitCalculation.Position) < 100))
                    {
                        if (DistractionSquad.All(u => DistractionSquad.Count(d => u.UnitCalculation.NearbyAllies.Any(a => a.Unit.Tag == d.UnitCalculation.Unit.Tag)) >= DistractionSquad.Count() - 1)) // they're all next to each other grouped up, // warp prism does mining so this doesn't work
                        {
                            DistractionSquadState = DistractionSquadState.Enroute;
                        }
                    }
                }
            }

            if (DistractionSquadState == DistractionSquadState.Enroute)
            {
                if (DistractionTarget != null && DistractionSquad.Any(d => Vector2.DistanceSquared(d.UnitCalculation.Position, new Vector2(DistractionTarget.X, DistractionTarget.Y)) < 100)) // any are near target
                {
                    DistractionSquadState = DistractionSquadState.Distracting;
                }
            }

            if (DistractionSquadState == DistractionSquadState.Distracting)
            {
                if (DistractionSquad.Count() < DistractionSquadClaim.Sum(c => c.Count)) // lost a unit, go back and regroup
                {
                    DistractionSquadState = DistractionSquadState.Regrouping;
                }
            }
        }

        public List<SC2Action> TakeAction(int frame)
        {
            if (!Enabled) { return new List<SC2Action>(); };

            UpdateState();
            UpdateTarget();

            if (DistractionSquadState == DistractionSquadState.Regrouping)
            {
                // go to group up point and wait
                return MicroController.Retreat(DistractionSquad, RegroupPoint, null, frame);
            }
            else if (DistractionSquadState == DistractionSquadState.Enroute)
            {
                // TODO: follow the path to the distraction target
                return MicroController.Attack(DistractionSquad, DistractionTarget, RegroupPoint, null, frame);
            }
            else if (DistractionSquadState == DistractionSquadState.Distracting)
            {
                // attack stuff and distract, bait away from main army's target
                return MicroController.Attack(DistractionSquad, DistractionTarget, RegroupPoint, null, frame);
            }

            return new List<SC2Action>();
        }

        void UpdateTarget()
        {
            RegroupPoint = TargetingData.ForwardDefensePoint;
            var attackVector = new Vector2(TargetingData.AttackPoint.X, TargetingData.AttackPoint.Y);

            var furthestBase = BaseData.EnemyBases.Skip(1).OrderByDescending(b => Vector2.DistanceSquared(attackVector, new Vector2(b.Location.X, b.Location.Y))).FirstOrDefault();
            if (furthestBase != null)
            {
                DistractionTarget = furthestBase.MineralLineLocation;
                
            }
            else
            {
                // TODO: if it's close to the main attack point check other base locations for unkown/hidden expansions, and make sure it isn't the main base
                var baseCheck = BaseData.EnemyBaseLocations.Skip(BaseData.EnemyBases.Count()).FirstOrDefault();
                if (baseCheck != null && baseCheck.MineralLineLocation != null)
                {
                    DistractionTarget = baseCheck.MineralLineLocation;
                }
                else
                {
                    DistractionTarget = BaseData.EnemyBaseLocations.FirstOrDefault().Location;
                }
            }
        }
    }
}
