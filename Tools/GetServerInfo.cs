using System.Text.Json;
using System.Threading.Tasks;
using DiscordMcp.Core;

namespace DiscordMcp.Tools
{
    [McpTool("Discord")]
public class GetServerInfo : IMcpTool
{
    public string Name => "discord_get_server_info";
    
    public string Description => "Get detailed information about a specific Discord server";
    
    public object InputSchema => new
    {
        type = "object",
        properties = new 
        {
            guildId = new
            {
                type = "string",
                description = "The ID of the Discord server/guild"
            }
        },
        required = new[] { "guildId" }
    };

    public async Task<object> ExecuteAsync(BotService bot, JsonElement arguments)
    {
        try
        {
            if (!arguments.TryGetProperty("guildId", out var guildIdElement))
            {
                return new
                {
                    success = false,
                    error = "guildId parameter is required"
                };
            }

            if (!ulong.TryParse(guildIdElement.GetString(), out var guildId))
            {
                return new
                {
                    success = false,
                    error = "Invalid guildId format"
                };
            }

            var serverInfo = await Task.Run(() =>
            {
                var guild = bot.Client.GetGuild(guildId);
                if (guild == null)
                {
                    return null;
                }

                return new
                {
                    id = guild.Id,
                    name = guild.Name,
                    description = guild.Description,
                    memberCount = guild.MemberCount,
                    createdAt = guild.CreatedAt,
                    ownerId = guild.OwnerId,
                    iconUrl = guild.IconUrl,
                    bannerUrl = guild.BannerUrl,
                    preferredLocale = guild.PreferredLocale,
                    premiumTier = guild.PremiumTier.ToString(),
                    boostCount = guild.PremiumSubscriptionCount,
                    verificationLevel = guild.VerificationLevel.ToString(),
                    explicitContentFilter = guild.ExplicitContentFilter.ToString(),
                    textChannelCount = guild.TextChannels.Count,
                    voiceChannelCount = guild.VoiceChannels.Count,
                    categoryCount = guild.CategoryChannels.Count,
                    roleCount = guild.Roles.Count
                };
            });

            if (serverInfo == null)
            {
                return new
                {
                    success = false,
                    error = "Server not found or bot is not a member of this server"
                };
            }

            return new { 
                success = true,
                serverInfo 
            };
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

