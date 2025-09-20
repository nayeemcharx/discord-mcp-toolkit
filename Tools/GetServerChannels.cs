using System.Text.Json;
using System.Threading.Tasks;
using DiscordMcp.Core;

namespace DiscordMcp.Tools
{
    [McpTool("Discord")]
    public class GetServerChannels : IMcpTool
    {
        public string Name => "discord_get_server_channels";
        
        public string Description => "Get all channels in a Discord server with their IDs";
        
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

                var channelsInfo = await Task.Run(() =>
                {
                    var guild = bot.Client.GetGuild(guildId);
                    if (guild == null)
                    {
                        return null;
                    }

                    var textChannels = guild.TextChannels
                        .Select(c => new { 
                            id = c.Id, 
                            name = c.Name, 
                            type = "text",
                            categoryId = c.CategoryId,
                            position = c.Position,
                            topic = c.Topic,
                            isNsfw = c.IsNsfw
                        })
                        .ToList();

                    var voiceChannels = guild.VoiceChannels
                        .Select(c => new { 
                            id = c.Id, 
                            name = c.Name, 
                            type = "voice",
                            categoryId = c.CategoryId,
                            position = c.Position,
                            userLimit = c.UserLimit,
                            bitrate = c.Bitrate
                        })
                        .ToList();

                    var categoryChannels = guild.CategoryChannels
                        .Select(c => new { 
                            id = c.Id, 
                            name = c.Name, 
                            type = "category",
                            position = c.Position
                        })
                        .ToList();

                    var forumChannels = guild.ForumChannels
                        .Select(c => new { 
                            id = c.Id, 
                            name = c.Name, 
                            type = "forum",
                            categoryId = c.CategoryId,
                            position = c.Position,
                            topic = c.Topic,
                            isNsfw = c.IsNsfw
                        })
                        .ToList();

                    var stageChannels = guild.StageChannels
                        .Select(c => new { 
                            id = c.Id, 
                            name = c.Name, 
                            type = "stage",
                            categoryId = c.CategoryId,
                            position = c.Position,
                            userLimit = c.UserLimit,
                            bitrate = c.Bitrate
                        })
                        .ToList();

                    return new
                    {
                        guildId = guild.Id,
                        guildName = guild.Name,
                        textChannels,
                        voiceChannels,
                        categoryChannels,
                        forumChannels,
                        stageChannels,
                        totalChannels = textChannels.Count + voiceChannels.Count + categoryChannels.Count + forumChannels.Count + stageChannels.Count
                    };
                });

                if (channelsInfo == null)
                {
                    return new
                    {
                        success = false,
                        error = "Server not found or bot is not a member of this server"
                    };
                }

                return new { 
                    success = true,
                    channels = channelsInfo
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

