// Copyright (c) MyDiscordBot. All rights reserved.

using System.Text.Json;
using Discord.WebSocket;
using MyDiscordBot.Models;

namespace MyDiscordBot.Services;

public class BirthdayService
{
  private readonly DiscordSocketClient client;
  private readonly Dictionary<ulong, Birthday> birthdays = new();
  private readonly Dictionary<ulong, GuildConfig> guildConfigs = new();

  private const string BirthdayFile = "birthdays.json";
  private const string ChannelFile = "channels.json";

  public BirthdayService(DiscordSocketClient client)
  {
    this.client = client;
    this.LoadBirthdays();
    this.LoadGuildConfigs();
  }

  /// <summary>
  /// Sends all birthday messages for today into the birthday channel immediately (slash command handler).
  /// </summary>
  public async Task SendTodaysBirthdaysNowAsync(SocketGuild guild)
  {
    var now = DateTime.Now;
    var today = now.Date;
    var todaysBirthdays = this.birthdays.Values.Where(b =>
      b.Date.Month == today.Month && b.Date.Day == today.Day).ToList();

    if (!this.guildConfigs.TryGetValue(guild.Id, out var config) || !config.IsAnnouncementEnabled || config.AnnouncementChannelId == null)
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

    if (todaysBirthdays.Count == 0)
    {
      await announcementChannel.SendMessageAsync($"No birthdays today!");
      Console.WriteLine($"No birthdays found for today in guild {guild.Name}");
    }
  }

  /// <summary>
  /// Sets a user's birthday.
  /// </summary>
  /// <returns></returns>
  public bool SetBirthday(ulong userId, DateTime birthday)
  {
    try
    {
      this.birthdays[userId] = new Birthday(userId, birthday);
      this.SaveBirthdays();
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
  public Birthday? GetBirthday(ulong userId)
  {
    return this.birthdays.TryGetValue(userId, out var birthday) ? birthday : null;
  }

  /// <summary>
  /// Sets the announcement channel for a guild.
  /// </summary>
  /// <returns></returns>
  public bool SetAnnouncementChannel(ulong guildId, ulong channelId)
  {
    try
    {
      this.guildConfigs[guildId] = new GuildConfig(guildId, channelId);
      this.SaveGuildConfigs();
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
  public bool DisableAnnouncements(ulong guildId)
  {
    try
    {
      if (this.guildConfigs.ContainsKey(guildId))
      {
        this.guildConfigs.Remove(guildId);
        this.SaveGuildConfigs();
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
  public GuildConfig? GetGuildConfig(ulong guildId)
  {
    return this.guildConfigs.TryGetValue(guildId, out var config) ? config : null;
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

      var todaysBirthdays = this.birthdays.Values.Where(b => b.IsTodayTheirBirthday()).ToList();
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
        if (guildUser != null && this.guildConfigs.TryGetValue(guild.Id, out var config) && config.IsAnnouncementEnabled)
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
    catch (Exception ex)
    {
      Console.WriteLine($"Error sending birthday message for user {userId}: {ex.Message}");
    }
  }

  /// <summary>
  /// Loads birthdays from file.
  /// </summary>
  private void LoadBirthdays()
  {
    try
    {
      if (File.Exists(BirthdayFile))
      {
        var json = File.ReadAllText(BirthdayFile);
        var birthdayDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        if (birthdayDict != null)
        {
          foreach (var kvp in birthdayDict)
          {
            if (ulong.TryParse(kvp.Key, out var userId) && DateTime.TryParse(kvp.Value, out var date))
            {
              this.birthdays[userId] = new Birthday(userId, date);
            }
          }
        }

        Console.WriteLine($"Loaded {this.birthdays.Count} birthdays from file");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error loading birthdays: {ex.Message}");
    }
  }

  /// <summary>
  /// Saves birthdays to file.
  /// </summary>
  private void SaveBirthdays()
  {
    try
    {
      var birthdayDict = this.birthdays.ToDictionary(
          kvp => kvp.Key.ToString(),
          kvp => kvp.Value.Date.ToString("yyyy-MM-dd"));

      var json = JsonSerializer.Serialize(birthdayDict, new JsonSerializerOptions { WriteIndented = true });
      File.WriteAllText(BirthdayFile, json);

      Console.WriteLine("Birthdays saved to file");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error saving birthdays: {ex.Message}");
    }
  }

  /// <summary>
  /// Loads guild configurations from file.
  /// </summary>
  private void LoadGuildConfigs()
  {
    try
    {
      if (File.Exists(ChannelFile))
      {
        var json = File.ReadAllText(ChannelFile);
        var channelDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        if (channelDict != null)
        {
          foreach (var kvp in channelDict)
          {
            if (ulong.TryParse(kvp.Key, out var guildId) && ulong.TryParse(kvp.Value, out var channelId))
            {
              this.guildConfigs[guildId] = new GuildConfig(guildId, channelId);
            }
          }
        }

        Console.WriteLine($"Loaded {this.guildConfigs.Count} announcement channels from file");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error loading channels: {ex.Message}");
    }
  }

  /// <summary>
  /// Saves guild configurations to file.
  /// </summary>
  private void SaveGuildConfigs()
  {
    try
    {
      var channelDict = this.guildConfigs
          .Where(kvp => kvp.Value.IsAnnouncementEnabled)
          .ToDictionary(
              kvp => kvp.Key.ToString(),
              kvp => kvp.Value.AnnouncementChannelId!.Value.ToString());

      var json = JsonSerializer.Serialize(channelDict, new JsonSerializerOptions { WriteIndented = true });
      File.WriteAllText(ChannelFile, json);

      Console.WriteLine("Channels saved to file");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error saving channels: {ex.Message}");
    }
  }

  /// <summary>
  /// Dispose resources.
  /// </summary>
  public void Dispose()
  {
    // No resources to dispose with Quartz implementation
  }
}
