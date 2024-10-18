using BadukServer;

public class UserAuthenticationModel
{
    public User User {get;set;}
    public string Token {get;set;}

    public UserAuthenticationModel(User user, string token)
    {
        User = user;
        Token = token;
    }
}