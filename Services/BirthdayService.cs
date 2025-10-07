// Copyright (c) MyDiscordBot. All rights reserved.

using System.Text.Json;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using MyDiscordBot.Data;
using MyDiscordBot.Models;

namespace MyDiscordBot.Services;

public class BirthdayService : IDisposable
{
  private readonly DiscordSocketClient client;
  private readonly ApplicationDbContext db;

  public BirthdayService(DiscordSocketClient client, ApplicationDbContext db)
  {
    this.client = client;
    this.db = db;
  }

  /// <summary>
  /// Sends all birthday messages for today into the birthday channel immediately (slash command handler).
  /// </summary>
  public async Task SendTodaysBirthdaysNowAsync(SocketGuild guild)
  {
    var now = DateTime.Now;
    var today = now.Date;
    var todaysBirthdays = await this.db.Birthdays
      .Where(b => b.Date.Month == today.Month && b.Date.Day == today.Day)
      .ToListAsync();

    var config = await this.db.GuildConfigs.FindAsync(guild.Id);
    if (config == null || !config.IsAnnouncementEnabled || config.AnnouncementChannelId == null)
    {
      Console.WriteLine($"No announcement channel configured for guild {guild.Name}");
      return;
    }

    var announcementChannel = guild.GetTextChannel(config.AnnouncementChannelId.Value);
    if (announcementChannel == null)
    {
      Console.WriteLine($"Announcement channel not found in guild {guild.Name}");
      return;
    }

    foreach (var birthday in todaysBirthdays)
    {
      await announcementChannel.SendMessageAsync($"🎉 Happy Birthday <@{birthday.UserId}>! 🎂");
      Console.WriteLine($"Sent birthday message for user {birthday.UserId} in guild {guild.Name}");
    }
  }

  /// <summary>
  /// Sets a user's birthday.
  /// </summary>
  /// <returns></returns>
  public async Task<bool> SetBirthdayAsync(ulong userId, DateTime birthday)
  {
    try
    {
      var existing = await this.db.Birthdays.FindAsync(userId);
      if (existing != null)
      {
        existing.Date = birthday;
      }
      else
      {
        this.db.Birthdays.Add(new Birthday(userId, birthday));
      }
      await this.db.SaveChangesAsync();
      return true;
    }
    catch
    {
      return false;
    }
  }

  /// <summary>
  /// Gets a user's birthday if it exists.
  /// </summary>
  /// <returns></returns>
  public async Task<Birthday?> GetBirthdayAsync(ulong userId)
  {
    return await this.db.Birthdays.FindAsync(userId);
  }

  /// <summary>
  /// Sets the announcement channel for a guild.
  /// </summary>
  /// <returns></returns>
  public async Task<bool> SetAnnouncementChannelAsync(ulong guildId, ulong channelId)
  {
    try
    {
      var existing = await this.db.GuildConfigs.FindAsync(guildId);
      if (existing != null)
      {
        existing.AnnouncementChannelId = channelId;
      }
      else
      {
        this.db.GuildConfigs.Add(new GuildConfig(guildId, channelId));
      }
      await this.db.SaveChangesAsync();
      return true;
    }
    catch
    {
      return false;
    }
  }

  /// <summary>
  /// Disables birthday announcements for a guild.
  /// </summary>
  /// <returns></returns>
  public async Task<bool> DisableAnnouncementsAsync(ulong guildId)
  {
    try
    {
      var config = await this.db.GuildConfigs.FindAsync(guildId);
      if (config != null)
      {
        this.db.GuildConfigs.Remove(config);
        await this.db.SaveChangesAsync();
      }
      return true;
    }
    catch
    {
      return false;
    }
  }

  /// <summary>
  /// Gets the guild configuration.
  /// </summary>
  /// <returns></returns>
  public async Task<GuildConfig?> GetGuildConfigAsync(ulong guildId)
  {
    return await this.db.GuildConfigs.FindAsync(guildId);
  }

  /// <summary>
  /// Parses a date string in MM-dd-yyyy format.
  /// </summary>
  /// <returns></returns>
  public bool TryParseBirthday(string dateString, out DateTime birthday)
  {
    return DateTime.TryParseExact(dateString, "MM-dd-yyyy", null,
        System.Globalization.DateTimeStyles.None, out birthday);
  }

  /// <summary>
  /// Checks for birthdays and sends announcements.
  /// </summary>
  public async Task CheckBirthdaysAsync()
  {
    try
    {
      var now = DateTime.Now;
      Console.WriteLine($"Birthday check triggered at {now:yyyy-MM-dd HH:mm:ss}");

      Console.WriteLine($"Current connection state: {this.client.ConnectionState}");

      var todaysBirthdays = await this.db.Birthdays
        .Where(b => b.Date.Month == now.Month && b.Date.Day == now.Day)
        .ToListAsync();
      Console.WriteLine($"Found {todaysBirthdays.Count} birthdays today");

      foreach (var birthday in todaysBirthdays)
      {
        await this.SendBirthdayAnnouncementAsync(birthday.UserId);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error checking birthdays: {ex.Message}");
    }
  }

  /// <summary>
  /// Sends birthday announcement for a specific user.
  /// </summary>
  private async Task SendBirthdayAnnouncementAsync(ulong userId)
  {
    try
    {
      var user = this.client.GetUser(userId);
      if (user == null)
      {
        return;
      }

      foreach (var guild in this.client.Guilds)
      {
        var guildUser = guild.GetUser(userId);
        if (guildUser != null)
        {
          var config = await this.db.GuildConfigs.FindAsync(guild.Id);
          if (config != null && config.IsAnnouncementEnabled)
          {
            var announcementChannel = guild.GetTextChannel(config.AnnouncementChannelId!.Value);

            if (announcementChannel != null)
            {
              await announcementChannel.SendMessageAsync($"🎉 Happy Birthday <@{userId}>! 🎂");
              Console.WriteLine($"Sent birthday message for user {userId} in guild {guild.Name}");
            }
            else
            {
              Console.WriteLine($"Configured announcement channel not found in guild {guild.Name}");
            }
          }
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error sending birthday message for user {userId}: {ex.Message}");
    }
  }

  /// <summary>
  /// Dispose resources.
  /// </summary>
  public void Dispose()
  {
    this.db.Dispose();
  }
}
