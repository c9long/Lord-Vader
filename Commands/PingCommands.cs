// Copyright (c) MyDiscordBot. All rights reserved.

using Discord;
using Discord.WebSocket;
using MyDiscordBot.Services;

namespace MyDiscordBot.Commands;

public class PingCommands
{
  private readonly PingService pingService;

  public PingCommands(PingService pingService)
  {
    this.pingService = pingService;
  }

  /// <summary>
  /// Registers the birthday slash command.
  /// </summary>
  /// <returns></returns>
  public SlashCommandProperties GetPingCommand()
  {
    return new SlashCommandBuilder()
      .WithName("ping")
      .WithDescription("Ping the bot to receive a pong response")
      .Build();
  }

  /// <summary>
  /// Handles ping slash command execution.
  /// </summary>
  public async Task HandlePingCommandAsync(SocketSlashCommand command)
  {
    if (command.Data.Name != "ping")
    {
      return;
    }

    await this.pingService.SendPingResponse(command);
  }
}
