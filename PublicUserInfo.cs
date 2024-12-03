[Immutable]
[GenerateSerializer]
public class PublicUserInfo {
    public string Id {get; set;}
    public string Email {get; set;}
    public UserRating? Rating {get; set;}

    public PublicUserInfo( string id, string email, UserRating rating) {
        Email = email;
        Id = id;
        Rating = rating;
    }
}