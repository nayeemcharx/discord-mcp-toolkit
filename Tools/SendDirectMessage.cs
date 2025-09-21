using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using DiscordMcp.Core;

namespace DiscordMcp.Tools
{
    [McpTool("Discord")]
    public class SendDirectMessage : IMcpTool
    {
        public string Name => "send_direct_message";
        
        public string Description => "Send a direct message to a Discord user";
        
        public object InputSchema => new
        {
            type = "object",
            properties = new 
            {
                userId = new
                {
                    type = "string",
                    description = "The ID of the Discord user to send a DM to"
                },
                message = new
                {
                    type = "string",
                    description = "The message content to send",
                    maxLength = 2000
                }
            },
            required = new[] { "userId", "message" }
        };

        public async Task<object> ExecuteAsync(BotService bot, JsonElement arguments)
        {
            try
            {
                if (!arguments.TryGetProperty("userId", out var userIdElement))
                {
                    return new
                    {
                        success = false,
                        error = "userId parameter is required"
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

                if (!ulong.TryParse(userIdElement.GetString(), out var userId))
                {
                    return new
                    {
                        success = false,
                        error = "Invalid userId format"
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

                var user = bot.Client.GetUser(userId) as IUser;
                if (user == null)
                {
                    // Try to fetch the user if not in cache
                    try
                    {
                        user = await bot.Client.Rest.GetUserAsync(userId);
                    }
                    catch (Discord.Net.HttpException)
                    {
                        return new 
                        { 
                            success = false,
                            error = "User not found or bot doesn't have access to this user" 
                        };
                    }
                }

                if (user == null)
                {
                    return new 
                    { 
                        success = false,
                        error = "User not found" 
                    };
                }

                // Check if user is a bot (optional protection)
                if (user.IsBot)
                {
                    return new 
                    { 
                        success = false,
                        error = "Cannot send direct messages to bots" 
                    };
                }

                try
                {
                    var dmChannel = await user.CreateDMChannelAsync();
                    var sentMessage = await dmChannel.SendMessageAsync(messageContent);
                    
                    return new
                    {
                        success = true,
                        messageId = sentMessage.Id,
                        content = sentMessage.Content,
                        timestamp = sentMessage.Timestamp,
                        channelId = sentMessage.Channel.Id,
                        recipient = new
                        {
                            id = user.Id,
                            username = user.Username,
                            displayName = user.GlobalName ?? user.Username,
                            discriminator = user.Discriminator,
                            isBot = user.IsBot
                        },
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
                    // Common reasons for DM failures
                    if (httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
                    {
                        return new 
                        { 
                            success = false,
                            error = "Cannot send message to this user. They may have DMs disabled or have blocked the bot." 
                        };
                    }
                    
                    return new 
                    { 
                        success = false,
                        error = $"Discord API error: {httpEx.Message}" 
                    };
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