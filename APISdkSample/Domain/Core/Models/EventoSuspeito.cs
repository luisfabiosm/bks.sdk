using Domain.Core.Enums;

namespace Domain.Core.Models
{
    public record EventoSuspeito
    {
        public string Id { get; init; } = string.Empty;
        public string TransacaoId { get; init; } = string.Empty;
        public string Motivo { get; init; } = string.Empty;
        public DateTime DataEvento { get; init; }
        public TipoNivelRisco Severidade { get; init; }
    }

}
