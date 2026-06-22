using System;
using System.Text.Json;

namespace MediBook.Models;

public class DaySchedule
{
    public bool IsOpen { get; set; } = true;
    public string OpenTime { get; set; } = "09:00 AM";
    public string CloseTime { get; set; } = "05:00 PM";
}

public class WeeklySchedule
{
    public DaySchedule Monday { get; set; } = new();
    public DaySchedule Tuesday { get; set; } = new();
    public DaySchedule Wednesday { get; set; } = new();
    public DaySchedule Thursday { get; set; } = new();
    public DaySchedule Friday { get; set; } = new();
    public DaySchedule Saturday { get; set; } = new() { IsOpen = false };
    public DaySchedule Sunday { get; set; } = new() { IsOpen = false };

    public DaySchedule GetDaySchedule(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => Monday,
            DayOfWeek.Tuesday => Tuesday,
            DayOfWeek.Wednesday => Wednesday,
            DayOfWeek.Thursday => Thursday,
            DayOfWeek.Friday => Friday,
            DayOfWeek.Saturday => Saturday,
            DayOfWeek.Sunday => Sunday,
            _ => new DaySchedule { IsOpen = false }
        };
    }

    public static WeeklySchedule FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new WeeklySchedule();
        try
        {
            return JsonSerializer.Deserialize<WeeklySchedule>(json) ?? new WeeklySchedule();
        }
        catch
        {
            return new WeeklySchedule();
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}
