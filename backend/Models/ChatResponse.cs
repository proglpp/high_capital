namespace backend.Models;

public class ChatResponse
{
    public string ConversationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = string.Empty;
    public Dictionary<string, object> Slots { get; set; } = new();
    public bool RequiresHuman { get; set; } = false;
    public List<string>? FunctionCalls { get; set; }
}

