using System.Collections.Generic;

namespace Sharky.Managers
{
    public interface IChatManager
    {
        void SendChatMessage(string message, bool instant = false);
        void SendChatType(string chatType, bool instant = false);
        void SendChatMessages(IEnumerable<string> messages);
        void SendDebugChatMessage(string message);
        void SendDebugChatMessages(IEnumerable<string> messages);
    }
}
