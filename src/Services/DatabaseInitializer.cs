using EmotionUpdaterWS.src.Data;
using EmotionUpdaterWS.src.Models;
using MongoDB.Driver;
namespace EmotionUpdaterWS.src.Services;
public class DatabaseInitializer
{
    private readonly EmotionContext _context;

    public DatabaseInitializer(EmotionContext context)
    {
        _context = context;
    }

    public async Task InitializeDatabaseAsync()
    {
        var collectionExists = (await _context.PersonEmotions.Database.ListCollectionNamesAsync())
            .ToList()
            .Contains("people");

        if (!collectionExists)
        {
            var initialData = new PersonEmotion
            {
                People = new List<Person>
                {
                    new Person
                    {
                        Id = 1,
                        Emotions = new Dictionary<string, int>
                        {
                            { "neutral", 10 },
                            { "happy", 5 },
                            { "sad", 3 },
                            { "angry", 2 },
                            { "surprise", 1 },
                            { "fear", 0 },
                            { "disgust", 0 }
                        }
                    },
                    new Person
                    {
                        Id = 2,
                        Emotions = new Dictionary<string, int>
                        {
                            { "neutral", 4 },
                            { "happy", 5 },
                            { "sad", 3 },
                            { "angry", 2 },
                            { "surprise", 1 },
                            { "fear", 1 },
                            { "disgust", 1 }
                        }
                    }

                }
            };

            await _context.PersonEmotions.InsertOneAsync(initialData);
        }
    }
}
