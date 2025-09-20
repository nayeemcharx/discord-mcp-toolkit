using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using DiscordMcp.Core;

namespace DiscordMcp.Tools
{

    [McpTool("Discord")]
    public class ReadTextChannelMessages : IMcpTool
    {
        public string Name => "read_text_channel_messages";
        
        public string Description => "Read recent messages from a Discord text channel";
        
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
                limit = new
                {
                    type = "integer",
                    description = "Number of recent messages to retrieve (default: 10, max: 100)",
                    minimum = 1,
                    maximum = 100
                }
            },
            required = new[] { "channelId" }
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

                if (!ulong.TryParse(channelIdElement.GetString(), out var channelId))
                {
                    return new
                    {
                        success = false,
                        error = "Invalid channelId format"
                    };
                }

                // Get limit with default value of 10
                int limit = 10;
                if (arguments.TryGetProperty("limit", out var limitElement))
                {
                    limit = limitElement.GetInt32();
                    if (limit < 1 || limit > 100)
                    {
                        return new
                        {
                            success = false,
                            error = "Limit must be between 1 and 100"
                        };
                    }
                }

                
                var channel = bot.Client.GetChannel(channelId) as ITextChannel;
                if (channel == null)
                {
                    return new { success = false, error = "Text channel not found or bot doesn't have access" };
                }

                var messages = await channel.GetMessagesAsync(limit).FlattenAsync();
                
                var messageList = messages.Select(m => new
                {
                    id = m.Id,
                    content = m.Content,
                    author = new
                    {
                        id = m.Author.Id,
                        username = m.Author.Username,
                        discriminator = m.Author.Discriminator,
                        displayName = m.Author.GlobalName ?? m.Author.Username,
                        isBot = m.Author.IsBot,
                        avatarUrl = m.Author.GetAvatarUrl()
                    },
                    timestamp = m.Timestamp,
                    editedTimestamp = m.EditedTimestamp,
                    messageType = m.Type.ToString(),
                    isPinned = m.IsPinned,
                    mentionsEveryone = m.MentionedEveryone,
                    mentionedUsers = m.MentionedUserIds.ToArray(),
                    mentionedRoles = m.MentionedRoleIds.ToArray(),
                    attachments = m.Attachments.Select(a => new 
                    { 
                        id = a.Id, 
                        filename = a.Filename, 
                        size = a.Size, 
                        url = a.Url,
                        contentType = a.ContentType
                    }).ToArray(),
                    embeds = m.Embeds.Select(e => new
                    {
                        title = e.Title,
                        description = e.Description,
                        url = e.Url,
                        color = e.Color?.RawValue,
                        timestamp = e.Timestamp,
                        footerText = e.Footer?.Text,
                        authorName = e.Author?.Name,
                        fields = e.Fields.Select(f => new { name = f.Name, value = f.Value, inline = f.Inline }).ToArray()
                    }).ToArray(),
                    reactions = m.Reactions.Select(r => new
                    {
                        emote = r.Key.Name,
                        count = r.Value.ReactionCount
                    }).ToArray()
                }).OrderByDescending(m => m.timestamp).ToList();

                var messagesInfo = new
                {
                    channelId = channel.Id,
                    channelName = channel.Name,
                    guildId = channel.Guild.Id,
                    guildName = channel.Guild.Name,
                    requestedLimit = limit,
                    actualCount = messageList.Count,
                    messages = messageList
                };
                return new { 
                    success = true,
                    data = messagesInfo
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

