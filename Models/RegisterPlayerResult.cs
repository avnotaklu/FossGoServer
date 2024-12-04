using BadukServer;
using Google.Apis.Http;

public class RegisterPlayerResult
{
    public PlayerInfo CurrentUser { get; set; }

    public RegisterPlayerResult(PlayerInfo currentUser)
    {
        CurrentUser = currentUser;
    }
}