using System.Text.Json;
using DotNetEnv;
using DiscordMcp.Core;

namespace DiscordMcp
{
    class Program
    {
        private static ToolRegistry _toolRegistry = new();
        private static BotService? _bot;
        private static CancellationTokenSource _cancellationTokenSource = new();
        
        static async Task Main(string[] args)
        {
            // Handle graceful shutdown
            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true;
                _cancellationTokenSource.Cancel();
            };

            try
            {
                Env.Load();
                string token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")
                               ?? throw new Exception("Please set DISCORD_BOT_TOKEN");

                // Initialize bot
                _bot = new BotService();
                await _bot.StartAsync(token);

                // Auto-discover tools
                Console.Error.WriteLine("Discovering MCP tools...");
                _toolRegistry.DiscoverTools();

                var reader = new StreamReader(Console.OpenStandardInput());
                var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

                Console.Error.WriteLine("MCP Discord server started, waiting for requests...");

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            Console.Error.WriteLine("Input stream closed, shutting down...");
                            break;
                        }
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            await Task.Delay(10, _cancellationTokenSource.Token);
                            continue;
                        }

                        Console.Error.WriteLine($"Received: {line}");

                        var request = JsonSerializer.Deserialize<JsonRpcRequest>(line);
                        if (request == null) continue;

                        switch (request.Method)
                        {
                            case "initialize":
                                Console.Error.WriteLine("Handling initialize request");
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new JsonRpcResponse
                                {
                                    Id = request.Id,
                                    Result = new
                                    {
                                        protocolVersion = "2024-11-05",
                                        capabilities = new
                                        {
                                            tools = new { }
                                        },
                                        serverInfo = new
                                        {
                                            name = "MCP-Discord",
                                            version = "1.0.0"
                                        }
                                    }
                                }));
                                break;

                            case "notifications/initialized":
                                Console.Error.WriteLine("Received initialized notification");
                                break;

                            case "tools/list":
                                Console.Error.WriteLine($"Handling tools/list request - returning {_toolRegistry.Count} tools");
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new JsonRpcResponse
                                {
                                    Id = request.Id,
                                    Result = new
                                    {
                                        tools = _toolRegistry.GetToolDefinitions()
                                    }
                                }));
                                break;

                            case "tools/call":
                                Console.Error.WriteLine("Handling tool call");
                                var toolName = request.Params.GetProperty("name").GetString()!;
                                var arguments = request.Params.GetProperty("arguments");

                                var result = await _toolRegistry.ExecuteToolAsync(toolName, _bot, arguments);

                                await writer.WriteLineAsync(JsonSerializer.Serialize(new JsonRpcResponse
                                {
                                    Id = request.Id,
                                    Result = new 
                                    { 
                                        content = new[] 
                                        { 
                                            new 
                                            { 
                                                type = "text", 
                                                text = JsonSerializer.Serialize(result) 
                                            } 
                                        } 
                                    }
                                }));
                                break;

                            default:
                                Console.Error.WriteLine($"Unknown method: {request.Method}");
                                if (request.Id.HasValue)
                                {
                                    await writer.WriteLineAsync(JsonSerializer.Serialize(new JsonRpcResponse
                                    {
                                        Id = request.Id,
                                        Error = new { code = -32601, message = "Method not found" }
                                    }));
                                }
                                break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Console.Error.WriteLine("Operation cancelled, shutting down...");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error processing request: {ex}");
                        // Continue processing other requests unless it's a critical error
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal error: {ex}");
            }
            finally
            {
                Console.Error.WriteLine("Cleaning up...");
                await Cleanup();
            }
        }

        private static async Task Cleanup()
        {
            try
            {
                if (_bot != null)
                {
                    Console.Error.WriteLine("Stopping Discord bot...");
                    await _bot.StopAsync();
                    _bot = null;
                }
                
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                
                Console.Error.WriteLine("Cleanup completed");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error during cleanup: {ex}");
            }
        }
    }
}