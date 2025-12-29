using backend.Models;
using backend.Services;
using OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.Configuration;

namespace backend.Services;

public class MemoryService
{
    private readonly ConversationService _conversationService;
    private readonly OpenAIClient _openAIClient;
    private readonly RAGService _ragService;
    private readonly int _maxContextMessages;
    private readonly int _summaryThreshold;

    public MemoryService(
        ConversationService conversationService,
        RAGService ragService,
        IConfiguration configuration)
    {
        _conversationService = conversationService;
        _ragService = ragService;
        
        var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        _openAIClient = new OpenAIClient(apiKey);
        
        _maxContextMessages = int.Parse(configuration["Memory:MaxContextMessages"] ?? "10");
        _summaryThreshold = int.Parse(configuration["Memory:SummaryThreshold"] ?? "20");
    }

    public async Task<string> GetContextAsync(Conversation conversation)
    {
        var messages = conversation.Messages;
        
        // Se exceder o threshold, cria resumo
        if (messages.Count > _summaryThreshold && string.IsNullOrEmpty(conversation.Summary))
        {
            conversation.Summary = await CreateSummaryAsync(messages);
            _conversationService.UpdateConversation(conversation);
        }

        var contextMessages = new List<Message>();
        
        // Adiciona resumo se existir
        if (!string.IsNullOrEmpty(conversation.Summary))
        {
            contextMessages.Add(new Message
            {
                Role = "system",
                Content = $"Resumo da conversa anterior: {conversation.Summary}"
            });
        }

        // Adiciona últimas N mensagens
        var recentMessages = messages.TakeLast(_maxContextMessages).ToList();
        contextMessages.AddRange(recentMessages);

        // Adiciona slots preenchidos
        if (conversation.Slots.Any())
        {
            var slotsInfo = string.Join(", ", conversation.Slots.Select(s => $"{s.Key}: {s.Value}"));
            contextMessages.Add(new Message
            {
                Role = "system",
                Content = $"Informações coletadas: {slotsInfo}"
            });
        }

        return string.Join("\n", contextMessages.Select(m => $"{m.Role}: {m.Content}"));
    }

    private async Task<string> CreateSummaryAsync(List<Message> messages)
    {
        try
        {
            var conversationText = string.Join("\n", messages.Select(m => $"{m.Role}: {m.Content}"));
            
            var prompt = $@"Resuma a seguinte conversa de forma concisa, mantendo informações importantes como nome do paciente, procedimento desejado, unidade preferida e horários discutidos:

{conversationText}

Resumo:";

            var response = await _openAIClient.ChatEndpoint.GetCompletionAsync(
                new ChatRequest(
                    messages: new[]
                    {
                        new Message(ChatRole.User, prompt)
                    },
                    model: "gpt-4o-mini"
                )
            );

            return response.FirstChoice.Message.Content.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar resumo: {ex.Message}");
            return "";
        }
    }

    public async Task<List<string>> GetRelevantContextAsync(string query)
    {
        return await _ragService.SearchKnowledgeBaseAsync(query);
    }
}

