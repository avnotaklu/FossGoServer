public class StatUpdateMessage
{
    public UserStatForVariant Stat { get; set; }
    public PlayerRatingsData Rating { get; set; }
    public string Variant { get; set; }

    public StatUpdateMessage(UserStatForVariant stat, PlayerRatingsData rating, string variant)
    {
        Stat = stat;
        Rating = rating;
        Variant = variant;
    }
}