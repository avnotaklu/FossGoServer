// using MongoDB.Bson;
// using MongoDB.Bson.Serialization;

// public class ObjectIdListSerializer : IBsonSerializer<List<string>>
// {
//     public Type ValueType => typeof(List<string>);

//     public List<string> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
//     {
//         var bsonReader = context.Reader;
//         var list = new List<string>();

//         if (bsonReader.IsArray)
//         {
//             bsonReader.ReadStartArray();
//             while (bsonReader.ReadBsonType() != BsonType.end)
//             {
//                 if (bsonReader.CurrentBsonType == BsonType.ObjectId)
//                 {
//                     list.Add(bsonReader.ReadObjectId().ToString());
//                 }
//                 else
//                 {
//                     throw new Exception("Expected ObjectId in the array.");
//                 }
//             }
//             bsonReader.ReadEndArray();
//         }

//         return list;
//     }

//     public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, List<string> value)
//     {
//         var bsonWriter = context.Writer;

//         bsonWriter.WriteStartArray();
//         foreach (var idString in value)
//         {
//             bsonWriter.WriteObjectId(new ObjectId(idString));
//         }
//         bsonWriter.WriteEndArray();
//     }
// }
