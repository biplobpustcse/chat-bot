using Chatbot.API.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatMessage request)
    {
        var reply = await _chatService.GetChatResponseAsync(request.Message);
        return Ok(new { response = reply });
    }
}
