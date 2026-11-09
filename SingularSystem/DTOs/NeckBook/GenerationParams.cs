using System.Text.Json.Serialization;
using SingularSystem.Models.Entities;

namespace SingularSystem.DTOs.NeckBook
{
    /// <summary>
    /// Envelope opcional de primeiro nível enviado ao NeckBook em <c>POST /v1/books/generate</c>:
    /// <c>{ "generation_params": { "book_params", "student_profile", "reports" } }</c>.
    /// </summary>
    public class NeckBookGenerateBookRequest
    {
        [JsonPropertyName("generation_params")]
        public GenerationParams GenerationParams { get; set; } = new();
    }

    /// <summary>
    /// Objeto consolidado <c>generation_params</c> contendo <c>book_params</c>,
    /// <c>student_profile</c> e <c>reports</c> (Req 13, 14, 15).
    /// </summary>
    public class GenerationParams
    {
        [JsonPropertyName("book_params")]
        public BookParams BookParams { get; set; } = new();

        [JsonPropertyName("student_profile")]
        public StudentProfile StudentProfile { get; set; } = new();

        [JsonPropertyName("reports")]
        public ReportsSummary Reports { get; set; } = new();
    }

    /// <summary>
    /// Bloco <c>book_params</c> do payload de geração do livro (Req 13.1–13.7).
    /// Parâmetros opcionais não informados permanecem <c>null</c> e são omitidos na serialização.
    /// </summary>
    public class BookParams
    {
        [JsonPropertyName("disciplina")]
        public string Disciplina { get; set; } = string.Empty;

        [JsonPropertyName("exercicios_por_capitulo")]
        public int? ExerciciosPorCapitulo { get; set; }

        [JsonPropertyName("num_capitulos")]
        public int? NumCapitulos { get; set; }

        [JsonPropertyName("suporte_caa")]
        public bool? SuporteCaa { get; set; }

        [JsonPropertyName("estilo_visual")]
        public string? EstiloVisual { get; set; }

        [JsonPropertyName("interesses")]
        public string[]? Interesses { get; set; }

        [JsonPropertyName("tema")]
        public string? Tema { get; set; }

        [JsonPropertyName("observacoes")]
        public string? Observacoes { get; set; }
    }

    /// <summary>
    /// Bloco <c>student_profile</c> do payload de geração do livro (Req 14.1–14.7).
    /// Campos nulos/vazios/fora do intervalo são mantidos como <c>null</c> e omitidos na serialização.
    /// </summary>
    public class StudentProfile
    {
        [JsonPropertyName("student_name")]
        public string? StudentName { get; set; }

        [JsonPropertyName("cpf")]
        public string? Cpf { get; set; }

        [JsonPropertyName("parent_name")]
        public string? ParentName { get; set; }

        [JsonPropertyName("teacher_name")]
        public string? TeacherName { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("school_name")]
        public string? SchoolName { get; set; }

        [JsonPropertyName("serie")]
        public string? Serie { get; set; }

        [JsonPropertyName("diagnostico_principal")]
        public string? DiagnosticoPrincipal { get; set; }

        [JsonPropertyName("type_disability")]
        public string? TypeDisability { get; set; }

        [JsonPropertyName("cars_resultado")]
        public string? CarsResultado { get; set; }

        [JsonPropertyName("snap_iv_resultado")]
        public string? SnapIvResultado { get; set; }

        [JsonPropertyName("cognitivo_resultado")]
        public string? CognitivoResultado { get; set; }

        [JsonPropertyName("escala_di_resultado")]
        public string? EscalaDiResultado { get; set; }

        [JsonPropertyName("scores_portage")]
        public double? ScoresPortage { get; set; }

        [JsonPropertyName("cars_score")]
        public double? CarsScore { get; set; }

        [JsonPropertyName("snap_iv")]
        public double? SnapIv { get; set; }
    }

    /// <summary>
    /// Bloco <c>reports</c> do payload de geração do livro (Req 15.1–15.3).
    /// Cada chave é uma string determinística concatenada por <c>" | "</c> ou <c>null</c> (omitida).
    /// </summary>
    public class ReportsSummary
    {
        [JsonPropertyName("psicologo")]
        public string? Psicologo { get; set; }

        [JsonPropertyName("psicopedagogo")]
        public string? Psicopedagogo { get; set; }

        [JsonPropertyName("assistente_social")]
        public string? AssistenteSocial { get; set; }

        [JsonPropertyName("professor_regular")]
        public string? ProfessorRegular { get; set; }

        [JsonPropertyName("pei_resumo")]
        public string? PeiResumo { get; set; }

        [JsonPropertyName("paee_resumo")]
        public string? PaeeResumo { get; set; }
    }

    /// <summary>
    /// Agrupador das entidades de relatório e instrumentos de um aluno para montagem determinística
    /// de <see cref="StudentProfile"/> e <see cref="ReportsSummary"/> pelo <c>NeckBookPayloadAssembler</c>.
    /// </summary>
    public class ReportsBundle
    {
        public ApplicationUser? Teacher { get; set; }
        public PsychologistReport? PsychologistReport { get; set; }
        public PsychopedagogueReport? PsychopedagogueReport { get; set; }
        public SocialWorkerReport? SocialWorkerReport { get; set; }
        public RegularTeacherQuestionnaire? RegularTeacherQuestionnaire { get; set; }
        public PEI? PEI { get; set; }
        public PAEE? PAEE { get; set; }
        public PortageGuide? PortageGuide { get; set; }

        /// <summary>
        /// Escore numérico agregado do Portage (opcional; quando informado sobrescreve o cálculo a partir de <see cref="PortageGuide"/>).
        /// </summary>
        public double? PortageScore { get; set; }

        /// <summary>
        /// Escore numérico do CARS (opcional; quando informado sobrescreve o parse de <c>CARSQuestionnaire.TotalScore</c>).
        /// </summary>
        public double? CarsScore { get; set; }

        /// <summary>
        /// Escore numérico do SNAP-IV (opcional; quando informado sobrescreve a soma dos contadores de <c>SnapIVQuestionnaire</c>).
        /// </summary>
        public double? SnapIvScore { get; set; }
    }

    /// <summary>
    /// DTO para aprovação de livro e administração de configuração por município.
    /// </summary>
    public class ApproveBookRequest
    {
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class UpsertMunicipalityNeckBookConfigRequest
    {
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class MunicipalityNeckBookConfigResponse
    {
        public Guid Id { get; set; }
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
