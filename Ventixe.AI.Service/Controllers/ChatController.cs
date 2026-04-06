using Microsoft.AspNetCore.Mvc;
using Ventixe.AI.Service.Models;
using Ventixe.AI.Service.Services;

namespace Ventixe.AI.Service.Controllers;

/// <summary>
/// Chat API endpoints for AI-powered event discovery
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly IEventDiscoveryAgent _agent;
    private readonly IConversationService _conversationService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IEventDiscoveryAgent agent,
        IConversationService conversationService,
        ILogger<ChatController> logger)
    {
        _agent = agent;
        _conversationService = conversationService;
        _logger = logger;
    }

    /// <summary>
    /// Start a new chat conversation
    /// </summary>
    /// <returns>Conversation ID and welcome message</returns>
    [HttpPost("start")]
    [ProducesResponseType(typeof(ConversationStartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConversationStartResponse>> StartConversation([FromQuery] string? userId = null)
    {
        try
        {
            var conversationId = await _conversationService.StartConversationAsync(userId, "Event Discovery Chat");
            
            return Ok(new ConversationStartResponse
            {
                ConversationId = conversationId,
                Message = "Hi! 👋 Welcome to Ventixe! I'm your AI event assistant. I can help you find amazing events based on your interests.\n\nTell me:\n• What music genres interest you?\n• Which cities would you like to explore?\n• Are you looking for events on specific dates?\n\nLet's find your next favorite event!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting conversation");
            return StatusCode(500, new { error = "Failed to start conversation" });
        }
    }

    /// <summary>
    /// Send a message and get AI response
    /// </summary>
    /// <param name="request">Chat message request with conversation ID</param>
    /// <returns>AI response with suggested events</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message cannot be empty" });

            if (request.Message.Length > 2000)
                return BadRequest(new { error = "Message is too long (max 2000 characters)" });

            if (string.IsNullOrWhiteSpace(request.ConversationId))
                return BadRequest(new { error = "Conversation ID is required" });

            // Sanitize input
            var message = request.Message.Trim();

            _logger.LogInformation("Received message for conversation: {ConversationId}", request.ConversationId);

            // Process message through agent
            var response = await _agent.ProcessUserMessageAsync(
                message,
                request.ConversationId,
                request.UserId);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid conversation ID");
            return BadRequest(new { error = "Invalid conversation ID" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return StatusCode(500, new { error = "Failed to process message" });
        }
    }

    /// <summary>
    /// End a conversation session
    /// </summary>
    /// <param name="conversationId">The conversation ID to end</param>
    [HttpPost("{conversationId}/end")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EndConversation(string conversationId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return BadRequest(new { error = "Conversation ID is required" });

            await _conversationService.EndConversationAsync(conversationId);
            
            _logger.LogInformation("Ended conversation: {ConversationId}", conversationId);
            return Ok(new { message = "Conversation ended successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending conversation: {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Failed to end conversation" });
        }
    }

    /// <summary>
    /// Get conversation history (for debugging/review)
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    [HttpGet("{conversationId}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetHistory(string conversationId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return BadRequest(new { error = "Conversation ID is required" });

            var history = await _conversationService.GetConversationHistoryAsync(conversationId);
            
            return Ok(new
            {
                conversationId,
                messageCount = history.Count,
                messages = history.Select(m => new
                {
                    role = m.role,
                    content = m.content,
                    timestamp = m.timestamp
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation history: {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Failed to retrieve conversation history" });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
