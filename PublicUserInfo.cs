[Immutable]
[GenerateSerializer]
public class PublicUserInfo {
    public string Id {get; set;}
    public string Email {get; set;}

    public PublicUserInfo( string id, string email)
    {
        Email = email;
        Id = id;
    }
}