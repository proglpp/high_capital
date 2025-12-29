using backend.Models;
using Microsoft.Extensions.Caching.Memory;

namespace backend.Services;

public class ConversationService
{
    private readonly IMemoryCache _cache;
    private const int CACHE_EXPIRATION_MINUTES = 60;

    public ConversationService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Conversation GetOrCreateConversation(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId) || !_cache.TryGetValue(conversationId, out Conversation? conversation))
        {
            conversation = new Conversation { Id = conversationId ?? Guid.NewGuid().ToString() };
            _cache.Set(conversation.Id, conversation, TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES));
        }
        return conversation!;
    }

    public void UpdateConversation(Conversation conversation)
    {
        conversation.UpdatedAt = DateTime.UtcNow;
        _cache.Set(conversation.Id, conversation, TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES));
    }

    public void AddMessage(Conversation conversation, string role, string content)
    {
        conversation.Messages.Add(new Message
        {
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow
        });
        UpdateConversation(conversation);
    }

    public void UpdateSlot(Conversation conversation, string key, object value)
    {
        conversation.Slots[key] = value;
        UpdateConversation(conversation);
    }
}

