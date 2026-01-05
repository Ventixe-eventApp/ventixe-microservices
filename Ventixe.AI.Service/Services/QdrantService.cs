using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Ventixe.AI.Service.Services;

public class QdrantService
{
    private readonly QdrantClient _client;
    private const string CollectionName = "events";
    private readonly ILogger<QdrantService> _logger;

    public QdrantService(IConfiguration config, ILogger<QdrantService> logger)
    { 
        _logger = logger;
        // These keys match your Docker environment variables: Qdrant__Host and Qdrant__Port
        var host = config["Qdrant:Host"] ?? config["Qdrant__Host"] ?? "qdrant";
        var portStr = config["Qdrant:Port"] ?? config["Qdrant__Port"] ?? "6334";

        if (!int.TryParse(portStr, out var port))
        {
            port = 6333;
        }

        _logger.LogInformation("Connecting to Qdrant at {Host}:{Port}", host, port);
        _client = new QdrantClient(host, port);
      
    }

    /// <summary>
    /// Ensures that the 'events' collection exists in Qdrant.
    /// If not, it creates it with the correct dimensions for Google Gemini (768).
    /// </summary>
    public async Task EnsureCollectionExistsAsync()
    {
        var collections = await _client.ListCollectionsAsync();

        if (!collections.Contains(CollectionName))
        {
            _logger.LogInformation("Collection '{Collection}' not found. Creating it now...", CollectionName);

            // Google's 'embedding-001' model produces vectors with 768 dimensions.
            // Cosine distance is the standard for text similarity.
            await _client.CreateCollectionAsync(CollectionName,
                new VectorParams { Size = 768, Distance = Distance.Cosine });

            _logger.LogInformation("Collection '{Collection}' created successfully.", CollectionName);
        }
    }

    /// <summary>
    /// Saves an event and its vector to Qdrant.
    /// </summary>
    public async Task UpsertEventAsync(Guid id, float[] vector, Dictionary<string, string> metadata)
    {
        var point = new PointStruct
        {
            Id = id,
            Vectors = vector
        };

        // Add metadata so we can display results without hitting SQL every time
        foreach (var entry in metadata)
        {
            point.Payload.Add(entry.Key, entry.Value);
        }

        await _client.UpsertAsync(CollectionName, new[] { point });
    }
}