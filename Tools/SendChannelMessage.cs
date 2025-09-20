using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using DiscordMcp.Core;

namespace DiscordMcp.Tools
{
    [McpTool("Discord")]
    public class SendMessage : IMcpTool
    {
        public string Name => "send_message";
        
        public string Description => "Send a message to a Discord text channel";
        
        public object InputSchema => new
        {
            type = "object",
            properties = new 
            {
                channelId = new
                {
                    type = "string",
                    description = "The ID of the Discord text channel"
                },
                message = new
                {
                    type = "string",
                    description = "The message content to send",
                    maxLength = 2000
                }
            },
            required = new[] { "channelId", "message" }
        };

        public async Task<object> ExecuteAsync(BotService bot, JsonElement arguments)
        {
            try
            {
                if (!arguments.TryGetProperty("channelId", out var channelIdElement))
                {
                    return new
                    {
                        success = false,
                        error = "channelId parameter is required"
                    };
                }

                if (!arguments.TryGetProperty("message", out var messageElement))
                {
                    return new
                    {
                        success = false,
                        error = "message parameter is required"
                    };
                }

                if (!ulong.TryParse(channelIdElement.GetString(), out var channelId))
                {
                    return new
                    {
                        success = false,
                        error = "Invalid channelId format"
                    };
                }

                var messageContent = messageElement.GetString();
                if (string.IsNullOrWhiteSpace(messageContent))
                {
                    return new
                    {
                        success = false,
                        error = "Message content cannot be empty"
                    };
                }

                if (messageContent.Length > 2000)
                {
                    return new
                    {
                        success = false,
                        error = "Message content exceeds Discord's 2000 character limit"
                    };
                }

                    var channel = bot.Client.GetChannel(channelId) as ITextChannel;
                    if (channel == null)
                    {
                        return new { success = false,error = "Text channel not found or bot doesn't have access" };
                    }

                    // Check if bot has permission to send messages
                    var botUser = await channel.Guild.GetUserAsync(bot.Client.CurrentUser.Id);
                    var permissions = botUser.GetPermissions(channel);
                    if (!permissions.SendMessages)
                    {
                        return new { success = false,error = "Bot doesn't have permission to send messages in this channel" };
                    }

                    try
                    {
                        var sentMessage = await channel.SendMessageAsync(messageContent);
                        
                        return new
                        {
                            success = true,
                            messageId = sentMessage.Id,
                            content = sentMessage.Content,
                            timestamp = sentMessage.Timestamp,
                            channelId = sentMessage.Channel.Id,
                            channelName = channel.Name,
                            guildId = channel.Guild.Id,
                            guildName = channel.Guild.Name,
                            author = new
                            {
                                id = sentMessage.Author.Id,
                                username = sentMessage.Author.Username,
                                displayName = sentMessage.Author.GlobalName ?? sentMessage.Author.Username,
                                isBot = sentMessage.Author.IsBot
                            }
                        };
                    }
                    catch (Discord.Net.HttpException httpEx)
                    {
                        return new { success = false,error = $"Discord API error: {httpEx.Message}" };
                    }

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

