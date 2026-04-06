using Microsoft.Extensions.AI;
using System.Text.Json;
using Ventixe.AI.Service.Models;

namespace Ventixe.AI.Service.Services;

/// <summary>
/// AI Agent for helping customers discover events through conversation
/// Uses Microsoft.Extensions.AI for function calling
/// </summary>
public interface IEventDiscoveryAgent
{
    Task<ChatResponse> ProcessUserMessageAsync(string userMessage, string conversationId, string? userId = null);
}

public class EventDiscoveryAgent : IEventDiscoveryAgent
{
    private readonly IChatClient _chatClient;
    private readonly IEventSearchService _eventSearchService;
    private readonly IConversationService _conversationService;
    private readonly ILogger<EventDiscoveryAgent> _logger;

    private static readonly AIFunction[] AvailableTools =
    {
        AIFunction.Create(
            "SearchEventsByMusic",
            "Search for events by music genre or artist name",
            SearchEventsByMusicSchema,
            "json"),
        AIFunction.Create(
            "SearchEventsByCity",
            "Search for events in a specific city or location",
            SearchEventsByCitySchema,
            "json"),
        AIFunction.Create(
            "SearchEventsByDate",
            "Search for events within a specific date range",
            SearchEventsByDateSchema,
            "json"),
        AIFunction.Create(
            "SearchEventsCombined",
            "Search for events with multiple filters (music, city, dates)",
            SearchEventsCombinedSchema,
            "json"),
        AIFunction.Create(
            "GetEventDetails",
            "Get detailed information about a specific event",
            GetEventDetailsSchema,
            "json")
    };

    public EventDiscoveryAgent(
        IChatClient chatClient,
        IEventSearchService eventSearchService,
        IConversationService conversationService,
        ILogger<EventDiscoveryAgent> logger)
    {
        _chatClient = chatClient;
        _eventSearchService = eventSearchService;
        _conversationService = conversationService;
        _logger = logger;
    }

    public async Task<ChatResponse> ProcessUserMessageAsync(string userMessage, string conversationId, string? userId = null)
    {
        try
        {
            _logger.LogInformation("Processing user message for conversation: {ConversationId}", conversationId);

            // Save user message
            await _conversationService.SaveMessageAsync(conversationId, "user", userMessage);

            // Build conversation history
            var history = await _conversationService.GetConversationHistoryAsync(conversationId);
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, GetSystemPrompt())
            };

            // Add previous messages
            foreach (var (role, content, _) in history)
            {
                messages.Add(new ChatMessage(
                    role == "user" ? ChatRole.User : ChatRole.Assistant,
                    content));
            }

            // Get agent response with function calling
            var options = new ChatOptions
            {
                Tools = AvailableTools
            };

            var response = await _chatClient.CompleteAsync(messages, options);
            var assistantMessage = response.Message.Text ?? "I couldn't find a response.";
            var suggestedEvents = new List<EventDto>();
            var searchFilters = new EventSearchFilters();

            // Process function calls if any
            if (response.Message.ToolCalls?.Count > 0)
            {
                foreach (var toolCall in response.Message.ToolCalls)
                {
                    _logger.LogInformation("Agent called tool: {ToolName}", toolCall.ToolName);
                    var toolResult = await ExecuteToolAsync(toolCall.ToolName, toolCall.Arguments);
                    suggestedEvents.AddRange(toolResult.Events);
                    
                    if (toolResult.Filters != null)
                        searchFilters = toolResult.Filters;
                }
            }

            // Save assistant response
            await _conversationService.SaveMessageAsync(
                conversationId,
                "assistant",
                assistantMessage,
                suggestedEvents.Count > 0 ? searchFilters : null,
                suggestedEvents);

