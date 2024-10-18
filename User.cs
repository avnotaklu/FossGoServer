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
    public User(string email)
    {
        Email = email;
        // Id = id;
    }
}
