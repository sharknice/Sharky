using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.Builds
{
    public interface ISharkyBuild
    {
        string Name();
        void StartBuild(int frame);
        void OnFrame(ResponseObservation observation);
        bool Transition();
        List<string> CounterTransition();
    }
}
