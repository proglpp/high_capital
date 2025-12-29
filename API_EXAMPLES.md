# Exemplos de Uso da API

## Endpoint Principal

### POST /api/chat/message

Envia uma mensagem para o agente e recebe uma resposta.

#### Request

```json
{
  "conversationId": "conv_1234567890",
  "message": "Olá, quero agendar uma consulta"
}
```

#### Response

```json
{
  "conversationId": "conv_1234567890",
  "message": "Olá! Sou o assistente virtual da clínica. Como posso ajudá-lo hoje? Você gostaria de agendar uma consulta?",
  "currentStage": "greeting",
  "slots": {},
  "requiresHuman": false,
  "functionCalls": null
}
```

## Exemplos de Conversação Completa

### Exemplo 1: Agendamento Completo

#### Mensagem 1
```http
POST /api/chat/message
Content-Type: application/json

{
  "conversationId": "conv_001",
  "message": "Olá"
}
```

**Resposta:**
```json
{
  "conversationId": "conv_001",
  "message": "Olá! Sou o assistente virtual da clínica. Como posso ajudá-lo hoje?",
  "currentStage": "greeting",
  "slots": {}
}
```

#### Mensagem 2
```http
POST /api/chat/message
Content-Type: application/json

{
  "conversationId": "conv_001",
  "message": "Quero agendar uma consulta"
}
```

**Resposta:**
```json
{
  "conversationId": "conv_001",
  "message": "Perfeito! Para começar, qual é o seu nome?",
  "currentStage": "collect_info",
  "slots": {}
}
```

#### Mensagem 3
```http
POST /api/chat/message
Content-Type: application/json

{
  "conversationId": "conv_001",
  "message": "Meu nome é Maria Santos"
}
```

**Resposta:**
```json
{
  "conversationId": "conv_001",
  "message": "Prazer em conhecê-la, Maria! Que tipo de procedimento você gostaria de realizar?",
  "currentStage": "collect_info",
  "slots": {
    "nome": "Maria"
  }
}
```

#### Mensagem 4
```http
POST /api/chat/message
Content-Type: application/json

{
  "conversationId": "conv_001",
  "message": "Quero fazer uma consulta médica geral"
}
```

**Resposta:**
```json
{
  "conversationId": "conv_001",
  "message": "Ótimo! Temos 3 unidades disponíveis: Unidade Centro, Unidade Zona Sul e Unidade Zona Norte. Qual você prefere?",
  "currentStage": "confirm_unit",
  "slots": {
    "nome": "Maria",
    "procedimento": "consulta"
  },
  "functionCalls": null
}
```

#### Mensagem 5
```http
POST /api/chat/message
Content-Type: application/json

{
  "conversationId": "conv_001",
  "message": "Prefiro a Unidade Centro"
}
```

**Resposta:**
```json
{
  "conversationId": "conv_001",
  "message": "Perfeito! Vou verificar os horários disponíveis para a Unidade Centro. Para qual data você gostaria de agendar?",
  "currentStage": "confirm_unit",
  "slots": {
    "nome": "Maria",
    "procedimento": "consulta",
    "unidade": "centro"
  },
  "functionCalls": ["consultar_horarios_disponiveis"]
}
```

#### Mensagem 6
```http
POST /api/chat/message
Content-Type: application/json

{
  "conversationId": "conv_001",
  "message": "Amanhã às 10h"
}
```

**Resposta:**
```json
{
  "conversationId": "conv_001",
  "message": "Deixe-me verificar a disponibilidade para amanhã às 10h na Unidade Centro...",
  "currentStage": "check_availability",
  "slots": {
    "nome": "Maria",
    "procedimento": "consulta",
    "unidade": "centro",
    "data": "2024-12-21",
    "horario": "10:00"
  },
  "functionCalls": ["verificar_disponibilidade"]
}
```

#### Mensagem 7
```http
POST /api/chat/message
Content-Type: application/json

{
  "conversationId": "conv_001",
  "message": "Sim, confirma"
}
```

**Resposta:**
```json
{
  "conversationId": "conv_001",
  "message": "Perfeito! Seu agendamento foi realizado com sucesso. Você receberá uma confirmação em breve.",
  "currentStage": "schedule",
  "slots": {
    "nome": "Maria",
    "procedimento": "consulta",
    "unidade": "centro",
    "data": "2024-12-21",
    "horario": "10:00"
  },
  "functionCalls": ["agendar_consulta", "enviar_confirmacao"]
}
```

## Exemplo 2: Consulta à Base de Conhecimento (RAG)

```http
POST /api/chat/message
Content-Type: application/json

{
  "conversationId": "conv_002",
  "message": "Quais procedimentos vocês oferecem?"
}
```

**Resposta:**
```json
{
  "conversationId": "conv_002",
  "message": "Oferecemos os seguintes procedimentos: Consultas médicas gerais, Exames de sangue, Ultrassonografia, Raio-X, Consultas com especialistas (cardiologista, dermatologista, ginecologista), Check-ups completos, Vacinação.",
  "currentStage": "greeting",
  "slots": {}
}
```

*Nota: A resposta usa informações da base de conhecimento (RAG) para fornecer informações precisas.*

## Exemplo 3: Fallback para Humano

```http
POST /api/chat/message
Content-Type: application/json

{
  "conversationId": "conv_003",
  "message": "Quero falar com um atendente humano"
}
```

**Resposta:**
```json
{
  "conversationId": "conv_003",
  "message": "Entendo sua necessidade. Vou transferir você para um atendente humano. Aguarde um momento, por favor.",
  "currentStage": "greeting",
  "slots": {},
  "requiresHuman": true
}
```

## Testando com cURL

### Windows PowerShell
```powershell
$body = @{
    conversationId = "conv_test_001"
    message = "Olá"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/chat/message" -Method Post -Body $body -ContentType "application/json"
```

### Linux/Mac
```bash
curl -X POST http://localhost:5000/api/chat/message \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": "conv_test_001",
    "message": "Olá"
  }'
```

## Testando com Swagger

1. Inicie o backend
2. Acesse `http://localhost:5000/swagger`
3. Expanda o endpoint `POST /api/chat/message`
4. Clique em "Try it out"
5. Preencha os dados e clique em "Execute"

## Observações Importantes

1. **ConversationId**: Use o mesmo `conversationId` para manter o contexto da conversa
2. **Slots**: Os slots são preenchidos automaticamente conforme a conversa progride
3. **Function Calls**: Funções são chamadas automaticamente quando necessário
4. **Stages**: O estágio atual indica em qual etapa do fluxo a conversa está
5. **Requires Human**: Quando `true`, indica que o usuário precisa de atendimento humano

