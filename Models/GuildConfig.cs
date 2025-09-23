// Copyright (c) MyDiscordBot. All rights reserved.

namespace MyDiscordBot.Models;

public class GuildConfig
{
  public ulong GuildId { get; set; }

  public ulong? AnnouncementChannelId { get; set; }

  public GuildConfig(ulong guildId, ulong? announcementChannelId = null)
  {
    this.GuildId = guildId;
    this.AnnouncementChannelId = announcementChannelId;
  }

  /// <summary>
  /// Gets a value indicating whether whether birthday announcements are enabled for this guild.
  /// </summary>
  public bool IsAnnouncementEnabled => this.AnnouncementChannelId.HasValue;
}
