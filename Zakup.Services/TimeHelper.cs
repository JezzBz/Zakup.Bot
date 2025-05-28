using Zakup.Common.DTO;

namespace Zakup.WebHost.Helpers;

public static class TimeHelper
{
    public static TimePeriod GetToday()
    {
        var period = new TimePeriod();
        var nowMoscow = DateTime.UtcNow.AddHours(3);
        var startOfDayMoscow = nowMoscow.Date;          // Это "полночь" по МСК в локальном представлении
        period.StartTime = startOfDayMoscow.AddHours(-3); // это и есть та самая "полночь МСК", но в UTC

        period.EndTime = period.StartTime.AddDays(1); // конец сегодняшних суток (по МСК), тоже в UTC
        return period;
    }
}