namespace Sharky.MicroControllers.Protoss
{
    public class ImmortalMicroController : IndividualMicroController
    {
        public ImmortalMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        public override bool AttackBestTargetInRange(UnitCommander commander, Point2D target, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (bestTarget != null)
            {
                if (commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && bestTarget.Unit.DisplayType == DisplayType.Visible && MapDataService.SelfVisible(bestTarget.Unit.Pos))
                {
                    if (WeaponReady(commander, frame))
                    {
                        bestTarget.IncomingDamage += GetDamage(commander.UnitCalculation.Weapons, bestTarget.Unit, bestTarget.UnitTypeData);
                        action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                        commander.LastInRangeAttackFrame = frame;
                    }
                    else
                    {
                        action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    }
                    return true;
                }
                else if (WeaponReady(commander, frame) && commander.UnitCalculation.EnemiesInRange.Any(e => e.FrameLastSeen == frame && e.Unit.UnitType != (uint)UnitTypes.PROTOSS_INTERCEPTOR))
                {
                    var bestInRange = GetBestTargetFromList(commander, commander.UnitCalculation.EnemiesInRange.Where(e => e.FrameLastSeen == frame && e.Unit.UnitType != (uint)UnitTypes.PROTOSS_INTERCEPTOR), null);
                    if (bestInRange != null)
                    {
                        if (Vector2.Distance(bestTarget.Position, commander.UnitCalculation.Position) - 2 < commander.UnitCalculation.Weapon.Range && MapDataService.MapHeight(bestTarget.Position) == MapDataService.MapHeight(commander.UnitCalculation.Position))
                        {
                            action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                        }
                        else
                        {
                            action = commander.Order(frame, Abilities.ATTACK, null, bestInRange.Unit.Tag);
                            commander.LastInRangeAttackFrame = frame;
                        }
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
