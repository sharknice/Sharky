using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sharky.Chat
{
    public interface IChatDataService
    {
        List<ChatData> DefaultChataData { get; }
        List<string> GetChatTypeMessage(ChatTypeData chatTypeData, string enemyName);
        public ChatTypeData GetChatTypeData(string chatType);
        public string GetChatMessage(ChatData chatData, Match matchData, string enemyName);
    }
}
