using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BadukServer;

public class UserFieldNames
{
    public const string Email = "em";
    public const string PasswordHash = "ph";
    public const string GoogleSignIn = "gs";
    public const string UserName = "un";
    public const string FullName = "fn";
    public const string Bio = "bio";
    public const string Avatar = "av";
    public const string CreationDate = "cd";
    public const string LastSeen = "ls";
    public const string Nationality = "nat";
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
    public string? Email { get; set; }

    [BsonElement(UserFieldNames.GoogleSignIn)]
    public bool GoogleSignIn { get; set; }

    [BsonElement(UserFieldNames.UserName)]
    public string UserName { get; set; }

    [BsonElement(UserFieldNames.FullName)]
    public string? FullName { get; set; }

    [BsonElement(UserFieldNames.Bio)]
    public string? Bio { get; set; }

    [BsonElement(UserFieldNames.Avatar)]
    public string? Avatar { get; set; }

    [BsonElement(UserFieldNames.Nationality)]
    public string? Nationality { get; set; }

    [BsonElement(UserFieldNames.CreationDate)]
    public DateTime CreationDate { get; set; }

    [BsonElement(UserFieldNames.LastSeen)]
    public DateTime LastSeen { get; set; }

    [BsonElement(UserFieldNames.PasswordHash)]
    [BsonIgnoreIfNull]
    public string? PasswordHash { get; set; }


    public User(string? email, bool googleSignIn, string userName, string? fullName, string? bio, string? avatar, string? nationality, DateTime creation, DateTime lastSeen, string? passwordHash = null)
    {
        Email = email;
        PasswordHash = passwordHash;
        GoogleSignIn = googleSignIn;

        UserName = userName;
        FullName = fullName;
        Bio = bio;
        Avatar = avatar;
        Nationality = nationality;
        CreationDate = creation;
        LastSeen = lastSeen;
    }
}
