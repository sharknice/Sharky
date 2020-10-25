using System;
using System.Collections.Generic;
using System.Text;

namespace Sharky.Builds
{
    public class BuildOptions
    {
        public bool StrictWorkerCount { get; set; }
        public bool StrictSupplyCount { get; set; }
        public bool StrictGasCount { get; set; }

        public BuildOptions()
        {
            StrictWorkerCount = false;
            StrictSupplyCount = false;
            StrictGasCount = false;
        }
    }
}
