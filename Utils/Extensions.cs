using System.CodeDom;

public static class DateTimeExtensions
{
    public static string SerializedDate(this DateTime dateTime)
    {
        return dateTime.ToString("o");
    }

    public static DateTime DeserializedDate(this string dateTime)
    {
        return DateTime.Parse(dateTime);
    }
}