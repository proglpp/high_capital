using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AgentService _agentService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(AgentService agentService, ILogger<ChatController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            _logger.LogInformation($"Mensagem recebida para conversa {request.ConversationId}: {request.Message}");
            
            var response = await _agentService.ProcessMessageAsync(
                request.ConversationId,
                request.Message
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem");
            return StatusCode(500, new ChatResponse
            {
                ConversationId = request.ConversationId,
                Message = "Desculpe, ocorreu um erro ao processar sua mensagem. Por favor, tente novamente.",
                CurrentStage = "error"
            });
        }
    }

    [HttpGet("conversation/{conversationId}")]
    public ActionResult<Conversation> GetConversation(string conversationId)
    {
        // Esta rota pode ser usada para recuperar o estado da conversa
        // Por enquanto, retorna apenas um placeholder
        return Ok(new { message = "Endpoint de recuperação de conversa" });
    }
}

