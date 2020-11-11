using System;
using System.Collections.Generic;

namespace Sharky.Chat
{
    [Serializable]
    public class ChatRequest
    {
        public Chat chat { get; set; }
        public string conversationName { get; set; }
        public string type { get; set; }
        public DateTime? requestTime { get; set; }
        public List<string> exclusiveTypes { get; set; }
        public List<string> excludedTypes { get; set; }
        public List<string> requiredPropertyMatches { get; set; }
        public List<string> subjectGoals { get; set; }
        public dynamic metadata { get; set; }
    }
}
