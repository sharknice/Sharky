using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Terran
{
    public class MarauderMicroController : IndividualMicroController
    {
        public MarauderMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.STIMPACK))
            {
                if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.STIMPACK) || commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.STIMPACKMARAUDER)) // don't double stim
                {
                    return false;
                }

                if (commander.UnitCalculation.EnemiesInRange.Sum(e => e.Unit.Health + e.Unit.Shield) > 100) // stim if more than 100 hitpoints in range
                {
                    action = commander.Order(frame, Abilities.EFFECT_STIM);
                    return true;
                }
            }

            return false;
        }

        protected override bool GetInBunker(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var nearbyBunkers = commander.UnitCalculation.NearbyAllies.Take(25).Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && u.Unit.BuildProgress == 1);
            foreach (var bunker in nearbyBunkers)
            {
                if (bunker.Unit.CargoSpaceMax - bunker.Unit.CargoSpaceTaken >= UnitDataService.CargoSize((UnitTypes)commander.UnitCalculation.Unit.UnitType))
                {
                    action = commander.Order(frame, Abilities.SMART, targetTag: bunker.Unit.Tag);
                    return true;
                }
            }

            return false;
        }

        protected override float GetMovementSpeed(UnitCommander commander)
        {
            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.STIMPACK) || commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.STIMPACKMARAUDER))
            {
                return base.GetMovementSpeed(commander) + 1.57f;
            }
            return base.GetMovementSpeed(commander);
        }

        protected override float GetWeaponCooldown(UnitCommander commander, UnitCalculation enemy)
        {
            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.STIMPACK) || commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.STIMPACKMARAUDER))
            {
                return SharkyOptions.FramesPerSecond * 0.71f;
            }

            return base.GetWeaponCooldown(commander, enemy);       
        }
    }
}
