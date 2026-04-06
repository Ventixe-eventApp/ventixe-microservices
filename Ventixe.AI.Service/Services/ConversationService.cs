using Dapper;
using System.Text.Json;
using Ventixe.AI.Service.Models;

namespace Ventixe.AI.Service.Services;

/// <summary>
/// Service for managing conversation history in PostgreSQL
/// </summary>
public interface IConversationService
{
    Task<string> StartConversationAsync(string? userId = null, string? title = null);
    Task SaveMessageAsync(string conversationId, string role, string content, 
        EventSearchFilters? filters = null, List<EventDto>? suggestedEvents = null);
    Task EndConversationAsync(string conversationId);
    Task<List<(string role, string content, DateTime timestamp)>> GetConversationHistoryAsync(string conversationId);
}

public class ConversationService : IConversationService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(IDbConnectionFactory dbConnectionFactory, ILogger<ConversationService> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Start a new conversation session
    /// </summary>
    public async Task<string> StartConversationAsync(string? userId = null, string? title = null)
    {
        try
        {
            var conversationId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO conversations (id, user_id, title, started_at, last_message_at, message_count, created_at)
                VALUES (@Id, @UserId, @Title, @StartedAt, @LastMessageAt, 0, @CreatedAt)";

            using var connection = _dbConnectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, new
            {
                Id = conversationId,
                UserId = string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId),
                Title = title ?? "Chat Session",
                StartedAt = now,
                LastMessageAt = now,
                CreatedAt = now
            });

            _logger.LogInformation("Started conversation: {ConversationId}", conversationId);
            return conversationId.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting conversation");
            throw;
        }
    }

    /// <summary>
    /// Save a message to conversation history
    /// </summary>
    public async Task SaveMessageAsync(string conversationId, string role, string content,
        EventSearchFilters? filters = null, List<EventDto>? suggestedEvents = null)
    {
        try
        {
            var messageId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO conversation_messages 
                (id, conversation_id, role, content, search_filters, suggested_events, created_at)
                VALUES (@Id, @ConversationId, @Role, @Content, @SearchFilters, @SuggestedEvents, @CreatedAt)";

            var filtersJson = filters != null ? JsonSerializer.Serialize(filters) : null;
            var eventsJson = suggestedEvents?.Count > 0 ? JsonSerializer.Serialize(suggestedEvents) : null;

            using var connection = _dbConnectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, new
            {
                Id = messageId,
                ConversationId = Guid.Parse(conversationId),
                Role = role,
                Content = content,
                SearchFilters = filtersJson,
                SuggestedEvents = eventsJson,
                CreatedAt = now
            });

            // Update conversation last message time and message count
            const string updateSql = @"
                UPDATE conversations 
                SET last_message_at = @LastMessageAt, message_count = message_count + 1
                WHERE id = @ConversationId";

            await connection.ExecuteAsync(updateSql, new
            {
                ConversationId = Guid.Parse(conversationId),
                LastMessageAt = now
            });

            _logger.LogInformation("Saved message to conversation: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message to conversation: {ConversationId}", conversationId);
            throw;
        }
    }

    /// <summary>
    /// End a conversation session
    /// </summary>
    public async Task EndConversationAsync(string conversationId)
    {
        try
        {
            const string sql = @"
                UPDATE conversations 
                SET ended_at = @EndedAt
                WHERE id = @ConversationId";

            using var connection = _dbConnectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, new
            {
                ConversationId = Guid.Parse(conversationId),
                EndedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Ended conversation: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending conversation: {ConversationId}", conversationId);
            throw;
        }
    }

    /// <summary>
    /// Get conversation history
    /// </summary>
    public async Task<List<(string role, string content, DateTime timestamp)>> GetConversationHistoryAsync(string conversationId)
    {
        try
        {
            const string sql = @"
                SELECT role, content, created_at
                FROM conversation_messages
                WHERE conversation_id = @ConversationId
                ORDER BY created_at ASC";

            using var connection = _dbConnectionFactory.CreateConnection();
            var results = await connection.QueryAsync<(string role, string content, DateTime timestamp)>(
                sql, 
                new { ConversationId = Guid.Parse(conversationId) });

            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation history: {ConversationId}", conversationId);
            throw;
        }
    }
}
