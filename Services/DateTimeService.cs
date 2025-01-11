public interface IDateTimeService
{
    public DateTime Now();
    public string NowFormatted();
}

public class DateTimeService : IDateTimeService
{
    public DateTime Now()
    {
        return DateTime.UtcNow;
    }

    public string NowFormatted() {
        return DateTime.UtcNow.ToString("o");
    }
}