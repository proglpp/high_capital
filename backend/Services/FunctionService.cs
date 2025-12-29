using backend.Models;
using System.Text.Json;

namespace backend.Services;

public class FunctionService
{
    // Agenda mockada
    private readonly Dictionary<string, List<string>> _mockSchedule = new()
    {
        { "2024-12-20", new List<string> { "08:00", "09:00", "10:00", "14:00", "15:00", "16:00" } },
        { "2024-12-21", new List<string> { "08:00", "09:00", "10:00", "11:00", "14:00", "15:00", "16:00", "17:00" } },
        { "2024-12-22", new List<string> { "08:00", "09:00", "10:00", "14:00", "15:00" } },
        { "2024-12-23", new List<string> { "08:00", "09:00", "10:00", "11:00", "14:00", "15:00", "16:00" } },
        { "2024-12-24", new List<string> { "08:00", "09:00", "10:00" } }
    };

    public FunctionResult ConsultarHorariosDisponiveis(string? data = null, string? unidade = null)
    {
        var targetDate = data ?? DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
        
        if (_mockSchedule.ContainsKey(targetDate))
        {
            var horarios = _mockSchedule[targetDate];
            return new FunctionResult
            {
                Success = true,
                Data = new
                {
                    data = targetDate,
                    unidade = unidade ?? "Não especificada",
                    horarios_disponiveis = horarios,
                    total = horarios.Count
                },
                Message = $"Horários disponíveis para {targetDate}: {string.Join(", ", horarios)}"
            };
        }

        return new FunctionResult
        {
            Success = false,
            Message = $"Não há horários disponíveis para a data {targetDate}"
        };
    }

    public FunctionResult VerificarDisponibilidade(string data, string horario, string? unidade = null)
    {
        if (_mockSchedule.ContainsKey(data) && _mockSchedule[data].Contains(horario))
        {
            return new FunctionResult
            {
                Success = true,
                Data = new
                {
                    disponivel = true,
                    data = data,
                    horario = horario,
                    unidade = unidade ?? "Não especificada"
                },
                Message = $"Horário {horario} do dia {data} está disponível!"
            };
        }

        return new FunctionResult
        {
            Success = false,
            Message = $"Horário {horario} do dia {data} não está disponível"
        };
    }

    public FunctionResult AgendarConsulta(string nome, string procedimento, string unidade, string data, string horario)
    {
        // Simula agendamento
        if (_mockSchedule.ContainsKey(data) && _mockSchedule[data].Contains(horario))
        {
            // Remove horário da agenda mockada
            _mockSchedule[data].Remove(horario);

            var agendamentoId = Guid.NewGuid().ToString();

            return new FunctionResult
            {
                Success = true,
                Data = new
                {
                    agendamento_id = agendamentoId,
                    nome = nome,
                    procedimento = procedimento,
                    unidade = unidade,
                    data = data,
                    horario = horario,
                    status = "confirmado"
                },
                Message = $"Agendamento realizado com sucesso! ID: {agendamentoId}"
            };
        }

        return new FunctionResult
        {
            Success = false,
            Message = "Não foi possível realizar o agendamento. Horário não disponível."
        };
    }

    public FunctionResult EnviarConfirmacao(string nome, string data, string horario, string unidade)
    {
        // Simula envio de confirmação
        var confirmacao = $"Olá {nome}, seu agendamento foi confirmado para {data} às {horario} na {unidade}. Aguardamos você!";
        
        return new FunctionResult
        {
            Success = true,
            Data = new
            {
                mensagem_enviada = true,
                destinatario = nome,
                conteudo = confirmacao
            },
            Message = confirmacao
        };
    }
}

public class FunctionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}

