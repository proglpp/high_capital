# Documentação Completa - Agente de IA SDR para Clínicas

## 📋 Checklist de Entregáveis

### ✅ Repositório GitHub
- [x] Código-fonte completo
- [x] Instruções de execução
- [x] Arquivos .env.example
- [x] README com explicações técnicas e decisões arquiteturais

### ✅ Documentação Mínima
- [x] Estrutura de fluxo implementado (diagrama ou texto)
- [x] Estratégia de memória/contexto
- [x] Lista de funções implementadas
- [x] Prompt base do agente

---

## 🔄 Estrutura de Fluxo Implementado

### Diagrama de Fluxo Conversacional

```
┌─────────────────────────────────────────────────────────────┐
│                    FLUXO CONVERSACIONAL                      │
└─────────────────────────────────────────────────────────────┘

1. GREETING (Recepção Inicial)
   │
   ├─> Agente se apresenta como SDR digital
   ├─> Pergunta como pode ajudar
   │
   ▼
2. COLLECT_INFO (Coleta de Informações)
   │
   ├─> Solicita nome do paciente
   ├─> Pergunta sobre procedimento desejado
   ├─> Valida informações coletadas
   │
   ▼
3. CONFIRM_UNIT (Confirmação de Unidade)
   │
   ├─> Apresenta unidades disponíveis
   ├─> Solicita preferência de unidade
   ├─> Mostra horários disponíveis (via function calling)
   │
   ▼
4. CHECK_AVAILABILITY (Verificação de Disponibilidade)
   │
   ├─> Consulta agenda mockada (via function calling)
   ├─> Verifica disponibilidade do horário escolhido
   ├─> Confirma ou sugere alternativas
   │
   ▼
5. SCHEDULE (Agendamento)
   │
   ├─> Confirma todos os dados coletados
   ├─> Simula agendamento (via function calling)
   ├─> Envia confirmação (via function calling)
   └─> Finaliza conversa
```

### Fluxo de Dados

```
Usuário → Frontend → Backend API → Agent Service
                                    │
                                    ├─> Memory Service (contexto)
                                    ├─> RAG Service (base conhecimento)
                                    ├─> Function Service (ações)
                                    └─> OpenAI GPT-4o
                                        │
                                        └─> Response → Frontend → Usuário
```

---

## 🧠 Estratégia de Memória/Contexto

### Short-term Memory (Memória de Curto Prazo)

**Implementação**: `MemoryService.cs`

**Características**:
- Armazena últimas 10 mensagens da conversa
- Usa `IMemoryCache` do .NET para armazenamento em memória
- Expira após 60 minutos de inatividade
- Janela deslizante: mantém apenas mensagens recentes

**Resumo Automático**:
- Quando a conversa excede 20 mensagens, cria resumo usando GPT-4o-mini
- Resumo mantém informações importantes (nome, procedimento, unidade, horários)
- Resumo substitui mensagens antigas no contexto
- Reduz custo de tokens mantendo contexto relevante

**Código**:
```csharp
// Janela de contexto
var recentMessages = messages.TakeLast(_maxContextMessages).ToList();

// Resumo quando necessário
if (messages.Count > _summaryThreshold && string.IsNullOrEmpty(conversation.Summary))
{
    conversation.Summary = await CreateSummaryAsync(messages);
}
```

### Long-term Memory (Memória de Longo Prazo)

**Implementação**: `RAGService.cs` + Qdrant VectorDB

**Características**:
- Armazena embeddings de conversas anteriores
- Usa Qdrant para busca semântica
- Permite recuperar contexto histórico relevante
- Busca por similaridade semântica (cosine similarity)

**Processo**:
1. Conversas são convertidas em embeddings (text-embedding-3-small)
2. Embeddings são armazenados no Qdrant com metadados
3. Busca semântica recupera conversas similares
4. Contexto histórico é injetado quando relevante

**Base de Conhecimento (RAG)**:
- FAQ sobre procedimentos
- Informações sobre unidades
- Políticas de agendamento
- Informações sobre cancelamentos
- Documentos necessários
- Planos de saúde aceitos

**Busca RAG**:
```csharp
// Busca semântica com threshold
var searchResults = await _qdrantClient.SearchAsync(
    _collectionName,
    queryEmbedding,
    limit: 3
);

// Filtra por relevância (score > 0.7)
return searchResults
    .Where(r => r.Score > 0.7)
    .Select(r => r.Payload["text"].StringValue)
    .ToList();
```

### Gerenciamento de Contexto

**Slots (Slot Filling)**:
- `nome`: Nome do paciente
- `procedimento`: Tipo de procedimento
- `unidade`: Unidade escolhida
- `data`: Data escolhida (YYYY-MM-DD)
- `horario`: Horário escolhido (HH:mm)

**Estágios da Conversa**:
- `greeting`: Recepção inicial
- `collect_info`: Coleta de informações
- `confirm_unit`: Confirmação de unidade
- `check_availability`: Verificação de disponibilidade
- `schedule`: Agendamento

**Transição de Estágios**:
- Automática baseada em slots preenchidos
- Validação de completude antes de avançar
- Fallback se informações estiverem incompletas

---

## 🔧 Lista de Funções Implementadas

### 1. consultar_horarios_disponiveis

**Localização**: `FunctionService.cs`

**Descrição**: Consulta os horários disponíveis para agendamento em uma data específica e unidade.

