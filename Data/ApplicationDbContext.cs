// Copyright (c) MyDiscordBot. All rights reserved.

using Microsoft.EntityFrameworkCore;
using MyDiscordBot.Models;

namespace MyDiscordBot.Data;

public class ApplicationDbContext : DbContext
{
  public DbSet<Birthday> Birthdays { get; set; } = null!;
  public DbSet<GuildConfig> GuildConfigs { get; set; } = null!;

  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
  {
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Birthday>()
      .HasKey(b => b.UserId);

    modelBuilder.Entity<GuildConfig>()
      .HasKey(g => g.GuildId);
  }
}