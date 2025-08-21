namespace Domain.Core.Models;

public record FrequenciaTransacoes
{
    public List<DateTime> TransacoesRecentes { get; set; } = new();
    public List<DateTime> TransacoesHoje { get; set; } = new();
}
