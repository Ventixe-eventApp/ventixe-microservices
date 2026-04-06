using Dapper;
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
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<EventSearchService> _logger;

    public EventSearchService(IDbConnectionFactory dbConnectionFactory, ILogger<EventSearchService> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Search events by music genre or artist name
    /// </summary>
    public async Task<List<EventDto>> SearchByMusicGenreAsync(string genre)
    {
        try
        {
            const string sql = @"
                SELECT 
                    Id, EventName, ArtistName, Description, Location, 
                    StartDate, Price, AvailableTickets, Category
                FROM events
                WHERE LOWER(ArtistName) LIKE LOWER(@Genre) 
                   OR LOWER(Description) LIKE LOWER(@Genre)
                   OR LOWER(Category) LIKE LOWER(@Genre)
                ORDER BY StartDate ASC
                LIMIT 50";

            using var connection = _dbConnectionFactory.CreateConnection();
            var result = (await connection.QueryAsync<EventDto>(sql, new { Genre = $"%{genre}%" })).ToList();
            
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
        try
        {
            const string sql = @"
                SELECT 
                    Id, EventName, ArtistName, Description, Location, 
                    StartDate, Price, AvailableTickets, Category
                FROM events
                WHERE LOWER(Location) LIKE LOWER(@City)
                ORDER BY StartDate ASC
                LIMIT 50";

            using var connection = _dbConnectionFactory.CreateConnection();
            var result = (await connection.QueryAsync<EventDto>(sql, new { City = $"%{city}%" })).ToList();
            
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
        try
        {
            const string sql = @"
                SELECT 
                    Id, EventName, ArtistName, Description, Location, 
                    StartDate, Price, AvailableTickets, Category
                FROM events
                WHERE StartDate >= @FromDate 
                  AND StartDate <= @ToDate
                ORDER BY StartDate ASC
                LIMIT 50";

            using var connection = _dbConnectionFactory.CreateConnection();
            var result = (await connection.QueryAsync<EventDto>(sql, new { FromDate = fromDate, ToDate = toDate })).ToList();
            
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
            var sql = @"
                SELECT 
                    Id, EventName, ArtistName, Description, Location, 
                    StartDate, Price, AvailableTickets, Category
                FROM events
                WHERE 1 = 1";

            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(city))
            {
                sql += " AND LOWER(Location) LIKE LOWER(@City)";
                parameters.Add("@City", $"%{city}%");
            }

            if (!string.IsNullOrEmpty(musicGenre))
            {
                sql += " AND (LOWER(ArtistName) LIKE LOWER(@Genre) OR LOWER(Category) LIKE LOWER(@Genre))";
                parameters.Add("@Genre", $"%{musicGenre}%");
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

            sql += " ORDER BY StartDate ASC LIMIT 50";

            using var connection = _dbConnectionFactory.CreateConnection();
            var result = (await connection.QueryAsync<EventDto>(sql, parameters)).ToList();
            
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
        try
        {
            const string sql = @"
                SELECT 
                    Id, EventName, ArtistName, Description, Location, 
                    StartDate, Price, AvailableTickets, Category
                FROM events
                WHERE Id = @EventId";

            using var connection = _dbConnectionFactory.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<EventDto>(sql, new { EventId = eventId });
            
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
}
