using Microsoft.Extensions.AI;
using System.Text.Json;
using Ventixe.AI.Service.Models;
using ChatResponse = Ventixe.AI.Service.Models.ChatResponse;

namespace Ventixe.AI.Service.Services;

/// <summary>
/// AI Agent for helping customers discover events through conversation
/// Provides intelligent search intent extraction and event discovery
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

    public EventDiscoveryAgent(
        IChatClient chatClient,
        IEventSearchService eventSearchService,
        IConversationService conversationService,
        ILogger<EventDiscoveryAgent> logger)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _eventSearchService = eventSearchService ?? throw new ArgumentNullException(nameof(eventSearchService));
        _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChatResponse> ProcessUserMessageAsync(string userMessage, string conversationId, string? userId = null)
    {
        try
        {
            _logger.LogInformation("Processing user message for conversation: {ConversationId}", conversationId);

            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("User message cannot be empty", nameof(userMessage));

            // Save user message
            await _conversationService.SaveMessageAsync(conversationId, "user", userMessage);

            // Build conversation history
            var history = await _conversationService.GetConversationHistoryAsync(conversationId);
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, GetSystemPrompt())
            };

            // Add previous messages (last 20 to keep context manageable)
            foreach (var (role, content, _) in history.TakeLast(20))
            {
                messages.Add(new ChatMessage(
                    role == "user" ? ChatRole.User : ChatRole.Assistant,
                    content));
            }

            // Get assistant message from event search results
            var assistantMessage = "I'm ready to help you find events!";
            var suggestedEvents = new List<EventDto>();
            var searchFilters = new EventSearchFilters();

            // Parse response intent and perform searches
            (suggestedEvents, searchFilters) = await ExtractAndSearchEventsAsync(userMessage);

            // Enhance assistant message with event suggestions if found
            if (suggestedEvents.Count > 0)
            {
                assistantMessage += $"\n\nI found {suggestedEvents.Count} event{(suggestedEvents.Count != 1 ? "s" : "")} matching your interests!";
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
                SearchFilters = suggestedEvents.Count > 0 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                        JsonSerializer.Serialize(searchFilters))
                    : null
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in ProcessUserMessageAsync");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user message");
            throw;
        }
    }

    /// <summary>
    /// Extract search intent from user message and perform searches
    /// Uses keyword matching to determine search parameters
    /// </summary>
    private async Task<(List<EventDto> events, EventSearchFilters filters)> ExtractAndSearchEventsAsync(string userMessage)
    {
        var events = new List<EventDto>();
        var filters = new EventSearchFilters();
        var messageLower = userMessage.ToLowerInvariant();

        try
        {
            // Check for music-related keywords
            if (ShouldSearchByMusic(messageLower))
            {
                var genre = ExtractMusicGenre(messageLower, userMessage);
                if (!string.IsNullOrEmpty(genre))
                {
                    events = await _eventSearchService.SearchByMusicGenreAsync(genre);
                    filters.MusicGenre = genre;
                    _logger.LogInformation("Searched for music genre: {Genre}", genre);
                }
            }

            // Check for city/location keywords
            var city = ExtractCity(messageLower);
            if (!string.IsNullOrEmpty(city))
            {
                var cityEvents = await _eventSearchService.SearchByCityAsync(city);
                if (events.Count == 0)
                {
                    events = cityEvents;
                }
                else
                {
                    events = events.Intersect(cityEvents, new EventDtoComparer()).ToList();
                }
                filters.City = city;
                _logger.LogInformation("Searched for city: {City}", city);
            }

            // Check for date keywords
            if (ShouldSearchByDate(messageLower))
            {
                var (fromDate, toDate) = ExtractDateRange(messageLower);
                var dateEvents = await _eventSearchService.SearchByDateRangeAsync(fromDate, toDate);
                
                if (events.Count == 0)
                {
                    events = dateEvents;
                }
                else
                {
                    events = events.Intersect(dateEvents, new EventDtoComparer()).ToList();
                }

                filters.FromDate = fromDate;
                filters.ToDate = toDate;
                _logger.LogInformation("Searched for date range: {FromDate} to {ToDate}", fromDate, toDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting and searching events from message");
        }

        return (events, filters);
    }

    private bool ShouldSearchByMusic(string messageLower)
    {
        var musicKeywords = new[] 
        { 
            "music", "artist", "band", "concert", "jazz", "rock", "pop", "hip-hop", 
            "classical", "electronic", "country", "reggae", "blues", "metal", "listen"
        };
        
        return musicKeywords.Any(kw => messageLower.Contains(kw));
    }

    private string? ExtractMusicGenre(string messageLower, string originalMessage)
    {
        var genres = new[] 
        { 
            "jazz", "rock", "pop", "hip-hop", "classical", "electronic", "country", 
            "reggae", "blues", "metal", "indie", "folk", "soul"
        };
        
        var foundGenre = genres.FirstOrDefault(g => messageLower.Contains(g));
        if (!string.IsNullOrEmpty(foundGenre))
            return foundGenre;

        // Try to extract artist/band name
        var artistKeywords = new[] { "artist", "band", "singer", "performer" };
        foreach (var keyword in artistKeywords)
        {
            var index = messageLower.IndexOf(keyword);
            if (index >= 0)
            {
                var afterKeyword = originalMessage.Substring(index + keyword.Length).Trim();
                var words = afterKeyword.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 0)
                {
                    var candidate = words[0];
                    if (candidate.Length > 2)
                        return candidate;
                }
            }
        }

        return null;
    }

    private string? ExtractCity(string messageLower)
    {
        var cities = new[] 
        { 
            "new york", "los angeles", "chicago", "houston", "miami", "seattle", "denver", 
            "austin", "boston", "philadelphia", "san francisco", "las vegas", "nashville",
            "nyc", "la", "sf", "dc"
        };
        
        var foundCity = cities.FirstOrDefault(c => messageLower.Contains(c));
        return foundCity;
    }

    private bool ShouldSearchByDate(string messageLower)
    {
        var dateKeywords = new[] 
        { 
            "today", "tomorrow", "week", "weekend", "month", "date", "when", "next", "this"
        };
        
        return dateKeywords.Any(kw => messageLower.Contains(kw));
    }

    private (DateTime fromDate, DateTime toDate) ExtractDateRange(string messageLower)
    {
        var today = DateTime.Today;
        
        if (messageLower.Contains("today"))
            return (today, today.AddDays(1));
        
        if (messageLower.Contains("tomorrow"))
            return (today.AddDays(1), today.AddDays(2));
        
        if (messageLower.Contains("this week"))
            return (today, today.AddDays(7));
        
        if (messageLower.Contains("this weekend"))
        {
            var daysToSaturday = (6 - (int)today.DayOfWeek) % 7;
            if (daysToSaturday == 0) daysToSaturday = 7;
            var saturday = today.AddDays(daysToSaturday);
            return (saturday, saturday.AddDays(2));
        }

        if (messageLower.Contains("next week"))
            return (today.AddDays(7), today.AddDays(14));

        if (messageLower.Contains("this month") || messageLower.Contains("next month"))
            return (today, today.AddMonths(1));

        return (today, today.AddDays(30)); // Default: next 30 days
    }

    private string GetSystemPrompt()
    {
        return @"You are an AI assistant for Ventixe, a modern event ticketing platform.
Your role is to help customers discover and book events they're interested in.

When a customer asks about events, be conversational and friendly. Respond in 1-3 sentences.

You can help with events by:
- Music genre or artist (jazz, rock, pop, etc.)
- Location (New York, Los Angeles, Chicago, etc.)
- Date (today, this weekend, next month, etc.)

Be helpful and enthusiastic! Ask clarifying questions if needed.";
    }
}

/// <summary>
/// Comparer for deduplicating EventDto objects by ID
/// </summary>
internal class EventDtoComparer : IEqualityComparer<EventDto>
{
    public bool Equals(EventDto? x, EventDto? y)
    {
        if (x == null || y == null) return false;
        return x.Id == y.Id;
    }

    public int GetHashCode(EventDto obj)
    {
        return obj.Id.GetHashCode();
    }
}
