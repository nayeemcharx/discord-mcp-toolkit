using Discord;
using Discord.WebSocket;
namespace DiscordMcp.Core
{
    public class BotService
    {
        private readonly DiscordSocketClient _client;

        public BotService()
        {
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
        }

        public async Task StartAsync(string token)
        {
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();


        }

        public DiscordSocketClient Client => _client;

        private Task LogAsync(LogMessage message)
        {
            return Task.CompletedTask;
        }
        public async Task StopAsync()
        {
            try
            {
                if (_client != null)
                {
                    await _client.LogoutAsync();
                    await _client.StopAsync();
                    _client.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error stopping Discord bot: {ex}");
            }
        }
    }

}
