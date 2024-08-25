using EmotionUpdaterWS.src.Models;
using MongoDB.Driver;

namespace EmotionUpdaterWS.src.Data;

public class EmotionContext
{
    private readonly IMongoCollection<PersonEmotion> _personEmotions;

    public EmotionContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _personEmotions = database.GetCollection<PersonEmotion>("people");
    }

    public IMongoCollection<PersonEmotion> PersonEmotions => _personEmotions;
}
