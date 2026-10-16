namespace TradingDataGenerator.Generation.Generators;

public interface ITradingCalendarGenerator
{
    List<DateTime> GetTradingDays(DateTime startDate, DateTime endDate);
}
