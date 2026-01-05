using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Ventixe.AI.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Core API Services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 2. AI Configuration & Embedding Generator
var googleApiKey = builder.Configuration["Google:ApiKey"]
    ?? throw new Exception("Google:ApiKey is missing in configuration!");


builder.Services.AddGoogleAIEmbeddingGenerator(
    modelId: "embedding-001",
    apiKey: googleApiKey);



// 3. Register our Custom Services
builder.Services.AddScoped<QdrantService>(); 
builder.Services.AddScoped<EventIndexer>();

var app = builder.Build();

// 4. Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 5. Trigger Endpoint
app.MapPost("/api/ai/index", async (EventIndexer indexer) =>
{
    await indexer.ProcessAndIndexEventsAsync();
    return Results.Ok("Indexing process started.");
});

app.Run();