// Copyright (c) MyDiscordBot. All rights reserved.

using Discord;
using Discord.WebSocket;
using MyDiscordBot.Services;

namespace MyDiscordBot.Commands;

public class BirthdayCommands
{
  private readonly BirthdayService birthdayService;

  public BirthdayCommands(BirthdayService birthdayService)
  {
    this.birthdayService = birthdayService;
  }

  /// <summary>
  /// Registers the birthday slash command.
  /// </summary>
  /// <returns></returns>
  public SlashCommandProperties GetBirthdayCommand()
  {
    return new SlashCommandBuilder()
      .WithName("birthday")
      .WithDescription("Birthday management commands")
      .AddOption(new SlashCommandOptionBuilder()
        .WithName("set")
        .WithDescription("Set your birthday")
        .WithType(ApplicationCommandOptionType.SubCommand)
        .AddOption("date", ApplicationCommandOptionType.String, "Your birthday (MM-DD-YYYY)", isRequired: true))
      .AddOption(new SlashCommandOptionBuilder()
        .WithName("channel")
        .WithDescription("Set the birthday announcement channel")
        .WithType(ApplicationCommandOptionType.SubCommand)
        .AddOption("channel", ApplicationCommandOptionType.Channel, "Channel for birthday announcements", isRequired: true))
      .AddOption(new SlashCommandOptionBuilder()
        .WithName("disable")
        .WithDescription("Disable birthday announcements")
        .WithType(ApplicationCommandOptionType.SubCommand))
      .AddOption(new SlashCommandOptionBuilder()
        .WithName("announce-today")
        .WithDescription("Send all birthday messages for today into the birthday channel now")
        .WithType(ApplicationCommandOptionType.SubCommand))
      .Build();
  }

  /// <summary>
  /// Handles birthday slash command execution.
  /// </summary>
  /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
  public async Task HandleBirthdayCommandAsync(SocketSlashCommand command)
  {
    if (command.Data.Name != "birthday")
    {
      return;
    }

    var subCommand = command.Data.Options.First();

    switch (subCommand.Name)
    {
      case "set":
        await this.HandleSetBirthdayAsync(command, subCommand);
        break;
      case "channel":
        await this.HandleSetChannelAsync(command, subCommand);
        break;
      case "disable":
        await this.HandleDisableAsync(command);
        break;
      case "announce-today":
        await this.HandleAnnounceTodayAsync(command);
        break;
    }
  }
  /// <summary>
  /// Handles the 'announce-today' subcommand.
  /// </summary>
  private async Task HandleAnnounceTodayAsync(SocketSlashCommand command)
  {
    try
    {
      if (!command.GuildId.HasValue)
      {
        await command.RespondAsync("❌ This command can only be used in a server.");
        return;
      }

      var guild = (command.Channel as SocketGuildChannel)?.Guild;
      if (guild == null)
      {
        await command.RespondAsync("❌ Could not determine the guild context.");
        return;
      }

      await this.birthdayService.SendTodaysBirthdaysNowAsync(guild);
      await command.RespondAsync("✅ Birthday messages for today have been sent.");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error handling announce-today command: {ex.Message}");
      await command.RespondAsync("❌ An error occurred while sending today's birthday messages.");
    }
  }

  /// <summary>
  /// Handles the 'set' subcommand.
  /// </summary>
  private async Task HandleSetBirthdayAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
  {
    try
    {
      var dateString = subCommand.Options.First(x => x.Name == "date").Value.ToString()!;

      if (this.birthdayService.TryParseBirthday(dateString, out DateTime birthday))
      {
        if (this.birthdayService.SetBirthday(command.User.Id, birthday))
        {
          await command.RespondAsync($"✅ Your birthday has been set to {birthday:MMMM dd, yyyy}!");
        }
        else
        {
          await command.RespondAsync("❌ Failed to save your birthday. Please try again.");
        }
      }
      else
      {
        await command.RespondAsync("❌ Invalid date format. Please use MM-DD-YYYY (e.g., 03-15-1990)");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error handling set birthday command: {ex.Message}");
      await command.RespondAsync("❌ An error occurred while setting your birthday.");
    }
  }

  /// <summary>
  /// Handles the 'channel' subcommand.
  /// </summary>
  private async Task HandleSetChannelAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
  {
    try
    {
      // Check if user has permission to manage channels
      var guildUser = command.User as SocketGuildUser;
      if (guildUser?.GuildPermissions.ManageChannels != true)
      {
        await command.RespondAsync("❌ You need 'Manage Channels' permission to set the announcement channel.");
        return;
      }

      if (!command.GuildId.HasValue)
      {
        await command.RespondAsync("❌ This command can only be used in a server.");
        return;
      }

      var channelOption = subCommand.Options.FirstOrDefault(x => x.Name == "channel");
      if (channelOption?.Value is SocketTextChannel channel)
      {
        if (this.birthdayService.SetAnnouncementChannel(command.GuildId.Value, channel.Id))
        {
          await command.RespondAsync($"✅ Birthday announcements will now be sent to {channel.Mention}!");
        }
        else
        {
          await command.RespondAsync("❌ Failed to set the announcement channel. Please try again.");
        }
      }
      else
      {
        await command.RespondAsync("❌ Please select a valid text channel.");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error handling set channel command: {ex.Message}");
      await command.RespondAsync("❌ An error occurred while setting the announcement channel.");
    }
  }

  /// <summary>
  /// Handles the 'disable' subcommand.
  /// </summary>
  private async Task HandleDisableAsync(SocketSlashCommand command)
  {
    try
    {
      // Check if user has permission to manage channels
      var guildUser = command.User as SocketGuildUser;
      if (guildUser?.GuildPermissions.ManageChannels != true)
      {
        await command.RespondAsync("❌ You need 'Manage Channels' permission to disable birthday announcements.");
        return;
      }

      if (!command.GuildId.HasValue)
      {
        await command.RespondAsync("❌ This command can only be used in a server.");
        return;
      }

      var config = this.birthdayService.GetGuildConfig(command.GuildId.Value);
      if (config?.IsAnnouncementEnabled == true)
      {
        if (this.birthdayService.DisableAnnouncements(command.GuildId.Value))
        {
          await command.RespondAsync("✅ Birthday announcements have been disabled for this server.");
        }
        else
        {
          await command.RespondAsync("❌ Failed to disable birthday announcements. Please try again.");
        }
      }
      else
      {
        await command.RespondAsync("ℹ️ Birthday announcements were already disabled for this server.");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error handling disable command: {ex.Message}");
      await command.RespondAsync("❌ An error occurred while disabling birthday announcements.");
    }
  }
}
