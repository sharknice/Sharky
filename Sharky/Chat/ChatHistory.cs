using System.Collections.Generic;

namespace Sharky.Chat
{
    public class ChatHistory
    {
        public Dictionary<int, string> EnemyChatHistory { get; set; }
        public Dictionary<int, string> MyChatHistory { get; set; }
    }
}
