using System;
using System.Collections.Generic;
using System.Text;

namespace Sharky
{
    public class UnitCommander
    {
        // replaces Agent.cs
        UnitCalculation UnitCalculation;
        UnitCalculation BestTarget;

        public UnitCommander(UnitCalculation unitCalculation)
        {
            UnitCalculation = unitCalculation;
        }
    }
}
