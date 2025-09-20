using System.Text.Json;
using System.Threading.Tasks;
using DiscordMcp.Core;

namespace DiscordMcp.Tools
{
    [McpTool("Discord")]
    public class GetServers : IMcpTool
    {
        public string Name => "get_discord_servers";
        
        public string Description => "List all Discord servers the bot is in";
        
        public object InputSchema => new
        {
            type = "object",
            properties = new { },
            required = new string[0]
        };

        public async Task<object> ExecuteAsync(BotService bot, JsonElement arguments)
        {
            try
            {
            var servers = await Task.Run(() =>
            {
                var list = new List<object>();
                foreach (var guild in bot.Client.Guilds)
                {
                list.Add(new { id = guild.Id, name = guild.Name });
                }
                return (object)list;
            });

            return new { servers };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }
    }
}

