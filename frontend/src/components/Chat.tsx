import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import './Chat.css';

interface Message {
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

interface ChatResponse {
  conversationId: string;
  message: string;
  currentStage: string;
  slots: Record<string, any>;
  requiresHuman: boolean;
  functionCalls?: string[];
}

const Chat: React.FC = () => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [conversationId, setConversationId] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);
  const [slots, setSlots] = useState<Record<string, any>>({});
  const [currentStage, setCurrentStage] = useState<string>('greeting');
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Gera um novo ID de conversa ao montar o componente
    const newConversationId = `conv_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    setConversationId(newConversationId);
    
    // Mensagem inicial do assistente
    const welcomeMessage: Message = {
      role: 'assistant',
      content: 'Olá! Sou o assistente virtual da clínica. Como posso ajudá-lo hoje?',
      timestamp: new Date()
    };
    setMessages([welcomeMessage]);
  }, []);

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const sendMessage = async () => {
    if (!input.trim() || isLoading) return;

    const userMessage: Message = {
      role: 'user',
      content: input,
      timestamp: new Date()
    };

    setMessages(prev => [...prev, userMessage]);
    setInput('');
    setIsLoading(true);

    try {
      const response = await axios.post<ChatResponse>(
        'http://localhost:5000/api/chat/message',
        {
          conversationId: conversationId,
          message: input
        }
      );

      const assistantMessage: Message = {
        role: 'assistant',
        content: response.data.message,
        timestamp: new Date()
      };

      setMessages(prev => [...prev, assistantMessage]);
      setSlots(response.data.slots);
      setCurrentStage(response.data.currentStage);

      if (response.data.requiresHuman) {
        const humanMessage: Message = {
          role: 'assistant',
          content: 'Entendo sua necessidade. Vou transferir você para um atendente humano. Aguarde um momento, por favor.',
          timestamp: new Date()
        };
        setMessages(prev => [...prev, humanMessage]);
      }
    } catch (error) {
      console.error('Erro ao enviar mensagem:', error);
      const errorMessage: Message = {
        role: 'assistant',
        content: 'Desculpe, ocorreu um erro ao processar sua mensagem. Por favor, tente novamente.',
        timestamp: new Date()
      };
      setMessages(prev => [...prev, errorMessage]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  };

  const getStageLabel = (stage: string): string => {
    const stages: Record<string, string> = {
      greeting: 'Recepção',
      collect_info: 'Coletando Informações',
      confirm_unit: 'Confirmando Unidade',
      check_availability: 'Verificando Disponibilidade',
      schedule: 'Agendando'
    };
    return stages[stage] || stage;
  };

  return (
    <div className="chat-container">
      <div className="chat-header">
        <h1>💬 Chat - Clínica</h1>
        <div className="stage-indicator">
          <span className="stage-label">Etapa: {getStageLabel(currentStage)}</span>
        </div>
        {Object.keys(slots).length > 0 && (
          <div className="slots-info">
            <strong>Informações coletadas:</strong>
            <ul>
              {Object.entries(slots).map(([key, value]) => (
                <li key={key}>
                  <strong>{key}:</strong> {String(value)}
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>

      <div className="chat-messages">
        {messages.map((msg, index) => (
          <div
            key={index}
            className={`message ${msg.role === 'user' ? 'user-message' : 'assistant-message'}`}
          >
            <div className="message-content">
              {msg.content}
            </div>
            <div className="message-time">
              {msg.timestamp.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}
            </div>
          </div>
        ))}
        {isLoading && (
          <div className="message assistant-message">
            <div className="message-content">
              <span className="typing-indicator">●</span>
              <span className="typing-indicator">●</span>
              <span className="typing-indicator">●</span>
            </div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      <div className="chat-input-container">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyPress={handleKeyPress}
          placeholder="Digite sua mensagem..."
          disabled={isLoading}
          className="chat-input"
        />
        <button
          onClick={sendMessage}
          disabled={isLoading || !input.trim()}
          className="send-button"
        >
          Enviar
        </button>
      </div>
    </div>
  );
};

export default Chat;

