using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.MicroControllers
{
    public interface IIndividualMicroController
    {
        List<Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame);
        List<Action> Retreat(UnitCommander commanders, Point2D defensivePoint, int frame);
        List<Action> Idle(UnitCommander commanders, Point2D target, Point2D defensivePoint, int frame);
    }
}
