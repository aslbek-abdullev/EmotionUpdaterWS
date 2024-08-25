using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EmotionUpdaterWS.src.Models;

public class PersonEmotion
{
    [BsonId]
    public ObjectId Id { get; set; }
    public List<Person>? People { get; set; }
}

