namespace Sharky
{
    public class UnitCalculation
    {
        public Unit Unit { get; set; }
        /// <summary>
        /// TODO: currently only set for enemies, allies is just the same as the current
        /// </summary>
        public Unit PreviousUnit { get; set; }
        public UnitCalculation PreviousUnitCalculation { get; set; }
        //public Dictionary<int, Unit> PreviousUnits { get; set; }
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
        public Vector2 EndPlusFive { get; set; }
        public float DamageRadius { get; set; }
        public float EstimatedCooldown { get; set; }
        public bool DamageGround { get; set; }
        public bool DamageAir { get; set; }
        public float Damage { get; set; }
        public float Dps { get; set; }
        public float Range { get; set; }
        public Weapon Weapon { get; set; }
        public List<Weapon> Weapons { get; set; }
        public float SimulatedHitpoints { get; set; }
        public float SimulatedHealPerSecond { get; set; }
        public IEnumerable<SC2APIProtocol.Attribute> Attributes { get; set; }
       
        /// <summary>
        /// Position this unit will be in the next frame
        /// </summary>
        public Vector2 Position { get; set; }
        public Vector2 Vector { get; set; }
        public float Velocity { get; set; }

        public Vector2 AverageVector { get; set; }
        public float AverageVelocity { get; set; }

        /// <summary>
        /// enemies this unit can attack
        /// </summary>
        public List<UnitCalculation> EnemiesInRange { get; set; }

        /// <summary>
        /// enemies that can hit this unit
        /// </summary>
        public List<UnitCalculation> EnemiesInRangeOf { get; set; }

        /// <summary>
        /// enemies that that can almost hit this unit
        /// </summary>
        public List<UnitCalculation> EnemiesInRangeOfAvoid { get; set; }

        /// <summary>
        /// enemies that will be able to damage this unit if it does not run away
        /// </summary>
        public List<UnitCalculation> EnemiesThreateningDamage { get; set; }

        public List<UnitCalculation> NearbyAllies { get; set; }
        public List<UnitCalculation> NearbyEnemies { get; set; }

        /// <summary>
        /// list of units attacking this unit
        /// </summary>
        public List<UnitCalculation> Attackers { get; set; }

        /// <summary>
        /// list of units Targeting this unit
        /// </summary>
        public List<UnitCalculation> Targeters { get; set; }

        public List<UnitClassification> UnitClassifications { get; set; }
        public TargetPriorityCalculation TargetPriorityCalculation { get; set; }
        public UnitTypeData UnitTypeData { get; set; }
        public int FrameLastSeen { get; set; }
        public int FrameFirstSeen { get; internal set; }

        public bool IsOnCreep { get; set; }

        /// <summary>
        /// amount of damage this unit will take from the next frame of attacks
        /// </summary>
        public float IncomingDamage { get; set; }

        public List<Unit> Repairers { get; set; }

        public bool Loaded { get; set; }

