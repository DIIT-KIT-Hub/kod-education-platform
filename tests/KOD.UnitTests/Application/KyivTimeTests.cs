using KOD.Application.Common.Text;

namespace KOD.UnitTests.Application;

public sealed class KyivTimeTests
{
    [Theory]
    [InlineData(2026, 1, 15, 2026, 1, 14, 22)] // winter: UTC+2
    [InlineData(2026, 7, 15, 2026, 7, 14, 21)] // summer: UTC+3
    [InlineData(2026, 10, 25, 2026, 10, 24, 21)] // the day daylight saving time ends starts in summer time
    [InlineData(2026, 10, 26, 2026, 10, 25, 22)]
    public void Kyiv_days_start_at_local_midnight(int year, int month, int day, int utcYear, int utcMonth, int utcDay, int utcHour)
    {
        var start = KyivTime.StartOfDay(new DateOnly(year, month, day));

        Assert.Equal(new DateTimeOffset(utcYear, utcMonth, utcDay, utcHour, 0, 0, TimeSpan.Zero), start);
    }
}
