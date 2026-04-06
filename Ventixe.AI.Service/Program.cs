using Microsoft.Extensions.AI;
using Npgsql;
using Ventixe.AI.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Core API Services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 2. Database Configuration with Connection Pooling
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured!");

// Create NpgsqlDataSource for automatic connection pooling
var dataSource = new NpgsqlDataSourceBuilder(connectionString)
    .EnableParameterLogging()
    .Build();

builder.Services.AddSingleton(dataSource);
builder.Services.AddScoped<IEventSearchService, EventSearchService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IEventDiscoveryAgent, EventDiscoveryAgent>();

// 3. AI/LLM Configuration
// Using a simple in-memory chat client for development
// In production, add: .AddOpenAIChatClient(o => o.ApiKey = "your-api-key")
var simpleChatClient = new SimpleChatClient();
builder.Services.AddSingleton<IChatClient>(simpleChatClient);

// 4. Logging
builder.Services.AddLogging();

var app = builder.Build();

// 5. Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Simple in-memory chat client for development/testing
/// </summary>
public class SimpleChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("simple-dev-model");

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Return a simple response
        var response = "I'm an AI assistant. Please configure a real LLM provider for production use.";
        await Task.Delay(50, cancellationToken); // Simulate processing
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, response));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Streaming not implemented in dev mode");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
