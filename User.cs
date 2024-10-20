using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BadukServer;

[BsonIgnoreExtraElements]
public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("email")]
    public string Email { get; set; }
    public string? PasswordHash { get; set; }
    public bool GoogleSignIn { get; set; }
    public User(string email,  bool googleSignIn, string? passwordHash = null)
    {
        Email = email;
        PasswordHash = passwordHash;
        GoogleSignIn = googleSignIn;
    }
}
