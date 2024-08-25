using EmotionUpdaterWS.src.Data;
using EmotionUpdaterWS.src.Models;
using MongoDB.Driver;

namespace EmotionUpdaterWS.src.Services;

public class EmotionUpdateService : BackgroundService
{
    private readonly EmotionContext _context;
    private readonly ILogger<EmotionUpdateService> _logger;

    public EmotionUpdateService(EmotionContext context, ILogger<EmotionUpdateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateEmotionsAsync();

                await WebSocketHandler.NotifyClientsAsync(_context);

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating emotions.");
            }
        }
    }

    private async Task UpdateEmotionsAsync()
    {
        var peopleToUpdate = await GetPeopleToUpdateAsync();

        if (peopleToUpdate.Count == 0)
        {
            _logger.LogInformation("No people to update.");
            return;
        }

        var filter = Builders<PersonEmotion>.Filter.In("People._id", peopleToUpdate);

        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new JsonArrayFilterDefinition<Person>("{ \"elem._id\": { $in: [" + string.Join(", ", peopleToUpdate) + "] } }")
        };

        var updates = new List<UpdateDefinition<PersonEmotion>>
        {
            Builders<PersonEmotion>.Update.Inc("People.$[elem].Emotions.neutral", 1),
            Builders<PersonEmotion>.Update.Inc("People.$[elem].Emotions.happy", 1)
        };

        var options = new UpdateOptions { ArrayFilters = arrayFilters };

        foreach (var update in updates)
        {
            var result = await _context.PersonEmotions.UpdateManyAsync(filter, update, options);
            _logger.LogInformation($"Update result: MatchedCount={result.MatchedCount}, ModifiedCount={result.ModifiedCount}");
        }
    }
    
    private async Task<List<int>> GetPeopleToUpdateAsync()
    {
        var filter = Builders<PersonEmotion>.Filter.Lt("People.Id", 10);

        var peopleToUpdate = await _context.PersonEmotions
            .Find(filter)
            .Project(p => p.People.Where(person => person.Id < 10).Select(person => person.Id).ToList())
            .ToListAsync();

        return peopleToUpdate.SelectMany(p => p).ToList();
    }
}
