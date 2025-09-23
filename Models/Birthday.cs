// Copyright (c) MyDiscordBot. All rights reserved.

namespace MyDiscordBot.Models;

public class Birthday
{
  public ulong UserId { get; set; }

  public DateTime Date { get; set; }

  public Birthday(ulong userId, DateTime date)
  {
    this.UserId = userId;
    this.Date = date;
  }

  /// <summary>
  /// Checks if today is this person's birthday (ignoring year).
  /// </summary>
  /// <returns></returns>
  public bool IsTodayTheirBirthday()
  {
    var today = DateTime.Now.Date;
    return this.Date.Month == today.Month && this.Date.Day == today.Day;
  }

  /// <summary>
  /// Gets the formatted birthday string for display.
  /// </summary>
  /// <returns></returns>
  public string GetFormattedDate()
  {
    return this.Date.ToString("MMMM dd, yyyy");
  }
}
