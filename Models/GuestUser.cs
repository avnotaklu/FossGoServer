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
    public AuthCreds Creds { get; set; }

    public GuestAuthenticationModel(GuestUser user, AuthCreds creds)
    {
        User = user;
        Creds = creds;
    }
}