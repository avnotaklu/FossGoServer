[Immutable, GenerateSerializer]
[Alias("GuestUser")]
public class GuestUser
{
    public string Id { get; set; }
    public GuestUser(string id)
    {
        Id = id;
    }
}

public class GuestAuthenticationModel
{
    public GuestUser User { get; set; }
    public string Token { get; set; }

    public GuestAuthenticationModel(GuestUser user, string token)
    {
        User = user;
        Token = token;
    }
}