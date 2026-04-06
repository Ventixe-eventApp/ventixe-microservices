using Dapper;
using Npgsql;
using Ventixe.AI.Service.Models;

namespace Ventixe.AI.Service.Services;

/// <summary>
/// Service for searching events from PostgreSQL database
/// </summary>
public interface IEventSearchService
{
    Task<List<EventDto>> SearchByMusicGenreAsync(string genre);
    Task<List<EventDto>> SearchByCityAsync(string city);
    Task<List<EventDto>> SearchByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<List<EventDto>> SearchByCombinedAsync(
        string? city = null,
        string? musicGenre = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
    Task<EventDto?> GetEventDetailsAsync(string eventId);
}

public class EventSearchService : IEventSearchService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<EventSearchService> _logger;
    private const int CommandTimeoutSeconds = 10;
    private const int MaxResults = 50;

    // Column selection constant to avoid duplication
    private const string EventSelectColumns = @"
        Id, EventName, ArtistName, Description, Location, 
        StartDate, Price, AvailableTickets, Category";

    public EventSearchService(NpgsqlDataSource dataSource, ILogger<EventSearchService> logger)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Search events by music genre or artist name
    /// </summary>
    public async Task<List<EventDto>> SearchByMusicGenreAsync(string genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
            return [];

        try
        {
            var sql = $@"
                SELECT {EventSelectColumns}
                FROM events
                WHERE LOWER(ArtistName) LIKE LOWER(@Genre) ESCAPE '\'
                   OR LOWER(Description) LIKE LOWER(@Genre) ESCAPE '\'
                   OR LOWER(Category) LIKE LOWER(@Genre) ESCAPE '\'
                ORDER BY StartDate ASC
                LIMIT @MaxResults";

            // Escape LIKE special characters to prevent DoS
            var escapedGenre = EscapeLikePattern(genre);
            
            await using var connection = await _dataSource.OpenConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandTimeout = CommandTimeoutSeconds;

            var result = (await connection.QueryAsync<EventDto>(sql, 
                new { Genre = $"%{escapedGenre}%", MaxResults = MaxResults },
                commandTimeout: CommandTimeoutSeconds)).ToList();
            
            _logger.LogInformation("Found {Count} events for genre: {Genre}", result.Count, genre);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching events by genre: {Genre}", genre);
            throw;
        }
    }

    /// <summary>
    /// Search events by city/location
    /// </summary>
    public async Task<List<EventDto>> SearchByCityAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return [];

        try
        {
            var sql = $@"
                SELECT {EventSelectColumns}
                FROM events
                WHERE LOWER(Location) LIKE LOWER(@City) ESCAPE '\'
                ORDER BY StartDate ASC
                LIMIT @MaxResults";

            var escapedCity = EscapeLikePattern(city);
            
            await using var connection = await _dataSource.OpenConnectionAsync();
            var result = (await connection.QueryAsync<EventDto>(sql,
                new { City = $"%{escapedCity}%", MaxResults = MaxResults },
                commandTimeout: CommandTimeoutSeconds)).ToList();
            
            _logger.LogInformation("Found {Count} events in city: {City}", result.Count, city);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching events by city: {City}", city);
            throw;
        }
    }

    /// <summary>
    /// Search events by date range
    /// </summary>
    public async Task<List<EventDto>> SearchByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        if (toDate < fromDate)
        {
            _logger.LogWarning("Invalid date range: fromDate {FromDate} > toDate {ToDate}", fromDate, toDate);
            return [];
        }

        try
        {
            var sql = $@"
                SELECT {EventSelectColumns}
                FROM events
                WHERE StartDate >= @FromDate 
                  AND StartDate <= @ToDate
                ORDER BY StartDate ASC
                LIMIT @MaxResults";

            await using var connection = await _dataSource.OpenConnectionAsync();
            var result = (await connection.QueryAsync<EventDto>(sql,
                new { FromDate = fromDate, ToDate = toDate, MaxResults = MaxResults },
                commandTimeout: CommandTimeoutSeconds)).ToList();
            
            _logger.LogInformation("Found {Count} events between {FromDate} and {ToDate}", result.Count, fromDate, toDate);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching events by date range");
            throw;
        }
    }

    /// <summary>
    /// Combined search with multiple filters
    /// </summary>
    public async Task<List<EventDto>> SearchByCombinedAsync(
        string? city = null,
        string? musicGenre = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            var sql = $@"
                SELECT {EventSelectColumns}
                FROM events
                WHERE 1 = 1";

            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(city))
            {
                sql += " AND LOWER(Location) LIKE LOWER(@City) ESCAPE '\\'";
                var escapedCity = EscapeLikePattern(city);
                parameters.Add("@City", $"%{escapedCity}%");
            }

            if (!string.IsNullOrEmpty(musicGenre))
            {
                sql += " AND (LOWER(ArtistName) LIKE LOWER(@Genre) ESCAPE '\\' OR LOWER(Category) LIKE LOWER(@Genre) ESCAPE '\\')";
                var escapedGenre = EscapeLikePattern(musicGenre);
                parameters.Add("@Genre", $"%{escapedGenre}%");
            }

            if (fromDate.HasValue)
            {
                sql += " AND StartDate >= @FromDate";
                parameters.Add("@FromDate", fromDate.Value);
            }

            if (toDate.HasValue)
            {
                sql += " AND StartDate <= @ToDate";
                parameters.Add("@ToDate", toDate.Value);
            }

            sql += $" ORDER BY StartDate ASC LIMIT @MaxResults";
            parameters.Add("@MaxResults", MaxResults);

            await using var connection = await _dataSource.OpenConnectionAsync();
            var result = (await connection.QueryAsync<EventDto>(sql, parameters,
                commandTimeout: CommandTimeoutSeconds)).ToList();
            
            _logger.LogInformation("Combined search found {Count} events", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in combined event search");
            throw;
        }
    }

    /// <summary>
    /// Get detailed information about a specific event
    /// </summary>
    public async Task<EventDto?> GetEventDetailsAsync(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId) || !Guid.TryParse(eventId, out _))
        {
            _logger.LogWarning("Invalid event ID format: {EventId}", eventId);
            return null;
        }

        try
        {
            var sql = $@"
                SELECT {EventSelectColumns}
                FROM events
                WHERE Id = @EventId";

            await using var connection = await _dataSource.OpenConnectionAsync();
            var result = await connection.QueryFirstOrDefaultAsync<EventDto>(sql,
                new { EventId = eventId },
                commandTimeout: CommandTimeoutSeconds);
            
            if (result != null)
                _logger.LogInformation("Retrieved details for event: {EventId}", eventId);
            else
                _logger.LogWarning("Event not found: {EventId}", eventId);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event details for: {EventId}", eventId);
            throw;
        }
    }

    /// <summary>
    /// Escape special LIKE pattern characters to prevent injection and DoS
    /// </summary>
    private static string EscapeLikePattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("\\", "\\\\")  // Backslash first
            .Replace("%", "\\%")    // Percent
            .Replace("_", "\\_");   // Underscore
    }
}
