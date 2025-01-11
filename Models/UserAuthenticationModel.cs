using BadukServer;



public class UserAuthenticationModel
{
    public User User {get;set;}
    public AuthCreds Creds {get;set;}

    public UserAuthenticationModel(User user, AuthCreds creds)
    {
        User = user;
        Creds = creds;
    }
}