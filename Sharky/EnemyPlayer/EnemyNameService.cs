namespace Sharky.EnemyPlayer
{
    public class EnemyNameService : IEnemyNameService
    {
        public string GetNameFromGame(Game game, List<EnemyPlayer> enemies)
        {
            foreach (var chat in game.EnemyChat)
            {
                var name = GetNameFromChat(chat.Value, enemies);
                if (name != string.Empty)
                {
                    return name;
                }
            }
            return string.Empty;
        }

        public string GetEnemyNameFromId(string id, List<EnemyPlayer> enemies)
        {
            var enemy = enemies.FirstOrDefault(e => e.Id == id);
            if (enemy != null && id != null)
            {
                return enemy.Name;
            }
            return string.Empty;
        }

        public string GetNameFromChat(string chat, List<EnemyPlayer> enemies)
        {
            var enemy = enemies.Where(e => e.ChatMatches.Any(c => chat.Contains(c))).FirstOrDefault();
            if (enemy != null)
            {
                return enemy.Name;
            }

            return string.Empty;
        }
    }
}
