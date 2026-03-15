using Microsoft.Extensions.AI;
using Dapper;
using Microsoft.Data.SqlClient;
using Ventixe.AI.Service.Models;

namespace Ventixe.AI.Service.Services;

public class EventIndexer
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly string _connectionString;
    private readonly ILogger<EventIndexer> _logger;
    private readonly QdrantService _qdrantService;

    public EventIndexer(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IConfiguration config,
        QdrantService qdrantService,
        ILogger<EventIndexer> logger)
    {
        _embeddingGenerator = embeddingGenerator;
        _connectionString = config.GetConnectionString("SqlConnection")
            ?? throw new ArgumentNullException("SqlConnection missing");
        _logger = logger;
        _qdrantService = qdrantService;

    }

    public async Task ProcessAndIndexEventsAsync()
    {
   
        _logger.LogInformation("Step 1: Fetching events from SQL Server...");
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT Id, EventName, ArtistName, Description, Location, StartDate FROM Events";
        var events = (await connection.QueryAsync<EventModel>(sql)).ToList();

        if (!events.Any()) return;

        _logger.LogInformation("Step 2: Ensuring Qdrant collection exists...");
        await _qdrantService.EnsureCollectionExistsAsync();

        _logger.LogInformation("Step 3: Creating embeddings for all {Count} events in one batch...", events.Count);

        try
        {
            // Skapa en lista med all text som ska omvandlas
            var texts = events.Select(e => e.ToEmbeddingText()).ToList();

            // SKICKA ALLT SAMTIDIGT - Detta är bara 1 anrop mot Google!
            var embeddings = await _embeddingGenerator.GenerateAsync(texts);

            for (int i = 0; i < events.Count; i++)
            {
                var vector = embeddings[i].Vector.ToArray();
                var ev = events[i];

                if (Guid.TryParse(ev.Id, out var guid))
                {
                    var metadata = new Dictionary<string, string> { { "EventName", ev.EventName } /* ... rest of metadata */ };
                    await _qdrantService.UpsertEventAsync(guid, vector, metadata);
                    _logger.LogInformation("Successfully indexed: {EventName}", ev.EventName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch indexing failed.");
        }
    
        _logger.LogInformation("Indexing process finished.");
    }
}