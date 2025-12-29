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

## 📄 Licença

Este projeto foi desenvolvido como desafio técnico.

## 👨‍💻 Autor

Desenvolvido como solução completa de agente de IA para clínicas médicas.