        public UnitCalculation(Unit unit, List<Unit> repairers, SharkyUnitData sharkyUnitData, SharkyOptions sharkyOptions, UnitDataService unitDataService, bool isOnCreep, int frame)
        {
            TargetPriorityCalculation = new TargetPriorityCalculation();

            //PreviousUnits = new Dictionary<int, Unit>();
            //PreviousUnits[frame] = unit;
            PreviousUnit = unit;
            Unit = unit;
            UnitTypeData = sharkyUnitData.UnitData[(UnitTypes)unit.UnitType];

            Repairers = repairers;

            IsOnCreep = isOnCreep;

            Velocity = 0;
            AverageVelocity = 0;
            Vector = Vector2.Zero;
            AverageVector = Vector2.Zero;
            Position = new Vector2(unit.Pos.X, unit.Pos.Y);

            FrameLastSeen = frame;
            FrameFirstSeen = frame;

            Range = unitDataService.GetRange(unit);

            Start = new Vector2(unit.Pos.X, unit.Pos.Y);

            if (unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS || unit.UnitType == (uint)UnitTypes.PROTOSS_IMMORTAL || unit.UnitType == (uint)UnitTypes.PROTOSS_PHOTONCANNON || unit.UnitType == (uint)UnitTypes.PROTOSS_MOTHERSHIP
                || unit.UnitType == (uint)UnitTypes.TERRAN_MISSILETURRET
                || unit.UnitType == (uint)UnitTypes.ZERG_SPORECRAWLER || unit.UnitType == (uint)UnitTypes.ZERG_SPINECRAWLER)
            {
                End = Start; // facing is always 0 for these units, can't calculate where they're aiming
                EndPlusFive = Start;
            }
            else
            {
                var endX = (float)(Range * Math.Sin(unit.Facing + (Math.PI / 2)));
                var endY = (float)(Range * Math.Cos(unit.Facing + (Math.PI / 2)));
                End = new Vector2(endX + unit.Pos.X, unit.Pos.Y - endY);
                var endFiveX = (float)((Range + 5) * Math.Sin(unit.Facing + (Math.PI / 2)));
                var endFiveY = (float)((Range + 5) * Math.Cos(unit.Facing + (Math.PI / 2)));
                EndPlusFive = new Vector2(endFiveX + unit.Pos.X, unit.Pos.Y - endFiveY);
            }

            DamageRadius = 1; // TODO: get damage radius
            EstimatedCooldown = 0; // TODO: get estimated cooldown

            DamageAir = false;
            if (unitDataService.CanAttackAir((UnitTypes)unit.UnitType))
            {
                DamageAir = true;
            }
            DamageGround = false;
            if (unitDataService.CanAttackGround((UnitTypes)unit.UnitType))
            {
                DamageGround = true;
            }
            Damage = unitDataService.GetDamage(unit);
            Dps = unitDataService.GetDps(unit);
            Weapon = unitDataService.GetWeapon(unit);
            Weapons = UnitTypeData.Weapons.ToList();
            if (Weapons == null || Weapons.Count() == 0)
            {
                Weapons = new List<Weapon>();
                if (Weapon != null)
                {
                    Weapons.Add(Weapon);
                }
            }

            SimulatedHitpoints = Unit.Health + Unit.Shield;
            if (Unit.BuffIds.Contains((uint)Buffs.IMMORTALOVERLOAD))
            {
                SimulatedHitpoints += 100;
            }
            if (unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM)
            {
                SimulatedHitpoints += 500;
            }
            if (unit.IsHallucination)
            {
                SimulatedHitpoints = SimulatedHitpoints / 2f;
            }

            if (sharkyUnitData.ZergTypes.Contains((UnitTypes)Unit.UnitType))
            {
                SimulatedHealPerSecond = 0.38f;
            }
            else if (repairers.Any() && UnitTypeData.Attributes.Contains(SC2APIProtocol.Attribute.Mechanical))
            {
                SimulatedHealPerSecond = (float)(unit.HealthMax / (UnitTypeData.BuildTime / sharkyOptions.FramesPerSecond)) * repairers.Count();
            }
            else if (Unit.UnitType == (uint)UnitTypes.TERRAN_MEDIVAC && Unit.Energy > 10)
            {
                SimulatedHealPerSecond = 12.6f;
            }
            else if (Unit.UnitType == (uint)UnitTypes.ZERG_QUEEN && Unit.Energy >= 50)
            {
                SimulatedHealPerSecond = 20;
            }
            else if (Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && unit.BuffIds.Contains((uint)Buffs.BATTERYOVERCHARGE))
            {
                SimulatedHealPerSecond = 150;
            }
            else if (Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && unit.Energy > 5)
            {
                SimulatedHealPerSecond = 50;
            }
            else
            {
                SimulatedHealPerSecond = 0;
            }

            if (Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && Unit.BuildProgress == 1) // assume 4 marines
            {
                Range = 6;
                DamageAir = true;
                DamageGround = true;
                Damage = 6;
                Dps = Damage * 4 / 0.61f;
            }

            Attributes = UnitTypeData.Attributes;

            UnitClassifications = new List<UnitClassification>();
            if (UnitTypeData.Attributes.Contains(SC2APIProtocol.Attribute.Structure))
            {
                if (sharkyUnitData.ResourceCenterTypes.Contains((UnitTypes)unit.UnitType))
                {
                    UnitClassifications.Add(UnitClassification.ResourceCenter);
                    UnitClassifications.Add(UnitClassification.ProductionStructure);
                }
                if (sharkyUnitData.DefensiveStructureTypes.Contains((UnitTypes)unit.UnitType))
                {
                    UnitClassifications.Add(UnitClassification.DefensiveStructure);
                }
            }
            else if (unit.UnitType == (uint)UnitTypes.TERRAN_SCV || unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE || unit.UnitType == (uint)UnitTypes.ZERG_DRONE)
            {
                UnitClassifications.Add(UnitClassification.Worker);
            }
            else if (unit.UnitType == (uint)UnitTypes.ZERG_QUEEN || unit.UnitType == (uint)UnitTypes.TERRAN_MULE || unit.UnitType == (uint)UnitTypes.ZERG_OVERLORD || unit.UnitType == (uint)UnitTypes.ZERG_LARVA || unit.UnitType == (uint)UnitTypes.ZERG_EGG)
            {

            }
            else if (Damage > 0 || unit.EnergyMax > 0 || unit.CargoSpaceMax > 0 || sharkyUnitData.DetectionTypes.Contains((UnitTypes)unit.UnitType) ||
                unit.UnitType == (uint)UnitTypes.ZERG_SWARMHOSTBURROWEDMP || unit.UnitType == (uint)UnitTypes.ZERG_SWARMHOSTMP || unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTOR || unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED)
            {
                UnitClassifications.Add(UnitClassification.ArmyUnit);
            }

            if (unit.UnitType == (uint)UnitTypes.ZERG_QUEEN && unit.Alliance == Alliance.Enemy)
            {
                UnitClassifications.Add(UnitClassification.ArmyUnit);
            }

            if (sharkyUnitData.DetectionTypes.Contains((UnitTypes)unit.UnitType))
            {
                UnitClassifications.Add(UnitClassification.Detector);
            }
            if (sharkyUnitData.AbilityDetectionTypes.Contains((UnitTypes)unit.UnitType))
            {
                UnitClassifications.Add(UnitClassification.DetectionCaster);
            }
            if (sharkyUnitData.CloakableAttackers.Contains((UnitTypes)unit.UnitType))
            {
                UnitClassifications.Add(UnitClassification.Cloakable);
            }

            EnemiesInRange = new List<UnitCalculation>();
            EnemiesInRangeOf = new List<UnitCalculation>();
            EnemiesInRangeOfAvoid = new List<UnitCalculation>();
            EnemiesThreateningDamage = new List<UnitCalculation>();
            NearbyAllies = new List<UnitCalculation>();
            NearbyEnemies = new List<UnitCalculation>();
            Attackers = new List<UnitCalculation>();
            Targeters = new List<UnitCalculation>();
            IncomingDamage = 0;
            Loaded = false;
        }

