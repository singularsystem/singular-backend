using System.Text.Json.Serialization;

namespace SingularSystem.DTOs.NeckBook
{
    /// <summary>
    /// DTO de entrada recebido pelo endpoint <c>POST /api/v1/neckbook/books/generate</c>
    /// com os parâmetros informados pelo professor (Req 13.1–13.7).
    /// Aceita chaves tanto em camelCase quanto em snake_case.
    /// </summary>
    public class BookForm
    {
        public Guid StudentId { get; set; }

        [JsonPropertyName("student_id")]
        public Guid? StudentIdSnakeCase
        {
            get => null;
            set
            {
                if (value.HasValue && value.Value != Guid.Empty)
                {
                    StudentId = value.Value;
                }
            }
        }

        public string? Disciplina { get; set; }

        public int? ExerciciosPorCapitulo { get; set; }

        [JsonPropertyName("exercicios_por_capitulo")]
        public int? ExerciciosPorCapituloSnakeCase
        {
            get => null;
            set
            {
                if (value.HasValue)
                {
                    ExerciciosPorCapitulo = value.Value;
                }
            }
        }

        public int? NumCapitulos { get; set; }

        [JsonPropertyName("num_capitulos")]
        public int? NumCapitulosSnakeCase
        {
            get => null;
            set
            {
                if (value.HasValue)
                {
                    NumCapitulos = value.Value;
                }
            }
        }

        public bool? SuporteCaa { get; set; }

        [JsonPropertyName("suporte_caa")]
        public bool? SuporteCaaSnakeCase
        {
            get => null;
            set
            {
                if (value.HasValue)
                {
                    SuporteCaa = value.Value;
                }
            }
        }

        public string? EstiloVisual { get; set; }

        [JsonPropertyName("estilo_visual")]
        public string? EstiloVisualSnakeCase
        {
            get => null;
            set
            {
                if (value is not null)
                {
                    EstiloVisual = value;
                }
            }
        }

        public string[]? Interesses { get; set; }

        public string? Tema { get; set; }

        public string? Observacoes { get; set; }
    }
}
