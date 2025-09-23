// Copyright (c) MyDiscordBot. All rights reserved.

using DotNetEnv;
using MyDiscordBot;

internal class Program
{
  private static async Task Main(string[] args)
  {
    // Load environment variables from .env file
    Env.Load();

    // Get bot token from environment variable
    string? token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");

    if (string.IsNullOrEmpty(token))
    {
      Console.WriteLine("Error: DISCORD_BOT_TOKEN environment variable not found!");
      Console.WriteLine("Please create a .env file with your Discord bot token.");
      Console.WriteLine("Press any key to exit...");
      Console.ReadKey();
      return;
    }

    var bot = new Bot(token);

    // Handle graceful shutdown
    Console.CancelKeyPress += async (_, e) =>
    {
      e.Cancel = true;
      Console.WriteLine("Shutting down bot...");
      await bot.StopAsync();
      Environment.Exit(0);
    };

    try
    {
      Console.WriteLine("Starting Discord bot...");
      await bot.StartAsync();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error starting bot: {ex.Message}");
      Console.WriteLine("Press any key to exit...");
      Console.ReadKey();
    }
  }
}
