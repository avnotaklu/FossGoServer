using Google.Apis.Http;

public class RegisterPlayerResult
{
    public HashSet<string> OtherActivePlayers { get; set; }

    public RegisterPlayerResult(HashSet<string> otherActivePlayers)
    {
        OtherActivePlayers = otherActivePlayers;
    }
}