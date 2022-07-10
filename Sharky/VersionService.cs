using System;

namespace Sharky
{
    public class VersionService
    {
        public Version Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        public DateTime BuildDate => new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);

        public string VersionString => $"Version {Version}, built on {BuildDate}";
    }
}
