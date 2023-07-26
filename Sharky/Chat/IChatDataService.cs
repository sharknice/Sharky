namespace Sharky.Chat
{
    public interface IChatDataService
    {
        List<ChatData> DefaultChataData { get; }
        List<string> GetChatTypeMessage(ChatTypeData chatTypeData, string enemyName);
        ChatTypeData GetChatTypeData(string chatType);
        List<string> GetChatMessage(ChatData chatData, Match matchData, string enemyName);
    }
}
