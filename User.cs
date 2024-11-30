using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BadukServer;

public class UserFieldNames
{
    public const string Email = "em";
    public const string PasswordHash = "ph";
    public const string GoogleSignIn = "gs";
}

public static class UserExtensions
{
    public static string GetUserId(this User user)
    {
        return user.Id ?? throw new ArgumentNullException(nameof(user.Id));
    }
}

[BsonIgnoreExtraElements]
public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement(UserFieldNames.Email)]
    public string Email { get; set; }
    [BsonElement(UserFieldNames.PasswordHash)]
    [BsonIgnoreIfNull]
    public string? PasswordHash { get; set; }
    [BsonElement(UserFieldNames.GoogleSignIn)]
    public bool GoogleSignIn { get; set; }
    public User(string email, bool googleSignIn, string? passwordHash = null)
    {
        Email = email;
        PasswordHash = passwordHash;
        GoogleSignIn = googleSignIn;
    }
}
