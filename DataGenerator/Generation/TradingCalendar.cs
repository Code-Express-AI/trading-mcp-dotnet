namespace TradingDataGenerator.Generation;

internal static class TradingCalendar
{
    // Weekdays only. Public holidays are ignored to keep things simple.
    public static List<DateTime> GetTradingDays(DateTime startDate, DateTime endDate)
    {
        var tradingDays = new List<DateTime>();
        for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                tradingDays.Add(day);
            }
        }

        return tradingDays;
    }
}
