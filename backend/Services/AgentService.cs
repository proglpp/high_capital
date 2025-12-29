using backend.Models;
using backend.Services;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace backend.Services;

public class AgentService
{
    private readonly OpenAIClient _openAIClient;
    private readonly MemoryService _memoryService;
    private readonly FunctionService _functionService;
    private readonly ConversationService _conversationService;
    private readonly string _model;

    // Definição das funções disponíveis
    private readonly List<Function> _availableFunctions = new()
    {
        new Function
        {
            Name = "consultar_horarios_disponiveis",
            Description = "Consulta os horários disponíveis para agendamento em uma data específica e unidade",
            Parameters = JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    data = new { type = "string", description = "Data no formato YYYY-MM-DD" },
                    unidade = new { type = "string", description = "Nome da unidade" }
                }
            })
        },
        new Function
        {
            Name = "verificar_disponibilidade",
            Description = "Verifica se um horário específico está disponível",
            Parameters = JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    data = new { type = "string", description = "Data no formato YYYY-MM-DD" },
                    horario = new { type = "string", description = "Horário no formato HH:mm" },
                    unidade = new { type = "string", description = "Nome da unidade" }
                },
                required = new[] { "data", "horario" }
            })
        },
        new Function
        {
            Name = "agendar_consulta",
            Description = "Realiza o agendamento de uma consulta",
            Parameters = JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    nome = new { type = "string", description = "Nome completo do paciente" },
                    procedimento = new { type = "string", description = "Tipo de procedimento desejado" },
                    unidade = new { type = "string", description = "Unidade escolhida" },
                    data = new { type = "string", description = "Data no formato YYYY-MM-DD" },
                    horario = new { type = "string", description = "Horário no formato HH:mm" }
                },
                required = new[] { "nome", "procedimento", "unidade", "data", "horario" }
            })
        },
        new Function
        {
            Name = "enviar_confirmacao",
            Description = "Envia mensagem de confirmação do agendamento",
            Parameters = JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    nome = new { type = "string", description = "Nome do paciente" },
                    data = new { type = "string", description = "Data do agendamento" },
                    horario = new { type = "string", description = "Horário do agendamento" },
                    unidade = new { type = "string", description = "Unidade do agendamento" }
                },
                required = new[] { "nome", "data", "horario", "unidade" }
            })
        }
    };

    public AgentService(
        MemoryService memoryService,
        FunctionService functionService,
        ConversationService conversationService,
        IConfiguration configuration)
    {
        _memoryService = memoryService;
        _functionService = functionService;
        _conversationService = conversationService;
        
        var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        _openAIClient = new OpenAIClient(apiKey);
        _model = configuration["OpenAI:Model"] ?? "gpt-4o";
    }

    public async Task<ChatResponse> ProcessMessageAsync(string conversationId, string userMessage)
    {
        var conversation = _conversationService.GetOrCreateConversation(conversationId);
        
        // Adiciona mensagem do usuário
        _conversationService.AddMessage(conversation, "user", userMessage);

        // Busca contexto relevante (RAG)
        var relevantContext = await _memoryService.GetRelevantContextAsync(userMessage);
        var contextInfo = relevantContext.Any() 
            ? $"\n\nInformações relevantes da base de conhecimento:\n{string.Join("\n", relevantContext)}"
            : "";

        // Obtém contexto da conversa
        var conversationContext = await _memoryService.GetContextAsync(conversation);

        // Sistema de prompt com instruções do agente
        var systemPrompt = $@"Você é um SDR (Sales Development Representative) digital para uma clínica médica. 
Seu papel é ajudar pacientes a agendar consultas de forma amigável e profissional.

ETAPAS DO FLUXO:
1. GREETING: Cumprimente o paciente e se apresente
2. COLLECT_INFO: Colete o nome do paciente e o tipo de procedimento desejado
3. CONFIRM_UNIT: Apresente as unidades disponíveis e confirme a preferência, depois mostre horários
4. CHECK_AVAILABILITY: Verifique a disponibilidade do horário escolhido
5. SCHEDULE: Confirme todos os dados e realize o agendamento

REGRAS IMPORTANTES:
- Seja sempre educado, empático e profissional
- Use as funções disponíveis quando necessário
- Preencha os slots (nome, procedimento, unidade, data, horario) conforme coletar informações
- Se o paciente estiver insatisfeito ou pedir para falar com humano, defina requiresHuman = true
- Mantenha a conversa natural e fluida
- Use as informações da base de conhecimento quando relevante

ESTÁGIO ATUAL: {conversation.CurrentStage}
SLOTS PREENCHIDOS: {JsonSerializer.Serialize(conversation.Slots)}

{contextInfo}";

        // Prepara mensagens para o LLM
        var messages = new List<Message>
        {
            new Message(ChatRole.System, systemPrompt)
        };

        // Adiciona histórico recente (últimas 10 mensagens, excluindo a atual)
        var recentMessages = conversation.Messages.TakeLast(10).ToList();
        foreach (var msg in recentMessages)
        {
            if (msg.Role == "user")
            {
                messages.Add(new Message(ChatRole.User, msg.Content));
            }
            else if (msg.Role == "assistant")
            {
                messages.Add(new Message(ChatRole.Assistant, msg.Content));
            }
        }

        // Chama o LLM com function calling
        var chatRequest = new ChatRequest(
            messages: messages.ToArray(),
            model: _model,
            functions: _availableFunctions.ToArray(),
            functionCall: FunctionCall.Auto
        );

        var response = await _openAIClient.ChatEndpoint.GetCompletionAsync(chatRequest);
        var assistantMessage = response.FirstChoice.Message;

        var functionCalls = new List<string>();

        // Processa function calls se houver
        if (assistantMessage.FunctionCall != null)
        {
            var functionResult = await ExecuteFunctionAsync(assistantMessage.FunctionCall, conversation);
            functionCalls.Add(assistantMessage.FunctionCall.Name ?? "");

            // Adiciona resultado da função ao contexto
            messages.Add(new Message(ChatRole.Assistant, assistantMessage.Content?.ToString() ?? ""));
            messages.Add(new Message(ChatRole.Function, functionResult.Message)
            {
                Name = assistantMessage.FunctionCall.Name
            });

            // Chama novamente para obter resposta final
            var finalResponse = await _openAIClient.ChatEndpoint.GetCompletionAsync(
                new ChatRequest(messages: messages.ToArray(), model: _model)
            );
            assistantMessage = finalResponse.FirstChoice.Message;
        }

        var assistantContent = assistantMessage.Content?.ToString() ?? "Desculpe, não consegui processar sua mensagem.";

        // Adiciona resposta do assistente
        _conversationService.AddMessage(conversation, "assistant", assistantContent);

        // Atualiza estágio da conversa baseado no conteúdo
        UpdateConversationStage(conversation, userMessage, assistantContent);

        // Extrai slots da conversa
        ExtractSlots(conversation, userMessage, assistantContent);

        // Verifica se precisa de humano
        var requiresHuman = CheckIfRequiresHuman(userMessage, assistantContent);

        return new ChatResponse
        {
            ConversationId = conversation.Id,
            Message = assistantContent,
            CurrentStage = conversation.CurrentStage,
            Slots = conversation.Slots,
            RequiresHuman = requiresHuman,
            FunctionCalls = functionCalls.Any() ? functionCalls : null
        };
    }

    private async Task<FunctionResult> ExecuteFunctionAsync(FunctionCall functionCall, Conversation conversation)
    {
        var functionName = functionCall.Name ?? "";
        var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            functionCall.Arguments?.ToString() ?? "{}"
        ) ?? new Dictionary<string, JsonElement>();

        return functionName switch
        {
            "consultar_horarios_disponiveis" => _functionService.ConsultarHorariosDisponiveis(
                arguments.ContainsKey("data") ? arguments["data"].GetString() : null,
                arguments.ContainsKey("unidade") ? arguments["unidade"].GetString() : null
            ),
            "verificar_disponibilidade" => _functionService.VerificarDisponibilidade(
                arguments["data"].GetString() ?? "",
                arguments["horario"].GetString() ?? "",
                arguments.ContainsKey("unidade") ? arguments["unidade"].GetString() : null
            ),
            "agendar_consulta" => _functionService.AgendarConsulta(
                arguments["nome"].GetString() ?? "",
                arguments["procedimento"].GetString() ?? "",
                arguments["unidade"].GetString() ?? "",
                arguments["data"].GetString() ?? "",
                arguments["horario"].GetString() ?? ""
            ),
            "enviar_confirmacao" => _functionService.EnviarConfirmacao(
                arguments["nome"].GetString() ?? "",
                arguments["data"].GetString() ?? "",
                arguments["horario"].GetString() ?? "",
                arguments["unidade"].GetString() ?? ""
            ),
            _ => new FunctionResult { Success = false, Message = "Função não encontrada" }
        };
    }

    private void UpdateConversationStage(Conversation conversation, string userMessage, string assistantMessage)
    {
        var lowerUser = userMessage.ToLower();
        var lowerAssistant = assistantMessage.ToLower();

        // Lógica de transição de estágios
        if (conversation.CurrentStage == "greeting" && !string.IsNullOrEmpty(userMessage))
        {
            conversation.CurrentStage = "collect_info";
        }
        else if (conversation.CurrentStage == "collect_info" && 
                 conversation.Slots.ContainsKey("nome") && 
                 conversation.Slots.ContainsKey("procedimento"))
        {
            conversation.CurrentStage = "confirm_unit";
        }
        else if (conversation.CurrentStage == "confirm_unit" && 
                 conversation.Slots.ContainsKey("unidade"))
        {
            conversation.CurrentStage = "check_availability";
        }
        else if (conversation.CurrentStage == "check_availability" && 
                 conversation.Slots.ContainsKey("data") && 
                 conversation.Slots.ContainsKey("horario"))
        {
            conversation.CurrentStage = "schedule";
        }

        _conversationService.UpdateConversation(conversation);
    }

    private void ExtractSlots(Conversation conversation, string userMessage, string assistantMessage)
    {
        // Extrai nome
        if (!conversation.Slots.ContainsKey("nome"))
        {
            var namePatterns = new[] { "meu nome é", "eu sou", "me chamo", "sou o", "sou a" };
            foreach (var pattern in namePatterns)
            {
                var index = userMessage.ToLower().IndexOf(pattern);
                if (index >= 0)
                {
                    var namePart = userMessage.Substring(index + pattern.Length).Trim();
                    var name = namePart.Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrEmpty(name))
                    {
                        _conversationService.UpdateSlot(conversation, "nome", name);
                        break;
                    }
                }
            }
        }

        // Extrai procedimento
        if (!conversation.Slots.ContainsKey("procedimento"))
        {
            var procedimentos = new[] { "consulta", "exame", "ultrassom", "raio-x", "check-up", "vacina" };
            foreach (var proc in procedimentos)
            {
                if (userMessage.ToLower().Contains(proc))
                {
                    _conversationService.UpdateSlot(conversation, "procedimento", proc);
                    break;
                }
            }
        }

        // Extrai unidade
        if (!conversation.Slots.ContainsKey("unidade"))
        {
            var unidades = new[] { "centro", "zona sul", "zona norte" };
            foreach (var unidade in unidades)
            {
                if (userMessage.ToLower().Contains(unidade))
                {
                    _conversationService.UpdateSlot(conversation, "unidade", unidade);
                    break;
                }
            }
        }

        // Extrai data e horário (formato simples)
        if (userMessage.Contains("às") || userMessage.Contains("as"))
        {
            var parts = userMessage.Split(new[] { "às", "as" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var datePart = parts[0].Trim();
                var timePart = parts[1].Trim();
                
                if (DateTime.TryParse(datePart, out var date))
                {
                    _conversationService.UpdateSlot(conversation, "data", date.ToString("yyyy-MM-dd"));
                }
                
                if (TimeSpan.TryParse(timePart, out var time))
                {
                    _conversationService.UpdateSlot(conversation, "horario", time.ToString(@"hh\:mm"));
                }
            }
        }
    }

    private bool CheckIfRequiresHuman(string userMessage, string assistantMessage)
    {
        var lowerUser = userMessage.ToLower();
        var indicators = new[]
        {
            "quero falar com humano",
            "atendente humano",
            "pessoa real",
            "não estou satisfeito",
            "não gostei",
            "problema",
            "reclamação"
        };

        return indicators.Any(indicator => lowerUser.Contains(indicator));
    }
}

