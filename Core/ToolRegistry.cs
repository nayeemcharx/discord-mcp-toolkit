using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordMcp.Core
{
    /// <summary>
    /// Automatically discovers and manages MCP tools
    /// </summary>
    public class ToolRegistry
    {
        private readonly Dictionary<string, IMcpTool> _tools = new();
        
        /// <summary>
        /// Auto-discover all tools in the assembly
        /// </summary>
        public void DiscoverTools()
        {
            var toolTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetInterface(nameof(IMcpTool)) != null)
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetCustomAttribute<McpToolAttribute>()?.Enabled != false);

            foreach (var toolType in toolTypes)
            {
                try
                {
                    var tool = (IMcpTool)Activator.CreateInstance(toolType)!;
                    _tools[tool.Name] = tool;
                    Console.Error.WriteLine($"Registered tool: {tool.Name}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to register tool {toolType.Name}: {ex.Message}");
                }
            }
            
            Console.Error.WriteLine($"Discovered {_tools.Count} tools total");
        }
        
        /// <summary>
        /// Get all registered tools for tools/list response
        /// </summary>
        public object[] GetToolDefinitions()
        {
            return _tools.Values.Select(tool => new
            {
                name = tool.Name,
                description = tool.Description,
                inputSchema = tool.InputSchema
            }).ToArray();
        }
        
        /// <summary>
        /// Execute a tool by name
        /// </summary>
        public async Task<object> ExecuteToolAsync(string toolName, BotService bot, JsonElement arguments)
        {
            if (!_tools.TryGetValue(toolName, out var tool))
            {
                return new { error = $"Tool '{toolName}' not found" };
            }
            
            try
            {
                return await tool.ExecuteAsync(bot, arguments);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Tool execution error in {toolName}: {ex}");
                return new { error = $"Tool execution failed: {ex.Message}" };
            }
        }
        
        /// <summary>
        /// Check if a tool exists
        /// </summary>
        public bool HasTool(string toolName) => _tools.ContainsKey(toolName);
        
        /// <summary>
        /// Get tool count
        /// </summary>
        public int Count => _tools.Count;
    }
}