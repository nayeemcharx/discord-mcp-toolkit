using System.Text.Json;

namespace DiscordMcp.Core
{
    /// <summary>
    /// Interface that all MCP tools must implement
    /// </summary>
    public interface IMcpTool
    {
        /// <summary>
        /// Tool name (used in tool calls)
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Tool description for MCP clients
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// JSON Schema for tool input parameters
        /// </summary>
        object InputSchema { get; }
        
        /// <summary>
        /// Execute the tool with given arguments
        /// </summary>
        /// <param name="bot">Discord bot service</param>
        /// <param name="arguments">Tool arguments as JsonElement</param>
        /// <returns>Tool execution result</returns>
        /// 
        Task<object> ExecuteAsync(BotService bot, JsonElement arguments);
    }
}