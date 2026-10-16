namespace TradingDataGenerator.Generation.Generators;

public sealed class TradingCalendarGenerator : ITradingCalendarGenerator
{
    public List<DateTime> GetTradingDays(DateTime startDate, DateTime endDate)
    {
        List<DateTime> tradingDaysToReturn;
        DateTime tradingDate;

        tradingDaysToReturn = new List<DateTime>();
        for (tradingDate = startDate.Date; tradingDate <= endDate.Date; tradingDate = tradingDate.AddDays(1))
        {
            if (tradingDate.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                tradingDaysToReturn.Add(tradingDate);
            }
        }
        return tradingDaysToReturn;
    }
}
