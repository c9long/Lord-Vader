// Copyright (c) MyDiscordBot. All rights reserved.

using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyDiscordBot.Commands;
using MyDiscordBot.Data;
using MyDiscordBot.Services;
using Quartz;
using System;

namespace MyDiscordBot;

public class Bot
{
  private readonly DiscordSocketClient client;
  private readonly IServiceProvider services;
  private readonly string token;

  public Bot(string token)
  {
    this.token = token;
    this.services = this.ConfigureServices();
    this.client = this.services.GetRequiredService<DiscordSocketClient>();

    this.SetupEventHandlers();
  }

  /// <summary>
  /// Configures dependency injection services.
  /// </summary>
  private IServiceProvider ConfigureServices()
  {
    var services = new ServiceCollection()
        .AddLogging()
        .AddSingleton<DiscordSocketClient>()
        .AddDbContext<ApplicationDbContext>(options =>
          options.UseSqlite("Data Source=birthdays.db"))
        .AddSingleton<BirthdayService>()
        .AddSingleton<BirthdayCommands>()
        .AddSingleton<PingCommands>()
        .AddSingleton<PingService>()
        .AddQuartz(q =>
        {
          var jobKey = new JobKey("BirthdayJob");
          q.AddJob<BirthdayJob>(opts => opts.WithIdentity(jobKey));
          q.AddTrigger(opts => opts
              .ForJob(jobKey)
              .WithIdentity("BirthdayTrigger")
              .WithCronSchedule("0 0 0 * * ?", x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/New_York")))
          );
        })
        .AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

    return services.BuildServiceProvider();
  }

  /// <summary>
  /// Sets up Discord client event handlers.
  /// </summary>
  private void SetupEventHandlers()
  {
    this.client.Log += this.LogAsync;
    this.client.Ready += this.ReadyAsync;
    this.client.SlashCommandExecuted += this.SlashCommandHandlerAsync;
  }

  /// <summary>
  /// Starts the bot.
  /// </summary>
  /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
  public async Task StartAsync()
  {
    await this.client.LoginAsync(TokenType.Bot, this.token);
    await this.client.StartAsync();

    // Start Quartz scheduler
    var schedulerFactory = this.services.GetRequiredService<ISchedulerFactory>();
    var scheduler = await schedulerFactory.GetScheduler();
    await scheduler.Start();

    // Start console command handler
    _ = Task.Run(this.HandleConsoleCommandsAsync);

    // Block this task until the program is closed
    await Task.Delay(-1);
  }

  /// <summary>
  /// Stops the bot and disposes resources.
  /// </summary>
  /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
  public async Task StopAsync()
  {
    var birthdayService = this.services.GetRequiredService<BirthdayService>();
    birthdayService.Dispose();

    await this.client.StopAsync();
    await this.client.DisposeAsync();

    if (this.services is IDisposable disposableServices)
    {
      disposableServices.Dispose();
    }
  }

  /// <summary>
  /// Handles Discord client ready event.
  /// </summary>
  private async Task ReadyAsync()
  {
    try
    {
      // Register slash commands
      var birthdayCommands = this.services.GetRequiredService<BirthdayCommands>();
      var birthdayCommand = birthdayCommands.GetBirthdayCommand();

      var pingCommands = this.services.GetRequiredService<PingCommands>();
      var pingCommand = pingCommands.GetPingCommand();

      // Register globally (takes up to 1 hour to appear)
      await this.client.CreateGlobalApplicationCommandAsync(birthdayCommand);
      await this.client.CreateGlobalApplicationCommandAsync(pingCommand);
      Console.WriteLine("Slash commands registered successfully!");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error registering slash commands: {ex.Message}");
    }
  }

  /// <summary>
  /// Handles slash command execution.
  /// </summary>
  private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
  {
    try
    {
      if (command.Data.Name == "birthday")
      {
        var birthdayCommands = this.services.GetRequiredService<BirthdayCommands>();
        await birthdayCommands.HandleBirthdayCommandAsync(command);
      }
      else if (command.Data.Name == "ping")
      {
        var pingCommands = this.services.GetRequiredService<PingCommands>();
        await pingCommands.HandlePingCommandAsync(command);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error handling slash command: {ex.Message}");

      // Try to respond with an error message if we haven't responded yet
      try
      {
        if (!command.HasResponded)
        {
          await command.RespondAsync("❌ An error occurred while processing your command.");
        }
      }
      catch
      {
        // If we can't respond, just log it
        Console.WriteLine("Failed to send error response to user");
      }
    }
  }

  /// <summary>
  /// Handles Discord client log messages.
  /// </summary>
  private Task LogAsync(LogMessage msg)
  {
    Console.WriteLine(msg.ToString());
    return Task.CompletedTask;
  }

  /// <summary>
  /// Handles console commands for manual bot control.
  /// </summary>
  private async Task HandleConsoleCommandsAsync()
  {
    while (true)
    {
      try
      {
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(input))
        {
          continue;
        }

        switch (input)
        {
          case "birthday":
            var birthdayService = this.services.GetRequiredService<BirthdayService>();
            await birthdayService.CheckBirthdaysAsync();
            break;

          case "status":
            Console.WriteLine($"🤖 Bot Status:");
            Console.WriteLine($"   Connection: {this.client.ConnectionState}");
            Console.WriteLine($"   Guilds: {this.client.Guilds.Count}");
            Console.WriteLine($"   Latency: {this.client.Latency}ms");
            break;

          case "quit":
          case "exit":
            Console.WriteLine("Shutting down bot...");
            await this.StopAsync();
            Environment.Exit(0);
            break;

          case "help":
            Console.WriteLine("Available console commands:");
            Console.WriteLine("  'birthday' - Manually trigger birthday check");
            Console.WriteLine("  'status' - Show bot status");
            Console.WriteLine("  'help' - Show this help message");
            Console.WriteLine("  'quit' or 'exit' - Stop the bot");
            break;

          default:
            Console.WriteLine($"Unknown command: '{input}'. Type 'help' for available commands.");
            break;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error handling console command: {ex.Message}");
      }
    }
  }
}
