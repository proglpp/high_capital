using OpenAI;
using OpenAI.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace backend.Services;

public class RAGService
{
    private readonly OpenAIClient _openAIClient;
    private readonly QdrantClient _qdrantClient;
    private readonly string _collectionName;
    private readonly string _embeddingModel;
    private readonly Dictionary<string, string> _knowledgeBase;

    public RAGService(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        _openAIClient = new OpenAIClient(apiKey);
        
        var qdrantUrl = configuration["Qdrant:Url"] ?? Environment.GetEnvironmentVariable("QDRANT_URL") ?? "http://localhost:6333";
        var qdrantApiKey = configuration["Qdrant:ApiKey"] ?? Environment.GetEnvironmentVariable("QDRANT_API_KEY");
        
        _qdrantClient = string.IsNullOrEmpty(qdrantApiKey) 
            ? new QdrantClient(qdrantUrl) 
            : new QdrantClient(qdrantUrl, apiKey: qdrantApiKey);
        
        _collectionName = configuration["Qdrant:CollectionName"] ?? "clinic_knowledge";
        _embeddingModel = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

        // Base de conhecimento inicial (FAQ)
        _knowledgeBase = new Dictionary<string, string>
        {
            {
                "procedimentos",
                "Oferecemos os seguintes procedimentos: Consultas médicas gerais, Exames de sangue, Ultrassonografia, Raio-X, Consultas com especialistas (cardiologista, dermatologista, ginecologista), Check-ups completos, Vacinação."
            },
            {
                "horarios",
                "Nossos horários de funcionamento são: Segunda a Sexta das 7h às 19h, Sábados das 8h às 14h. Não atendemos aos domingos."
            },
            {
                "unidades",
                "Temos 3 unidades disponíveis: Unidade Centro (Rua Principal, 123), Unidade Zona Sul (Av. Beira Mar, 456), Unidade Zona Norte (Rua Comercial, 789). Todas as unidades oferecem os mesmos serviços."
            },
            {
                "cancelamento",
                "Para cancelar ou remarcar um agendamento, entre em contato com pelo menos 24 horas de antecedência. Você pode cancelar pelo chat ou ligando para nossa central."
            },
            {
                "documentos",
                "Para o atendimento, traga um documento de identidade com foto e, se tiver, carteirinha do plano de saúde ou comprovante de pagamento particular."
            },
            {
                "planos",
                "Aceitamos os principais planos de saúde e também atendemos particular. Consulte nossa equipe para verificar se seu plano é aceito."
            }
        };

        // Inicializa base de conhecimento de forma assíncrona
        _ = Task.Run(async () => await InitializeKnowledgeBase());
    }

    private async Task InitializeKnowledgeBase()
    {
        try
        {
            // Verifica se a coleção existe
            var collections = await _qdrantClient.ListCollectionsAsync();
            var collectionExists = collections.Any(c => c == _collectionName);

            if (!collectionExists)
            {
                // Cria a coleção
                await _qdrantClient.CreateCollectionAsync(
                    _collectionName,
                    new VectorParams { Size = 1536, Distance = Distance.Cosine }
                );

                // Adiciona documentos à base de conhecimento
                await AddDocumentsToKnowledgeBase();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inicializar base de conhecimento: {ex.Message}");
        }
    }

    private async Task AddDocumentsToKnowledgeBase()
    {
        uint pointId = 1;
        foreach (var kvp in _knowledgeBase)
        {
            try
            {
                var embedding = await GetEmbeddingAsync(kvp.Value);
                
                await _qdrantClient.UpsertAsync(
                    _collectionName,
                    new[]
                    {
                        new PointStruct
                        {
                            Id = pointId++,
                            Vectors = embedding,
                            Payload = new Dictionary<string, Value>
                            {
                                { "text", kvp.Value },
                                { "category", kvp.Key }
                            }
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao adicionar documento {kvp.Key}: {ex.Message}");
            }
        }
    }

    private async Task<float[]> GetEmbeddingAsync(string text)
    {
        var response = await _openAIClient.EmbeddingsEndpoint.CreateEmbeddingAsync(
            new EmbeddingRequest(text, model: _embeddingModel)
        );

        return response.Data[0].Embedding.Select(e => (float)e).ToArray();
    }

    public async Task<List<string>> SearchKnowledgeBaseAsync(string query, int topK = 3)
    {
        try
        {
            var queryEmbedding = await GetEmbeddingAsync(query);

            var searchResults = await _qdrantClient.SearchAsync(
                _collectionName,
                queryEmbedding,
                limit: (ulong)topK
            );

            return searchResults
                .Where(r => r.Score > 0.7) // Threshold de relevância
                .Select(r => r.Payload["text"].StringValue)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na busca RAG: {ex.Message}");
            return new List<string>();
        }
    }
}

