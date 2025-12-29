# Agente de IA - SDR Digital para Clínicas

Sistema completo de agente de IA que simula um SDR (Sales Development Representative) digital para clínicas médicas, com memória contextual, base de conhecimento (RAG), function calling e fluxo conversacional inteligente.

## 🎯 Funcionalidades

### Fluxo Conversacional (5 Etapas)
1. **Recepção inicial** - Boas-vindas e apresentação
2. **Coleta de informações** - Nome e tipo de procedimento desejado
3. **Confirmação de unidade** - Seleção da unidade e horários disponíveis
4. **Verificação de disponibilidade** - Consulta de agenda
5. **Agendamento** - Confirmação e finalização

### Recursos Técnicos
- ✅ **Memória Contextual**: Short-term (resumos de conversa) + Long-term (vectorDB)
- ✅ **RAG (Retrieval-Augmented Generation)**: Base de conhecimento com embeddings
- ✅ **Function Calling**: Integração com funções externas (agenda, agendamento, confirmação)
- ✅ **Slot Filling**: Registro de variáveis ao longo da conversa
- ✅ **Fallback Inteligente**: Redirecionamento para humano quando necessário

## 🏗️ Arquitetura

```
┌─────────────┐
│   React     │  Frontend - Interface de Chat
│   Frontend  │
└──────┬──────┘
       │ HTTP/REST
┌──────▼──────────────────────────────────────┐
│         .NET Backend API                    │
│  ┌──────────────────────────────────────┐   │
│  │  Chat Controller                     │   │
│  │  - Gerenciamento de Conversas        │   │
│  │  - Orquestração do Fluxo             │   │
│  └──────────────────────────────────────┘   │
│  ┌──────────────────────────────────────┐   │
│  │  Agent Service                       │   │
│  │  - Integração OpenAI GPT-4o          │   │
│  │  - Function Calling                  │   │
│  │  - Slot Filling                      │   │
│  └──────────────────────────────────────┘   │
│  ┌──────────────────────────────────────┐   │
│  │  Memory Service                      │   │
│  │  - Short-term (resumos)              │   │
│  │  - Long-term (Qdrant VectorDB)       │   │
│  └──────────────────────────────────────┘   │
│  ┌──────────────────────────────────────┐   │
│  │  RAG Service                         │   │
│  │  - Embeddings (OpenAI)               │   │
│  │  - Busca semântica                   │   │
│  │  - Base de conhecimento (FAQ)        │   │
│  └──────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
       │
       ├─── OpenAI API (GPT-4o + Embeddings)
       └─── Qdrant VectorDB
```

## 📋 Pré-requisitos

- .NET 8.0 SDK
- Node.js 18+ e npm
- Qdrant (Docker ou instalação local)
- Conta OpenAI com API Key

## 🚀 Instalação e Execução

### 1. Backend (.NET)

```bash
cd backend
dotnet restore
dotnet build
dotnet run
```

O backend estará disponível em `http://localhost:5000`

### 2. Frontend (React)

```bash
cd frontend
npm install
npm start
```

O frontend estará disponível em `http://localhost:3000`

### 3. Qdrant (VectorDB)

```bash
docker run -p 6333:6333 qdrant/qdrant
```

Ou use o Qdrant Cloud.

## ⚙️ Configuração

1. Copie o arquivo `backend/env.example` e configure as variáveis de ambiente:

```bash
# Windows PowerShell
$env:OPENAI_API_KEY="your_openai_api_key"
$env:QDRANT_URL="http://localhost:6333"

# Linux/Mac
export OPENAI_API_KEY="your_openai_api_key"
export QDRANT_URL="http://localhost:6333"
```

Ou edite `backend/appsettings.json` diretamente.

## 📚 Estrutura do Projeto

```
.
├── backend/
│   ├── Controllers/
│   │   └── ChatController.cs
│   ├── Services/
│   │   ├── AgentService.cs
│   │   ├── MemoryService.cs
│   │   ├── RAGService.cs
│   │   ├── FunctionService.cs
│   │   └── ConversationService.cs
│   ├── Models/
│   │   ├── Conversation.cs
│   │   ├── Message.cs
│   │   ├── ChatRequest.cs
│   │   └── ChatResponse.cs
│   ├── Program.cs
│   └── appsettings.json
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   │   └── Chat.tsx
│   │   ├── App.tsx
│   │   └── index.tsx
│   └── package.json
└── README.md
```

## 🔄 Fluxo Conversacional

### Etapa 1: Recepção Inicial
- Agente se apresenta como SDR digital
- Pergunta como pode ajudar

### Etapa 2: Coleta de Informações
- Solicita nome do paciente
- Pergunta sobre o procedimento desejado
- Valida informações coletadas

### Etapa 3: Confirmação de Unidade
- Apresenta unidades disponíveis
- Solicita preferência de unidade
- Mostra horários disponíveis

### Etapa 4: Verificação de Disponibilidade
- Consulta agenda mockada
- Verifica disponibilidade do horário escolhido
- Confirma ou sugere alternativas

