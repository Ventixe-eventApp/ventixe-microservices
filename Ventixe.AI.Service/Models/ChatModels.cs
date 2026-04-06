namespace Ventixe.AI.Service.Models;

/// <summary>
/// Request model for sending chat messages
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = null!;
    public string? ConversationId { get; set; }
    public string? UserId { get; set; }
}

/// <summary>
/// Response model for chat messages
/// </summary>
public class ChatResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; } = null!;
    public string Message { get; set; } = null!;
    public List<EventDto> SuggestedEvents { get; set; } = [];
    public Dictionary<string, object>? SearchFilters { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event details DTO for chat responses
/// </summary>
public class EventDto
{
    public string Id { get; set; } = null!;
    public string EventName { get; set; } = null!;
    public string? ArtistName { get; set; }
    public string? Description { get; set; }
    public string Location { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public decimal Price { get; set; }
    public int AvailableTickets { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// Conversation start response
/// </summary>
public class ConversationStartResponse
{
    public string ConversationId { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a search filter extracted from user message
/// </summary>
public class EventSearchFilters
{
    public string? MusicGenre { get; set; }
    public string? Artist { get; set; }
    public string? City { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
