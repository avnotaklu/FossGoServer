public interface IDateTimeService
{
    public DateTime Now();
    public string NowFormatted();
}

public class DateTimeService : IDateTimeService
{
    public DateTime Now()
    {
        return DateTime.Now;
    }

    public string NowFormatted() {
        return DateTime.Now.ToString("o");
    }
}