        public float SimulatedDamagePerSecond(IEnumerable<SC2APIProtocol.Attribute> includedAttributes, bool air, bool ground)
        {
            if (Unit.IsHallucination)
            {
                return 0;
            }

            if (Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && Unit.BuildProgress == 1) // assume 4 marines
            {
                return 24 / 0.61f;
            }

            if (Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR && Unit.Energy > 75)
            {
                return 25 * 3; // storm does 25 damage per second, probably will hit 3 units
            }
            if (Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTOR && ground)
            {
                return 145;
            }
            if (Unit.UnitType == (uint)UnitTypes.ZERG_INFESTOR || Unit.UnitType == (uint)UnitTypes.ZERG_INFESTORBURROWED && Unit.Energy >= 75)
            {
                return 100; // fungal growth
            }
            if (Unit.UnitType == (uint)UnitTypes.ZERG_VIPER && Unit.Energy >= 75)
            {
                return 100;
            }

            if (Weapon == null || Weapon.Damage == 0) { return 0; }
            if (air && !ground && !DamageAir) { return 0; }
            if (ground && !air && !DamageGround) { return 0; }

            float damage = Damage;
            var damageBonus = Weapon.DamageBonus.FirstOrDefault(d => includedAttributes.Contains(d.Attribute));
            if (damageBonus != null)
            {
                damage += damageBonus.Bonus;
            }

            if (Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED)
            {
                damage *= 5;
            }
            if (Unit.UnitType == (uint)UnitTypes.TERRAN_PLANETARYFORTRESS)
            {
                damage *= 3;
            }
            if (Unit.UnitType == (uint)UnitTypes.TERRAN_THOR && air)
            {
                damage *= 2;
            }
            if (Unit.UnitType == (uint)UnitTypes.TERRAN_LIBERATOR)
            {
                damage *= 2;
            }
            if (Unit.UnitType == (uint)UnitTypes.TERRAN_HELLIONTANK)
            {
                damage *= 2;
            }
            if (Unit.UnitType == (uint)UnitTypes.TERRAN_HELLION)
            {
                damage *= 2;
            }
            if (Unit.UnitType == (uint)UnitTypes.TERRAN_WIDOWMINEBURROWED)
            {
                damage *= 2;
            }

            if (Unit.UnitType == (uint)UnitTypes.PROTOSS_ARCHON)
            {
                damage *= 2;
            }
            if (Unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS)
            {
                damage *= 2;
            }

            if (Unit.UnitType == (uint)UnitTypes.ZERG_BANELING || Unit.UnitType == (uint)UnitTypes.ZERG_BANELINGBURROWED)
            {
                damage *= 3.5f;
            }
            if (Unit.UnitType == (uint)UnitTypes.ZERG_ULTRALISK)
            {
                damage *= 2;
            }
            if (Unit.UnitType == (uint)UnitTypes.ZERG_LURKERMPBURROWED)
            {
                damage *= 2;
            }

            return damage / Weapon.Speed;
        }

        public void SetPreviousUnit(UnitCalculation previous, int frame)
        {
            if (FrameLastSeen == frame) { return; }

            TargetPriorityCalculation = previous.TargetPriorityCalculation;
            PreviousUnit = previous.Unit;
            PreviousUnitCalculation = previous;
            PreviousUnitCalculation.PreviousUnitCalculation = null;
            var vector = new Vector2(Unit.Pos.X - PreviousUnit.Pos.X, Unit.Pos.Y - PreviousUnit.Pos.Y);

            Vector = vector / (FrameLastSeen - frame);
            AverageVector = Vector;
            Velocity = Vector.Length();
            AverageVelocity = Velocity;
            FrameFirstSeen = previous.FrameFirstSeen;
        }

        public override string ToString()
        {
            return $"UnitCalculation: {(UnitTypes)Unit.UnitType}, tag: {Unit.Tag}, last seen: {FrameLastSeen}";
        }
    }
}
