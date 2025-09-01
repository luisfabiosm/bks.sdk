using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Extensions;



public static class DateTimeExtensions
{
    public static bool IsWeekend(this DateTime dateTime)
    {
        return dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday;
    }

    public static bool IsWeekday(this DateTime dateTime)
    {
        return !dateTime.IsWeekend();
    }

    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Kind);
    }

    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 23, 59, 59, 999, dateTime.Kind);
    }

    public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        var diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
        return dateTime.AddDays(-1 * diff).StartOfDay();
    }

    public static DateTime EndOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        return dateTime.StartOfWeek(startOfWeek).AddDays(6).EndOfDay();
    }

    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind);
    }

    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return dateTime.StartOfMonth().AddMonths(1).AddDays(-1).EndOfDay();
    }

    public static DateTime StartOfYear(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);
    }

    public static DateTime EndOfYear(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 12, 31, 23, 59, 59, 999, dateTime.Kind);
    }

    public static int Age(this DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;

        if (birthDate.Date > today.AddYears(-age))
            age--;

        return age;
    }

    public static string ToRelativeTime(this DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();

        return timeSpan switch
        {
            _ when timeSpan.TotalSeconds < 60 => "agora há pouco",
            _ when timeSpan.TotalMinutes < 2 => "há 1 minuto",
            _ when timeSpan.TotalMinutes < 60 => $"há {(int)timeSpan.TotalMinutes} minutos",
            _ when timeSpan.TotalHours < 2 => "há 1 hora",
            _ when timeSpan.TotalHours < 24 => $"há {(int)timeSpan.TotalHours} horas",
            _ when timeSpan.TotalDays < 2 => "ontem",
            _ when timeSpan.TotalDays < 30 => $"há {(int)timeSpan.TotalDays} dias",
            _ when timeSpan.TotalDays < 60 => "há 1 mês",
            _ when timeSpan.TotalDays < 365 => $"há {(int)(timeSpan.TotalDays / 30)} meses",
            _ when timeSpan.TotalDays < 730 => "há 1 ano",
            _ => $"há {(int)(timeSpan.TotalDays / 365)} anos"
        };
    }

    public static bool IsBetween(this DateTime dateTime, DateTime start, DateTime end)
    {
        return dateTime >= start && dateTime <= end;
    }

    public static DateTime NextWeekday(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        var daysUntil = ((int)dayOfWeek - (int)dateTime.DayOfWeek + 7) % 7;
        if (daysUntil == 0)
            daysUntil = 7;

        return dateTime.AddDays(daysUntil);
    }

    public static DateTime PreviousWeekday(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        var daysSince = ((int)dateTime.DayOfWeek - (int)dayOfWeek + 7) % 7;
        if (daysSince == 0)
            daysSince = 7;

        return dateTime.AddDays(-daysSince);
    }
}
