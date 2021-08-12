using Sharky.DefaultBot;
using System;

namespace Sharky
{
    public class FrameToTimeConverter
    {
        SharkyOptions SharkyOptions;

        public FrameToTimeConverter(SharkyOptions sharkyOptions)
        {
            SharkyOptions = sharkyOptions;
        }

        public TimeSpan GetTime(int frame)
        {
            return TimeSpan.FromSeconds(frame / SharkyOptions.FramesPerSecond);
        }
    }
}
