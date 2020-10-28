using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers
{
    public class TargetingManager : ITargetingManager
    {
        public Point2D AttackPoint { get; private set; }
        public Point2D DefensePoint { get; private set; }

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var location in gameInfo.StartRaw.StartLocations)
            {
                AttackPoint = location;
            }
            foreach (var unit in observation.Observation.RawData.Units.Where(u => u.Alliance == Alliance.Self))
            {
                DefensePoint = new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
                return;
            }
        }

        public void OnEnd(ResponseObservation observation, Result result)
        {
        }

        public IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            return new List<Action>();
        }
    }
}
