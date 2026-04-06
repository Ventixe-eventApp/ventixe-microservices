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
// Try to use Ollama if configured, otherwise use fallback
var ollamaEndpoint = builder.Configuration["Ollama:Endpoint"];
var ollamaModel = builder.Configuration["Ollama:Model"];

if (!string.IsNullOrEmpty(ollamaEndpoint) && !string.IsNullOrEmpty(ollamaModel))
{
    try
    {
        var ollamaChatClient = new OllamaChatClient(new Uri(ollamaEndpoint), ollamaModel);
        builder.Services.AddSingleton<IChatClient>(ollamaChatClient);
    }
    catch
    {
        // Fallback to simple client if Ollama not available
        var simpleChatClient = new SimpleChatClient();
        builder.Services.AddSingleton<IChatClient>(simpleChatClient);
    }
}
else
{
    // Using a simple in-memory chat client for development
    var simpleChatClient = new SimpleChatClient();
    builder.Services.AddSingleton<IChatClient>(simpleChatClient);
}

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
/// Ollama-based chat client for local LLM inference
/// </summary>
public class OllamaChatClient : IChatClient
{
    private readonly Uri _endpoint;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public OllamaChatClient(Uri endpoint, string model)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _httpClient = new HttpClient();
    }

    public ChatClientMetadata Metadata => new(_model);

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = chatMessages.Select(m => new
            {
                role = m.Role.ToString().ToLower(),
                content = m.Text
            }).ToList();

            var requestBody = new
            {
                model = _model,
                messages,
                stream = false
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                new Uri(_endpoint, "/api/chat"),
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonResponse = System.Text.Json.JsonDocument.Parse(responseContent);

            if (jsonResponse.RootElement.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                var assistantMessage = contentElement.GetString() ?? "No response from Ollama";
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, assistantMessage));
            }

            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "No valid response from Ollama"));
        }
        catch (Exception ex)
        {
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, $"Error communicating with Ollama: {ex.Message}"));
        }
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Streaming not implemented yet");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() => _httpClient?.Dispose();
}

/// <summary>
/// Simple in-memory chat client for development/testing (fallback)
/// </summary>
public class SimpleChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("simple-dev-model");

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = "I'm an AI assistant. Ollama is not configured or not responding.";
        await Task.Delay(50, cancellationToken);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, response));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Streaming not implemented in dev mode");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