**Parâmetros**:
- `data` (string, opcional): Data no formato YYYY-MM-DD. Se não fornecido, usa próximo dia útil.
- `unidade` (string, opcional): Nome da unidade.

**Retorno**:
```json
{
  "success": true,
  "message": "Horários disponíveis para 2024-12-20: 08:00, 09:00, 10:00...",
  "data": {
    "data": "2024-12-20",
    "unidade": "Centro",
    "horarios_disponiveis": ["08:00", "09:00", "10:00", "14:00", "15:00", "16:00"],
    "total": 6
  }
}
```

**Quando é chamada**: Quando o usuário pergunta sobre horários disponíveis ou quando o agente precisa mostrar opções de agendamento.

**Implementação**: Agenda mockada com horários pré-definidos para algumas datas.

---

### 2. verificar_disponibilidade

**Localização**: `FunctionService.cs`

**Descrição**: Verifica se um horário específico está disponível.

**Parâmetros**:
- `data` (string, obrigatório): Data no formato YYYY-MM-DD
- `horario` (string, obrigatório): Horário no formato HH:mm
- `unidade` (string, opcional): Nome da unidade

**Retorno**:
```json
{
  "success": true,
  "message": "Horário 10:00 do dia 2024-12-20 está disponível!",
  "data": {
    "disponivel": true,
    "data": "2024-12-20",
    "horario": "10:00",
    "unidade": "Centro"
  }
}
```

**Quando é chamada**: Quando o usuário escolhe um horário específico e o agente precisa verificar se está disponível.

**Implementação**: Verifica na agenda mockada se o horário existe e está disponível.

---

### 3. agendar_consulta

**Localização**: `FunctionService.cs`

**Descrição**: Realiza o agendamento de uma consulta.

**Parâmetros**:
- `nome` (string, obrigatório): Nome completo do paciente
- `procedimento` (string, obrigatório): Tipo de procedimento desejado
- `unidade` (string, obrigatório): Unidade escolhida
- `data` (string, obrigatório): Data no formato YYYY-MM-DD
- `horario` (string, obrigatório): Horário no formato HH:mm

**Retorno**:
```json
{
  "success": true,
  "message": "Agendamento realizado com sucesso! ID: abc123-def456-ghi789",
  "data": {
    "agendamento_id": "abc123-def456-ghi789",
    "nome": "João Silva",
    "procedimento": "consulta",
    "unidade": "Centro",
    "data": "2024-12-20",
    "horario": "10:00",
    "status": "confirmado"
  }
}
```

**Quando é chamada**: Quando todos os dados necessários foram coletados e o usuário confirma o agendamento.

**Efeito colateral**: Remove o horário da agenda mockada para simular o bloqueio do horário.

---

### 4. enviar_confirmacao

**Localização**: `FunctionService.cs`

**Descrição**: Envia mensagem de confirmação do agendamento.

**Parâmetros**:
- `nome` (string, obrigatório): Nome do paciente
- `data` (string, obrigatório): Data do agendamento
- `horario` (string, obrigatório): Horário do agendamento
- `unidade` (string, obrigatório): Unidade do agendamento

**Retorno**:
```json
{
  "success": true,
  "message": "Olá João Silva, seu agendamento foi confirmado para 2024-12-20 às 10:00 na Unidade Centro. Aguardamos você!",
  "data": {
    "mensagem_enviada": true,
    "destinatario": "João Silva",
    "conteudo": "Olá João Silva, seu agendamento foi confirmado..."
  }
}
```

**Quando é chamada**: Após o agendamento ser realizado com sucesso.

**Implementação**: Simula envio de mensagem de confirmação (em produção, integraria com sistema de SMS/Email).

---

## 💬 Prompt Base do Agente

**Localização**: `AgentService.cs` (método `ProcessMessageAsync`)

**Prompt Completo**:

```
Você é um SDR (Sales Development Representative) digital para uma clínica médica. 
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

ESTÁGIO ATUAL: {currentStage}
SLOTS PREENCHIDOS: {slots}

{contextInfo}  // Informações da base de conhecimento (RAG) quando relevante
```

**Variáveis Dinâmicas**:
- `{currentStage}`: Estágio atual da conversa (greeting, collect_info, etc.)
- `{slots}`: Slots preenchidos até o momento (JSON)
- `{contextInfo}`: Informações relevantes da base de conhecimento (RAG)

**Integração com Function Calling**:
- O LLM decide automaticamente quando chamar funções
- Funções são definidas no formato OpenAI Function Calling
- Resultados das funções são injetados no contexto antes da resposta final

**Personalização**:
O prompt pode ser personalizado editando o `systemPrompt` em `AgentService.cs`.

---

## 📚 Arquivos de Referência

- **README.md**: Visão geral e instruções principais
- **PROMPTS.md**: Documentação detalhada de prompts e exemplos
- **API_EXAMPLES.md**: Exemplos de uso da API
- **backend/env.example**: Exemplo de variáveis de ambiente
- **backend/Services/AgentService.cs**: Implementação do agente
- **backend/Services/MemoryService.cs**: Implementação da memória
- **backend/Services/RAGService.cs**: Implementação do RAG
- **backend/Services/FunctionService.cs**: Implementação das funções

---

## ✅ Status

**TODOS OS REQUISITOS DE DOCUMENTAÇÃO FORAM ATENDIDOS!**

