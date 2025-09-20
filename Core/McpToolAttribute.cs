using System;

namespace DiscordMcp.Core
{
    /// <summary>
    /// Attribute to mark classes as MCP tools for auto-registration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class McpToolAttribute : Attribute
    {
        public string? Category { get; set; }
        public bool Enabled { get; set; } = true;
        
        public McpToolAttribute(string? category = null)
        {
            Category = category;
        }
    }
}