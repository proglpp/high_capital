namespace backend.Models;

public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<Message> Messages { get; set; } = new();
    public Dictionary<string, object> Slots { get; set; } = new();
    public string CurrentStage { get; set; } = "greeting"; // greeting, collect_info, confirm_unit, check_availability, schedule
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool RequiresHuman { get; set; } = false;
}