### Etapa 5: Agendamento
- Confirma todos os dados coletados
- Simula agendamento
- Envia confirmação
- Finaliza conversa

## 🧠 Estratégia de Memória

### Short-term Memory
- Resumos de janelas de contexto (últimas N mensagens)
- Armazenamento em memória durante a sessão
- Resumo automático quando o contexto excede limite

### Long-term Memory
- Armazenamento em Qdrant VectorDB
- Embeddings de conversas anteriores
- Busca semântica para contexto histórico

## 📖 Base de Conhecimento (RAG)

A base de conhecimento contém:
- FAQ sobre procedimentos
- Informações sobre unidades
- Políticas de agendamento
- Informações sobre cancelamentos

Os documentos são convertidos em embeddings e armazenados no Qdrant para busca semântica.

## 🔧 Function Calling

Funções implementadas:
1. `consultar_horarios_disponiveis` - Consulta agenda mockada
2. `agendar_consulta` - Simula agendamento
3. `enviar_confirmacao` - Envia mensagem de confirmação
4. `verificar_disponibilidade` - Verifica horário específico

## 📝 Slot Filling

Variáveis coletadas durante a conversa:
- `nome` - Nome do paciente
- `procedimento` - Tipo de procedimento desejado
- `unidade` - Unidade escolhida
- `horario` - Horário selecionado
- `data` - Data escolhida

## 🛡️ Fallback Inteligente

O sistema detecta quando:
- O usuário está insatisfeito
- A conversa não progride
- Há necessidade de atendimento humano

Nesses casos, redireciona para atendimento humano (simulado).

## 🏛️ Decisões Arquiteturais

### Por que .NET/C#?
- **Performance**: .NET 8.0 oferece excelente performance para APIs REST
- **Tipagem Forte**: Reduz erros em tempo de compilação
- **Ecossistema Maduro**: Grande suporte da comunidade e Microsoft
- **Escalabilidade**: Preparado para crescimento e alta demanda

### Por que React/TypeScript?
- **Interface Moderna**: Facilita criação de UIs responsivas e interativas
- **TypeScript**: Adiciona segurança de tipos ao JavaScript
- **Ecossistema Rico**: Grande quantidade de bibliotecas e componentes
- **Manutenibilidade**: Código mais fácil de manter e evoluir

### Por que Qdrant?
- **Open Source**: Sem custos de licenciamento
- **Performance**: Otimizado para busca vetorial
- **Fácil Deploy**: Pode rodar localmente (Docker) ou na nuvem
- **Compatibilidade**: Suporta embeddings de diferentes modelos

### Por que OpenAI GPT-4o?
- **Melhor Modelo**: GPT-4o oferece melhor qualidade de conversação
- **Function Calling Nativo**: Suporte nativo a chamadas de função
- **Embeddings de Qualidade**: text-embedding-3-small é eficiente e preciso
- **API Estável**: Infraestrutura confiável da OpenAI

### Estratégia de Memória
- **Short-term em Cache**: Rápido acesso durante a sessão
- **Long-term em VectorDB**: Permite busca semântica histórica
- **Resumos Automáticos**: Reduz custo de tokens mantendo contexto

### Estratégia de RAG
- **Embeddings Pré-calculados**: Base de conhecimento indexada uma vez
- **Busca Semântica**: Encontra informações relevantes mesmo com palavras diferentes
- **Threshold de Relevância**: Filtra resultados pouco relevantes (score > 0.7)

## 📋 Lista Completa de Funções Implementadas

### 1. consultar_horarios_disponiveis
- **Descrição**: Consulta os horários disponíveis para agendamento
- **Parâmetros**: `data` (opcional), `unidade` (opcional)
- **Retorno**: Lista de horários disponíveis para a data/unidade especificada
- **Uso**: Chamada quando o usuário pergunta sobre horários ou quando o agente precisa mostrar opções

### 2. verificar_disponibilidade
- **Descrição**: Verifica se um horário específico está disponível
- **Parâmetros**: `data` (obrigatório), `horario` (obrigatório), `unidade` (opcional)
- **Retorno**: Status de disponibilidade do horário
- **Uso**: Chamada quando o usuário escolhe um horário específico

### 3. agendar_consulta
- **Descrição**: Realiza o agendamento de uma consulta
- **Parâmetros**: `nome`, `procedimento`, `unidade`, `data`, `horario` (todos obrigatórios)
- **Retorno**: ID do agendamento e confirmação
- **Uso**: Chamada quando todos os dados foram coletados e o usuário confirma

### 4. enviar_confirmacao
- **Descrição**: Envia mensagem de confirmação do agendamento
- **Parâmetros**: `nome`, `data`, `horario`, `unidade` (todos obrigatórios)
- **Retorno**: Confirmação de envio da mensagem
- **Uso**: Chamada após agendamento bem-sucedido

*Detalhes completos em `PROMPTS.md`, `API_EXAMPLES.md` e `DOCUMENTACAO_COMPLETA.md`*



