using BadukServer;
using Google.Apis.Http;

public class RegisterPlayerResult
{
    public List<User> OtherActivePlayers { get; set; }

    public RegisterPlayerResult(List<User> otherActivePlayers)
    {
        OtherActivePlayers = otherActivePlayers;
    }
}