            return new ChatResponse
            {
                ConversationId = conversationId,
                Message = assistantMessage,
                SuggestedEvents = suggestedEvents,
                SearchFilters = searchFilters.MusicGenre != null || searchFilters.City != null
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                        JsonSerializer.Serialize(searchFilters))
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user message");
            throw;
        }
    }

    private async Task<(List<EventDto> Events, EventSearchFilters? Filters)> ExecuteToolAsync(string toolName, object? arguments)
    {
        var events = new List<EventDto>();
        EventSearchFilters? filters = null;

        try
        {
            var args = arguments as JsonElement? ?? default;

            switch (toolName)
            {
                case "SearchEventsByMusic":
                    var genre = args.GetProperty("genre").GetString();
                    if (!string.IsNullOrEmpty(genre))
                    {
                        events = await _eventSearchService.SearchByMusicGenreAsync(genre);
                        filters = new EventSearchFilters { MusicGenre = genre };
                    }
                    break;

                case "SearchEventsByCity":
                    var city = args.GetProperty("city").GetString();
                    if (!string.IsNullOrEmpty(city))
                    {
                        events = await _eventSearchService.SearchByCityAsync(city);
                        filters = new EventSearchFilters { City = city };
                    }
                    break;

                case "SearchEventsByDate":
                    var fromDateStr = args.GetProperty("fromDate").GetString();
                    var toDateStr = args.GetProperty("toDate").GetString();
                    
                    if (DateTime.TryParse(fromDateStr, out var fromDate) && 
                        DateTime.TryParse(toDateStr, out var toDate))
                    {
                        events = await _eventSearchService.SearchByDateRangeAsync(fromDate, toDate);
                        filters = new EventSearchFilters { FromDate = fromDate, ToDate = toDate };
                    }
                    break;

                case "SearchEventsCombined":
                    var combinedCity = args.GetProperty("city").GetString();
                    var combinedGenre = args.GetProperty("musicGenre").GetString();
                    var combinedFromStr = args.GetProperty("fromDate").GetString();
                    var combinedToStr = args.GetProperty("toDate").GetString();

                    DateTime? combinedFromDate = null;
                    DateTime? combinedToDate = null;

                    if (DateTime.TryParse(combinedFromStr, out var cFromDate))
                        combinedFromDate = cFromDate;
                    if (DateTime.TryParse(combinedToStr, out var cToDate))
                        combinedToDate = cToDate;

                    events = await _eventSearchService.SearchByCombinedAsync(
                        combinedCity, combinedGenre, combinedFromDate, combinedToDate);
                    
                    filters = new EventSearchFilters
                    {
                        City = combinedCity,
                        MusicGenre = combinedGenre,
                        FromDate = combinedFromDate,
                        ToDate = combinedToDate
                    };
                    break;

                case "GetEventDetails":
                    var eventId = args.GetProperty("eventId").GetString();
                    if (!string.IsNullOrEmpty(eventId))
                    {
                        var eventDetail = await _eventSearchService.GetEventDetailsAsync(eventId);
                        if (eventDetail != null)
                            events.Add(eventDetail);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
        }

        return (events, filters);
    }

    private string GetSystemPrompt()
    {
        return @"You are an AI assistant for Ventixe, a modern event ticketing platform.
Your role is to help customers discover and book events they're interested in.

You have access to tools to search for events by:
- Music genre or artist name
- City or location
- Date range
- Multiple criteria combined

When a customer asks about events:
1. Understand their preferences (music genre, location, dates)
2. Use the appropriate search tools to find matching events
3. Present results in a friendly, conversational way
4. Provide event details (name, artist, date, location, price)
5. Ask follow-up questions if needed to refine the search

Be helpful, friendly, and concise. Always offer to help with more specific searches if results aren't what they want.";
    }

    private static string SearchEventsByMusicSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""genre"": {
      ""type"": ""string"",
      ""description"": ""The music genre or artist name to search for (e.g., 'jazz', 'Miles Davis', 'rock')""
    }
  },
  ""required"": [""genre""]
}";

    private static string SearchEventsByCitySchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""city"": {
      ""type"": ""string"",
      ""description"": ""The city or location to search for events (e.g., 'New York', 'NYC', 'Los Angeles')""
    }
  },
  ""required"": [""city""]
}";

    private static string SearchEventsByDateSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""fromDate"": {
      ""type"": ""string"",
      ""description"": ""Start date in ISO format (YYYY-MM-DD)""
    },
    ""toDate"": {
      ""type"": ""string"",
      ""description"": ""End date in ISO format (YYYY-MM-DD)""
    }
  },
  ""required"": [""fromDate"", ""toDate""]
}";

    private static string SearchEventsCombinedSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""city"": {
      ""type"": ""string"",
      ""description"": ""The city or location (optional)""
    },
    ""musicGenre"": {
      ""type"": ""string"",
      ""description"": ""The music genre or artist (optional)""
    },
    ""fromDate"": {
      ""type"": ""string"",
      ""description"": ""Start date in ISO format (optional)""
    },
    ""toDate"": {
      ""type"": ""string"",
      ""description"": ""End date in ISO format (optional)""
    }
  }
}";

    private static string GetEventDetailsSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""eventId"": {
      ""type"": ""string"",
      ""description"": ""The UUID of the event to get details for""
    }
  },
  ""required"": [""eventId""]
}";
}
