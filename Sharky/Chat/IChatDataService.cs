namespace Sharky.Chat
{
    public interface IChatDataService
    {
        List<ChatData> DefaultChataData { get; }
        List<string> GetChatTypeMessage(ChatTypeData chatTypeData, string enemyName, List<KeyValuePair<string, string>> parameters = null);
        ChatTypeData GetChatTypeData(string chatType);
        List<string> GetChatMessage(ChatData chatData, Match matchData, string enemyName);
    }
}
