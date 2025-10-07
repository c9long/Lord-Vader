// Copyright (c) MyDiscordBot. All rights reserved.

using Discord.WebSocket;

namespace MyDiscordBot.Services;

public class PingService
{
  private readonly DiscordSocketClient client;

  public PingService(DiscordSocketClient client)
  {
    this.client = client;
  }

  public async Task SendPingResponse(SocketSlashCommand command)
  {
    await command.RespondAsync($"*pong* Latency: {this.client.Latency}ms");
  }
}