using System.Collections.Generic;

namespace Sharky.EnemyPlayer
{
    public class EnemyPlayer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> ChatMatches { get; set; }
        public List<Game> Games { get; set; }
    }
}
