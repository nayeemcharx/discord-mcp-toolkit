using System.Text.Json;
using Discord;
using DiscordMcp.Core;

namespace DiscordMcp.Tools
{
    [McpTool("Discord")]
    public class GetChannelMembers : IMcpTool
    {
        public string Name => "get_channel_members";
        
        public string Description => "Get a list of members who have access to a Discord channel";
        
        public object InputSchema => new
        {
            type = "object",
            properties = new 
            {
                channelId = new
                {
                    type = "string",
                    description = "The ID of the Discord channel"
                },
                limit = new
                {
                    type = "integer",
                    description = "Maximum number of members to return (default: 100, max: 1000)",
                    minimum = 1,
                    maximum = 1000
                },
                includeOffline = new
                {
                    type = "boolean",
                    description = "Whether to include offline members (default: true)"
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

                // Parse optional parameters
                var limit = 100;
                if (arguments.TryGetProperty("limit", out var limitElement))
                {
                    limit = limitElement.GetInt32();
                    if (limit < 1 || limit > 1000)
                    {
                        return new
                        {
                            success = false,
                            error = "Limit must be between 1 and 1000"
                        };
                    }
                }

                var includeOffline = true;
                if (arguments.TryGetProperty("includeOffline", out var includeOfflineElement))
                {
                    includeOffline = includeOfflineElement.GetBoolean();
                }

                var channel = bot.Client.GetChannel(channelId);
                if (channel == null)
                {
                    return new 
                    { 
                        success = false,
                        error = "Channel not found or bot doesn't have access" 
                    };
                }

                // Check if it's a guild channel
                if (!(channel is IGuildChannel guildChannel))
                {
                    return new 
                    { 
                        success = false,
                        error = "Channel is not a guild channel" 
                    };
                }

                var guild = guildChannel.Guild;
                
                // Check if bot has permission to view the channel
                var botUser = await guild.GetUserAsync(bot.Client.CurrentUser.Id);
                var permissions = botUser.GetPermissions(guildChannel);
                if (!permissions.ViewChannel)
                {
                    return new 
                    { 
                        success = false,
                        error = "Bot doesn't have permission to view this channel" 
                    };
                }

                try
                {
                    // Get all guild members
                    var allMembers = await guild.GetUsersAsync();
                    
                    // Filter members who can view the channel
                    var channelMembers = allMembers.Where(member =>
                    {
                        var memberPermissions = member.GetPermissions(guildChannel);
                        return memberPermissions.ViewChannel;
                    });

                    // Filter by online status if requested
                    if (!includeOffline)
                    {
                        channelMembers = channelMembers.Where(member => 
                            member.Status != UserStatus.Offline && 
                            member.Status != UserStatus.Invisible);
                    }

                    // Apply limit and convert to result format
                    var limitedMembers = channelMembers.Take(limit).Select(member => new
                    {
                        id = member.Id,
                        username = member.Username,
                        displayName = member.DisplayName,
                        globalName = member.GlobalName,
                        discriminator = member.Discriminator,
                        nickname = member.Nickname,
                        isBot = member.IsBot,
                        status = member.Status.ToString(),
                        joinedAt = member.JoinedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        roles = member.RoleIds.Select(roleId =>
                        {
                            var role = guild.GetRole(roleId);
                            return new
                            {
                                id = roleId,
                                name = role?.Name ?? "Unknown",
                                color = role?.Color.ToString() ?? "#000000",
                                position = role?.Position ?? 0
                            };
                        }).Where(r => r.name != "@everyone").ToArray(),
                        permissions = new
                        {
                            manageChannel = member.GetPermissions(guildChannel).ManageChannel,
                            sendMessages = member.GetPermissions(guildChannel).SendMessages,
                            readMessageHistory = member.GetPermissions(guildChannel).ReadMessageHistory,
                            mentionEveryone = member.GetPermissions(guildChannel).MentionEveryone
                        }
                    }).ToArray();

                    return new
                    {
                        success = true,
                        channelId = channelId,
                        channelName = guildChannel.Name,
                        guildId = guild.Id,
                        guildName = guild.Name,
                        memberCount = limitedMembers.Length,
                        totalMembersWithAccess = channelMembers.Count(),
                        includeOffline = includeOffline,
                        members = limitedMembers
                    };
                }
                catch (Discord.Net.HttpException httpEx)
                {
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