using SingularSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SingularSystem.Models
{
    /// <summary>
    /// Tabela dedicada de deduplicação de webhooks (estratégia fechada de idempotência atômica
    /// no nível do banco). Cada evento de webhook já processado é registrado como uma linha; a
    /// constraint UNIQUE sobre <c>(Provider, EnvelopeOrDocumentToken, EventState)</c> garante que
    /// o reprocessamento do mesmo evento falhe por violação de unicidade dentro da transação,
    /// tornando o processamento idempotente independentemente de concorrência ou reinícios
    /// (Requirements 4.12, 12.5).
    /// </summary>
    public class ProcessedWebhookEvent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// Provedor de assinatura que originou o evento (<see cref="SignatureProvider.ZapSign"/> ou
        /// <see cref="SignatureProvider.NeckSign"/>), persistido como <c>int</c>.
        /// </summary>
        [Required]
        public SignatureProvider Provider { get; set; }

        /// <summary>
        /// Identificador do envelope (NeckSign) ou <c>DocumentToken</c> (ZapSign) associado ao evento.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string EnvelopeOrDocumentToken { get; set; } = string.Empty;

        /// <summary>
        /// Estado derivado do evento aplicado (ex.: <c>"signed"</c>, <c>"declined"</c>, <c>"expired"</c>
        /// para NeckSign; <c>"signed-doc"</c> para ZapSign).
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EventState { get; set; } = string.Empty;

        /// <summary>
        /// Instante em que o evento foi processado.
        /// </summary>
        [Required]
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
