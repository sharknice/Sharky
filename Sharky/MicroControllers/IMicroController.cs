using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.MicroControllers
{
    public interface IMicroController
    {
        List<Action> Attack(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame);
        List<Action> Retreat(IEnumerable<UnitCommander> commanders, Point2D defensivePoint, Point2D groupCenter, int frame);
        List<Action> Idle(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, int frame);
    }
}
