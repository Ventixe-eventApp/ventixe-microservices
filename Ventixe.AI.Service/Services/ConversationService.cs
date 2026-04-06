using Dapper;
using System.Text.Json;
using Npgsql;
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
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<ConversationService> _logger;
    private const int CommandTimeoutSeconds = 10;
    private const int MaxHistoryLimit = 100;

    public ConversationService(NpgsqlDataSource dataSource, ILogger<ConversationService> logger)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            // Validate userId if provided
            Guid? parsedUserId = null;
            if (!string.IsNullOrEmpty(userId))
            {
                if (!Guid.TryParse(userId, out var parsed))
                {
                    _logger.LogWarning("Invalid userId format: {UserId}", userId);
                    return "";
                }
                parsedUserId = parsed;
            }

            const string sql = @"
                INSERT INTO conversations (id, user_id, title, started_at, last_message_at, message_count, created_at)
                VALUES (@Id, @UserId, @Title, @StartedAt, @LastMessageAt, 0, @CreatedAt)";

            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new
            {
                Id = conversationId,
                UserId = parsedUserId,
                Title = title ?? "Chat Session",
                StartedAt = now,
                LastMessageAt = now,
                CreatedAt = now
            }, commandTimeout: CommandTimeoutSeconds);

            _logger.LogInformation("Started conversation: {ConversationId} for user: {UserId}", conversationId, userId ?? "anonymous");
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
        if (!Guid.TryParse(conversationId, out var convId))
        {
            _logger.LogWarning("Invalid conversation ID format: {ConversationId}", conversationId);
            return;
        }

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

            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new
            {
                Id = messageId,
                ConversationId = convId,
                Role = role,
                Content = content,
                SearchFilters = filtersJson,
                SuggestedEvents = eventsJson,
                CreatedAt = now
            }, commandTimeout: CommandTimeoutSeconds);

            // Update conversation last message time and message count
            const string updateSql = @"
                UPDATE conversations 
                SET last_message_at = @LastMessageAt, message_count = message_count + 1
                WHERE id = @ConversationId";

            await connection.ExecuteAsync(updateSql, new
            {
                ConversationId = convId,
                LastMessageAt = now
            }, commandTimeout: CommandTimeoutSeconds);

            _logger.LogDebug("Saved message to conversation: {ConversationId}", conversationId);
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
        if (!Guid.TryParse(conversationId, out var convId))
        {
            _logger.LogWarning("Invalid conversation ID format: {ConversationId}", conversationId);
            return;
        }

        try
        {
            const string sql = @"
                UPDATE conversations 
                SET ended_at = @EndedAt
                WHERE id = @ConversationId";

            await using var connection = await _dataSource.OpenConnectionAsync();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                ConversationId = convId,
                EndedAt = DateTime.UtcNow
            }, commandTimeout: CommandTimeoutSeconds);

            _logger.LogInformation("Ended conversation: {ConversationId} (rows updated: {RowsAffected})", conversationId, rowsAffected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending conversation: {ConversationId}", conversationId);
            throw;
        }
    }

    /// <summary>
    /// Get conversation history with limit to prevent memory issues
    /// </summary>
    public async Task<List<(string role, string content, DateTime timestamp)>> GetConversationHistoryAsync(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out var convId))
        {
            _logger.LogWarning("Invalid conversation ID format: {ConversationId}", conversationId);
            return [];
        }

        try
        {
            const string sql = @"
                SELECT role, content, created_at
                FROM conversation_messages
                WHERE conversation_id = @ConversationId
                ORDER BY created_at ASC
                LIMIT @MaxLimit";

            await using var connection = await _dataSource.OpenConnectionAsync();
            var results = await connection.QueryAsync<(string role, string content, DateTime timestamp)>(
                sql, 
                new { ConversationId = convId, MaxLimit = MaxHistoryLimit },
                commandTimeout: CommandTimeoutSeconds);

            var messages = results.ToList();
            _logger.LogDebug("Retrieved {Count} messages for conversation: {ConversationId}", messages.Count, conversationId);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation history: {ConversationId}", conversationId);
            throw;
        }
    }
}
