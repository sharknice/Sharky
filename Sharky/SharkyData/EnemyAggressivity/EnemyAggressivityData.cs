using System.Collections.Generic;

namespace Sharky
{ 
    public class EnemyAggressivityData
    {
        /// <summary>
        /// If true, enemy is close enough to one of our bases with harassing unit(s). Harassing is considered only with small unit groups.
        /// </summary>
        public bool IsHarassing => IsGroundHarassing || IsAirHarassing;
        public bool IsGroundHarassing { get; set; }
        public bool IsAirHarassing { get; set; }
        public List<UnitCalculation> HarassingUnits { get; set; }

        /// <summary>
        /// 0 means no enemy army or no enemy aggressivity (enemy army is in his base), 1 means enemy has army next to our base.
        /// </summary>
        public float ArmyAggressivity { get; set; }

        /// <summary>
        /// Grid with distances from enemy and self base.
        /// </summary>
        public DistanceGrid DistanceGrid { get; set; }

        public override string ToString()
        {
            return $"{ArmyAggressivity}";
        }
    }
}
