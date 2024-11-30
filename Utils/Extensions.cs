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

public static class DictionaryExtensions
{
    public static TValue? GetOrNull<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull where TValue : class
    {
        return dictionary.TryGetValue(key, out var value) ? value : null;
    }
}