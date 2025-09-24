// Copyright (c) MyDiscordBot. All rights reserved.

using Discord.WebSocket;
using MyDiscordBot.Services;
using Quartz;

namespace MyDiscordBot.Services;

public class BirthdayJob : IJob
{
  private readonly BirthdayService birthdayService;
  private readonly DiscordSocketClient client;

  public BirthdayJob(BirthdayService birthdayService, DiscordSocketClient client)
  {
    this.birthdayService = birthdayService;
    this.client = client;
  }

  public async Task Execute(IJobExecutionContext context)
  {
    try
    {
      Console.WriteLine("Executing daily birthday check job...");

      foreach (var guild in this.client.Guilds)
      {
        await this.birthdayService.SendTodaysBirthdaysNowAsync(guild);
      }

      Console.WriteLine("Daily birthday check completed.");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error in BirthdayJob: {ex.Message}");
    }
  }
}