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
        
    }

    public async Task ProcessAndIndexEventsAsync()
    {
        _logger.LogInformation("Fetching events from SQL Server...");

        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT Id, EventName, ArtistName, Description, Location, StartDate FROM Events";
        var events = await connection.QueryAsync<EventModel>(sql);

        foreach (var ev in events)
        {
            try
            {
                string textToEmbed = ev.ToEmbeddingText();

                // Updated method call
                var embeddings = await _embeddingGenerator.GenerateAsync(new[] { textToEmbed });
                var vector = embeddings[0].Vector.ToArray();

                var metadata = new Dictionary<string, string>
                {
                    { "EventName", ev.EventName },
                    { "ArtistName", ev.ArtistName },
                    { "Location", ev.Location },
                    { "StartDate", ev.StartDate.ToString("yyyy-MM-dd") }
                };

                if (Guid.TryParse(ev.Id, out var guid))
                {
                    await _qdrantService.UpsertEventAsync(guid, vector, metadata);
                    _logger.LogInformation("Indexed event: {EventName}", ev.EventName);
                }
                else
                {
                    _logger.LogWarning("Invalid GUID for event ID: {EventId}", ev.Id);
                }   



            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index {EventName}", ev.EventName);
            }
        }
    }
}