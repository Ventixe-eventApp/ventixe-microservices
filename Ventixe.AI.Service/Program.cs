using Microsoft.Extensions.AI;
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

// 2. Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured!");
builder.Services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(builder.Configuration));

// 3. AI/LLM Configuration - Using OpenAI or Local LLM
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"]
    ?? builder.Configuration["OpenAI__ApiKey"];

if (!string.IsNullOrEmpty(openAiApiKey))
{
    // Using OpenAI GPT-4 (recommended for production)
    builder.Services.AddChatClient(
        new OpenAIChatClient("gpt-4-turbo", openAiApiKey));
}
else
{
    // Fallback: Using ollama or local LLM (for development)
    var ollamaEndpoint = builder.Configuration["Ollama:Endpoint"] 
        ?? builder.Configuration["Ollama__Endpoint"]
        ?? "http://localhost:11434";
    
    builder.Services.AddChatClient(
        new OllamaChatClient(new Uri(ollamaEndpoint), "neural-chat"));
}

// 4. Register Application Services
builder.Services.AddScoped<IEventSearchService, EventSearchService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IEventDiscoveryAgent, EventDiscoveryAgent>();

// 5. Logging
builder.Services.AddLogging();

var app = builder.Build();

// 6. Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();